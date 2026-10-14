'use strict';

/**
 * 固定窗口限流中间件 (429 / 429400)。
 *
 * 契约来源: game-design/word-chain-game-design.md
 *   - 同一 IP 每分钟超过 10 个请求 -> HTTP 429 + Retry-After
 *   - 同一 IP 每小时超过 100 个请求 -> HTTP 429 + Retry-After   (429400 的第二档触发条件)
 *   - 可疑请求模式 (Bot detection: suspicious pattern of requests) -> HTTP 429 + Retry-After
 *     (429400 的第三条触发条件, 即下面的"突发档"; 默认关闭, 见 burstMax)
 *   - 响应体: { error: 'RateLimitExceeded', message, retryAfter, scope, code: 429400 }
 *     设计文档把该错误码标为 429400 (#### 429400 - Rate Limiting), 故响应体统一带 code: 429400。
 *
 * 设计要点 (与 game-design/api-implementation-example.ts 中的演示实现不同):
 *   1. 配额是"先用后扣": 一个窗口内前 `max` 个请求放行, 第 max+1 个才 429。
 *      演示实现里计数器从不累加, 导致第 2 个请求就 429 —— 这正是"非预期 429"的来源。
 *   2. 时钟可注入 (clock), 因此测试无需真实等待, 也不会因为等待期重叠而误触发 429。
 *   3. 配置可在运行时通过 configure() 调整, 便于运维与测试使用独立配额。
 *   4. 默认不信任 X-Forwarded-For (可伪造); 只有在网关之后部署时才开启 trustProxyHeader。
 *   5. 分钟/小时/突发三档各自独立计数, 任一档超限即 429; Retry-After 取"最晚解锁"的那一档,
 *      因为只等最早的窗口翻篇而更晚的档仍耗尽的话, 客户端按提示重试会立刻再撞一次 429。
 *   6. 突发档默认关闭 (burstMax=0): 测试套件与真实的功能流量都是"毫秒级连打",
 *      若默认开启会把正常功能用例打成非预期 429。开启后仍必须有界(固定窗口计数, 不做滑动窗口)。
 */

const DEFAULT_WINDOW_MS = 60 * 1000; // 1 分钟
const DEFAULT_MAX = 10; // 设计文档: 每 IP 每分钟 10 次
const DEFAULT_HOUR_WINDOW_MS = 60 * 60 * 1000; // 1 小时
const DEFAULT_HOURLY_MAX = 100; // 设计文档: 每 IP 每小时 100 次
const DEFAULT_BURST_WINDOW_MS = 5 * 1000; // 5 秒(可疑模式观测窗)
const DEFAULT_BURST_MAX = 0; // 0 = 关闭突发档(默认); 正数才作为 bot 检测阈值启用
const DEFAULT_MAX_TRACKED_KEYS = 10000;
const SWEEP_TRIGGER_FACTOR = 2; // 桶数量超过上限 * 该系数时强制清理
/** 设计文档 #### 429400 的错误码: 所有限流器产生的 429 响应体都带上 */
const RATE_LIMIT_ERROR_CODE = 429400;

function toPositiveInt(value, fallback) {
  const parsed = Number(value);
  if (!Number.isFinite(parsed) || parsed <= 0) {
    return fallback;
  }
  return Math.floor(parsed);
}

/** 与 toPositiveInt 的区别: 允许显式 0 (0 = 关闭该档), 非法值才回退默认 */
function toQuotaInt(value, fallback) {
  if (value === false || value === 'false') {
    return 0;
  }
  const parsed = Number(value);
  if (!Number.isFinite(parsed) || parsed < 0) {
    return fallback;
  }
  return Math.floor(parsed);
}

/**
 * 默认的限流键: 客户端 IP。
 * 不默认信任 X-Forwarded-For, 避免客户端用伪造请求头绕过限流。
 */
function createDefaultKeyGenerator(trustProxyHeader) {
  return function keyGenerator(req) {
    if (trustProxyHeader) {
      const forwarded = req.headers && req.headers['x-forwarded-for'];
      if (typeof forwarded === 'string' && forwarded.trim().length > 0) {
        return forwarded.split(',')[0].trim();
      }
    }
    return req.ip || (req.socket && req.socket.remoteAddress) || 'unknown';
  };
}

/**
 * 创建限流中间件。
 *
 * @param {object} [options]
 * @param {number} [options.windowMs=60000]        分钟窗口长度(毫秒)
 * @param {number} [options.max=10]                分钟窗口内允许的请求数
 * @param {number} [options.hourWindowMs=3600000]  小时窗口长度(毫秒)
 * @param {number} [options.hourlyMax=100]         小时窗口内允许的请求数; 传 0 关闭该档
 * @param {number} [options.burstWindowMs=5000]    突发档(可疑模式)窗口长度(毫秒)
 * @param {number} [options.burstMax=0]            突发档窗口内允许的请求数; 0(默认)关闭该档
 * @param {number} [options.maxTrackedKeys=10000]  内存保护上限
 * @param {boolean} [options.trustProxyHeader=false] 是否信任 X-Forwarded-For
 * @param {function} [options.keyGenerator]        自定义键函数 (req) => string
 * @param {function} [options.clock]               时钟 () => epochMs, 可注入用于测试
 * @returns {function} express 中间件, 附带 configure/reset/getStats
 */
function createRateLimiter(options) {
  const opts = options || {};

  const settings = {
    windowMs: toPositiveInt(opts.windowMs, DEFAULT_WINDOW_MS),
    max: toPositiveInt(opts.max, DEFAULT_MAX),
    hourWindowMs: toPositiveInt(opts.hourWindowMs, DEFAULT_HOUR_WINDOW_MS),
    hourlyMax: toQuotaInt(opts.hourlyMax, DEFAULT_HOURLY_MAX),
    burstWindowMs: toPositiveInt(opts.burstWindowMs, DEFAULT_BURST_WINDOW_MS),
    burstMax: toQuotaInt(opts.burstMax, DEFAULT_BURST_MAX),
    maxTrackedKeys: toPositiveInt(opts.maxTrackedKeys, DEFAULT_MAX_TRACKED_KEYS),
    trustProxyHeader: opts.trustProxyHeader === true
  };

  let clock = typeof opts.clock === 'function' ? opts.clock : Date.now;
  let keyGenerator =
    typeof opts.keyGenerator === 'function'
      ? opts.keyGenerator
      : createDefaultKeyGenerator(settings.trustProxyHeader);

  /** @type {Map<string, {count: number, resetAt: number}>} 分钟档 */
  const buckets = new Map();
  /** @type {Map<string, {count: number, resetAt: number}>} 小时档 */
  const hourBuckets = new Map();
  /** @type {Map<string, {count: number, resetAt: number}>} 突发档(bot 检测); 关闭时不使用 */
  const burstBuckets = new Map();

  const counters = {
    allowed: 0,
    blocked: 0,
    blockedByMinute: 0,
    blockedByHour: 0,
    blockedByBurst: 0,
    windowsCreated: 0,
    sweeps: 0,
    hardResets: 0
  };

  /** 取(或按当前窗口新建)某个键的计数桶 */
  function touchBucket(store, key, now, windowMs) {
    let bucket = store.get(key);
    if (!bucket || bucket.resetAt <= now) {
      bucket = { count: 0, resetAt: now + windowMs };
      store.set(key, bucket);
      counters.windowsCreated += 1;
    }
    return bucket;
  }

  function sweep(now) {
    counters.sweeps += 1;
    for (const [key, bucket] of buckets) {
      if (bucket.resetAt <= now) {
        buckets.delete(key);
      }
    }
    for (const [key, bucket] of hourBuckets) {
      if (bucket.resetAt <= now) {
        hourBuckets.delete(key);
      }
    }
    for (const [key, bucket] of burstBuckets) {
      if (bucket.resetAt <= now) {
        burstBuckets.delete(key);
      }
    }
    // 最后一道内存保护: 极端情况下(大量不同 IP)直接清空, 避免无限增长。
    if (trackedTotal() > settings.maxTrackedKeys * SWEEP_TRIGGER_FACTOR) {
      buckets.clear();
      hourBuckets.clear();
      burstBuckets.clear();
      counters.hardResets += 1;
    }
  }

  /** 三档桶总数, 用于内存保护判定 */
  function trackedTotal() {
    return buckets.size + hourBuckets.size + burstBuckets.size;
  }

  function middleware(req, res, next) {
    const now = clock();
    const key = keyGenerator(req) || 'unknown';

    const minute = touchBucket(buckets, key, now, settings.windowMs);
    const hour = settings.hourlyMax > 0 ? touchBucket(hourBuckets, key, now, settings.hourWindowMs) : null;
    const burst =
      settings.burstMax > 0 ? touchBucket(burstBuckets, key, now, settings.burstWindowMs) : null;

    const minuteRemaining = Math.max(0, settings.max - minute.count - 1);
    res.setHeader('X-RateLimit-Limit', String(settings.max));
    res.setHeader('X-RateLimit-Remaining', String(minuteRemaining));
    res.setHeader('X-RateLimit-Reset', String(Math.ceil(minute.resetAt / 1000)));

    if (hour) {
      const hourRemaining = Math.max(0, settings.hourlyMax - hour.count - 1);
      res.setHeader('X-RateLimit-Hour-Limit', String(settings.hourlyMax));
      res.setHeader('X-RateLimit-Hour-Remaining', String(hourRemaining));
      res.setHeader('X-RateLimit-Hour-Reset', String(Math.ceil(hour.resetAt / 1000)));
    }

    if (burst) {
      const burstRemaining = Math.max(0, settings.burstMax - burst.count - 1);
      res.setHeader('X-RateLimit-Burst-Limit', String(settings.burstMax));
      res.setHeader('X-RateLimit-Burst-Remaining', String(burstRemaining));
      res.setHeader('X-RateLimit-Burst-Reset', String(Math.ceil(burst.resetAt / 1000)));
    }

    // 任一档超限即拒绝; 决定 Retry-After 的是"最晚解锁"的那一档。
    const blockers = [];
    if (minute.count >= settings.max) {
      blockers.push({ scope: 'minute', resetAt: minute.resetAt });
    }
    if (hour && hour.count >= settings.hourlyMax) {
      blockers.push({ scope: 'hour', resetAt: hour.resetAt });
    }
    // 可疑请求模式(bot 检测): 突发窗内放行数已达阈值
    if (burst && burst.count >= settings.burstMax) {
      blockers.push({ scope: 'burst', resetAt: burst.resetAt });
    }

    if (blockers.length > 0) {
      const governing = blockers.reduce(
        (latest, item) => (item.resetAt > latest.resetAt ? item : latest),
        blockers[0]
      );
      const retryAfter = Math.max(1, Math.ceil((governing.resetAt - now) / 1000));
      counters.blocked += 1;
      if (governing.scope === 'hour') {
        counters.blockedByHour += 1;
      } else if (governing.scope === 'burst') {
        counters.blockedByBurst += 1;
      } else {
        counters.blockedByMinute += 1;
      }
      res.setHeader('Retry-After', String(retryAfter));
      res.setHeader('X-RateLimit-Scope', governing.scope);
      return res.status(429).json({
        error: 'RateLimitExceeded',
        message: 'Too many requests. Please slow down.',
        retryAfter,
        scope: governing.scope,
        // 设计文档 #### 429400 - Rate Limiting: 限流错误的业务错误码
        code: RATE_LIMIT_ERROR_CODE
      });
    }

    minute.count += 1;
    if (hour) {
      hour.count += 1;
    }
    if (burst) {
      burst.count += 1;
    }
    counters.allowed += 1;

    if (trackedTotal() > settings.maxTrackedKeys) {
      sweep(now);
    }

    return next();
  }

  middleware.configure = function configure(partial) {
    const next_ = partial || {};
    if (next_.windowMs !== undefined) {
      settings.windowMs = toPositiveInt(next_.windowMs, settings.windowMs);
    }
    if (next_.max !== undefined) {
      settings.max = toPositiveInt(next_.max, settings.max);
    }
    if (next_.hourWindowMs !== undefined) {
      settings.hourWindowMs = toPositiveInt(next_.hourWindowMs, settings.hourWindowMs);
    }
    if (next_.hourlyMax !== undefined) {
      settings.hourlyMax = toQuotaInt(next_.hourlyMax, settings.hourlyMax);
    }
    if (next_.burstWindowMs !== undefined) {
      settings.burstWindowMs = toPositiveInt(next_.burstWindowMs, settings.burstWindowMs);
    }
    if (next_.burstMax !== undefined) {
      settings.burstMax = toQuotaInt(next_.burstMax, settings.burstMax);
    }
    if (next_.maxTrackedKeys !== undefined) {
      settings.maxTrackedKeys = toPositiveInt(next_.maxTrackedKeys, settings.maxTrackedKeys);
    }
    if (next_.trustProxyHeader !== undefined) {
      settings.trustProxyHeader = next_.trustProxyHeader === true;
      if (typeof opts.keyGenerator !== 'function') {
        keyGenerator = createDefaultKeyGenerator(settings.trustProxyHeader);
      }
    }
    if (typeof next_.clock === 'function') {
      clock = next_.clock;
    }
    if (typeof next_.keyGenerator === 'function') {
      keyGenerator = next_.keyGenerator;
    }
    return middleware;
  };

  middleware.reset = function reset() {
    buckets.clear();
    hourBuckets.clear();
    burstBuckets.clear();
    counters.allowed = 0;
    counters.blocked = 0;
    counters.blockedByMinute = 0;
    counters.blockedByHour = 0;
    counters.blockedByBurst = 0;
    counters.windowsCreated = 0;
    counters.sweeps = 0;
    counters.hardResets = 0;
    return middleware;
  };

  middleware.getStats = function getStats() {
    return {
      windowMs: settings.windowMs,
      max: settings.max,
      hourWindowMs: settings.hourWindowMs,
      hourlyMax: settings.hourlyMax,
      burstWindowMs: settings.burstWindowMs,
      burstMax: settings.burstMax,
      trustProxyHeader: settings.trustProxyHeader,
      trackedKeys: buckets.size,
      hourTrackedKeys: hourBuckets.size,
      burstTrackedKeys: burstBuckets.size,
      allowed: counters.allowed,
      blocked: counters.blocked,
      blockedByMinute: counters.blockedByMinute,
      blockedByHour: counters.blockedByHour,
      blockedByBurst: counters.blockedByBurst,
      windowsCreated: counters.windowsCreated,
      sweeps: counters.sweeps,
      hardResets: counters.hardResets
    };
  };

  return middleware;
}

module.exports = {
  createRateLimiter,
  RATE_LIMIT_ERROR_CODE,
  DEFAULT_WINDOW_MS,
  DEFAULT_MAX,
  DEFAULT_HOUR_WINDOW_MS,
  DEFAULT_HOURLY_MAX,
  DEFAULT_BURST_WINDOW_MS,
  DEFAULT_BURST_MAX,
  DEFAULT_MAX_TRACKED_KEYS
};