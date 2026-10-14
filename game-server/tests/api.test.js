'use strict';

/**
 * game-server 集成测试 (node:test + node:http, 零第三方依赖)。
 *
 *   npm test            # node --test tests/
 *
 * 设计约束 (对应本次任务的硬要求): 测试过程绝不依赖真实等待去触碰限流窗口 ——
 *   - 功能类测试在 setup 里把限额调到 1e6, 保证不会出现"非预期 429";
 *   - 429 路径用注入时钟 (clock) 主动验证, 不 sleep 一分钟。
 *
 * 注意: winston 的日志文件路径是相对 cwd 的, 因此这里先 chdir 到 game-server 根目录。
 */

const path = require('node:path');
process.chdir(path.join(__dirname, '..'));

// CORS 必须在 require('../server') 之前配置: server.js 在加载时就把 env 读进 config。
// 真正的"默认关闭"行为由 CORS 段落的单测覆盖(不传 origin)。
process.env.WORD_CHAIN_CORS_ORIGIN = 'https://client.example';

const test = require('node:test');
const { before, after } = require('node:test');
const assert = require('node:assert/strict');
const http = require('node:http');
const fs = require('node:fs');

const {
  startServer,
  fetchWithRetry,
  logger,
  circuitBreakers,
  degradedMode,
  consoleFormat,
  wordChainRateLimiter,
  wordChainService,
  closeLogger
} = require('../server');
const { createDictionary } = require('../src/word-chain/dictionary');
const { createGameService } = require('../src/word-chain/game-service');
const { createRateLimiter } = require('../src/rate-limit');
const { createCorsMiddleware } = require('../src/cors');

// 去掉控制台输出, 保留文件日志(下面有日志回归测试)
for (const transport of logger.transports.slice()) {
  if (transport.name === 'console') {
    logger.remove(transport);
  }
}

const LOG_DIR = path.join(__dirname, '..', 'logs');
const COMBINED_LOG = path.join(LOG_DIR, 'combined.log');

let server = null;
let baseUrl = '';

// ------------------------------------------------------------------ 工具

function request(method, urlPath, body, headers) {
  return new Promise((resolve, reject) => {
    const payload = body === undefined ? null : JSON.stringify(body);
    const finalHeaders = Object.assign({}, headers);
    if (payload !== null) {
      finalHeaders['content-type'] = 'application/json';
      finalHeaders['content-length'] = Buffer.byteLength(payload);
    }

    const req = http.request(
      { host: '127.0.0.1', port: server.address().port, method, path: urlPath, headers: finalHeaders },
      (res) => {
        let raw = '';
        res.setEncoding('utf8');
        res.on('data', (chunk) => { raw += chunk; });
        res.on('end', () => {
          let parsed = null;
          try {
            parsed = raw.length > 0 ? JSON.parse(raw) : null;
          } catch (error) {
            parsed = raw;
          }
          // 只统计"真的被限流器拦下"的 429: /simulate-error 会按剧本返回 429, 那是被测行为不是配额问题
          if (res.statusCode === 429 && !expect429 && parsed && parsed.error === 'RateLimitExceeded') {
            unexpected429.push(`${method} ${urlPath}`);
          }
          resolve({ status: res.statusCode, headers: res.headers, body: parsed, raw });
        });
      }
    );

    req.on('error', reject);
    if (payload !== null) {
      req.write(payload);
    }
    req.end();
  });
}

const get = (urlPath, headers) => request('GET', urlPath, undefined, headers);
const post = (urlPath, body, headers) => request('POST', urlPath, body, headers);

// 「0 次非预期 429」收口: 只有显式声明预期的限流用例才允许看到 429,
// 其余任何一次 429 都会被记下来, 并在 after() 里让整套测试红掉。
let expect429 = false;
const unexpected429 = [];
function allow429(fn) {
  return async (...args) => {
    expect429 = true;
    try {
      return await fn(...args);
    } finally {
      expect429 = false;
    }
  };
}

async function newGame(names) {
  const res = await post('/api/v1/games', {
    players: (names || ['Alice', 'Bob']).map((name) => ({ name }))
  });
  assert.equal(res.status, 201, `创建对局失败: ${res.status} ${res.raw}`);
  return res.body;
}

function play(gameId, playerId, word) {
  return post(`/api/v1/games/${gameId}/words`, { playerId, word });
}

async function waitFor(check, timeoutMs = 3000) {
  const deadline = Date.now() + timeoutMs;
  for (;;) {
    const value = await check();
    if (value) {
      return value;
    }
    if (Date.now() > deadline) {
      return null;
    }
    await new Promise((resolve) => setTimeout(resolve, 50));
  }
}

/** 构造中间件用的假 req/res, 便于直接单测限流器 */
function fakeReq(ip, headers) {
  return { headers: headers || {}, ip: ip || '10.0.0.1', socket: { remoteAddress: ip || '10.0.0.1' } };
}

function fakeRes() {
  const headers = {};
  return {
    statusCode: 0,
    body: null,
    headers,
    setHeader(key, value) { headers[String(key).toLowerCase()] = String(value); },
    status(code) { this.statusCode = code; return this; },
    json(payload) { this.body = payload; return this; }
  };
}

// ------------------------------------------------------------------ 生命周期

before(async () => {
  server = await startServer(0);
  baseUrl = `http://127.0.0.1:${server.address().port}`;

  // 功能测试期间把配额拉高: 既不触发 429, 也不改动生产默认值(见 /health 断言)
  // 分钟档与小时档都要抬: 整套用例来自同一个 IP, 只抬分钟档会撞上 100 次/小时 -> 非预期 429
  wordChainRateLimiter.configure({
    max: 1000000,
    windowMs: 60000,
    hourlyMax: 1000000,
    hourWindowMs: 3600000,
    clock: () => Date.now()
  });
  wordChainRateLimiter.reset();
  wordChainService.reset();
});

after(async () => {
  if (server) {
    await new Promise((resolve) => server.close(resolve));
  }
  await closeLogger();
  // 收口: 整套功能用例跑完, 除限流用例之外不应有任何一次 429
  // (放在资源清理之后: 断言失败也不能让进程挂着 server 不退出)
  assert.deepEqual(unexpected429, [], `出现非预期 429: ${unexpected429.join(', ')}`);
});

// ================================================================== 词库

test('词库: 每个字母都能接上, 且没有死尾字母', () => {
  const dictionary = createDictionary();
  assert.ok(dictionary.size >= 100, `词库过小: ${dictionary.size}`);

  const deadEndLetters = ['b', 'i', 'j', 'p', 'q', 'u', 'v', 'x'];
  const letters = 'abcdefghijklmnopqrstuvwxyz'.split('');

  for (const letter of letters) {
    const result = dictionary.hint(letter, { limit: 1 });
    assert.ok(result.totalAvailable >= 1, `字母 ${letter} 没有可接的词, 会造成接龙死局`);
  }
  for (const word of dictionary.words) {
    assert.ok(!deadEndLetters.includes(word[word.length - 1]), `词 ${word} 以死尾字母结尾`);
  }
});

test('词库: has() 忽略大小写与首尾空格', () => {
  const dictionary = createDictionary();
  assert.equal(dictionary.has('apple'), true);
  assert.equal(dictionary.has('  APPLE '), true);
  assert.equal(dictionary.has('notaword'), false);
  assert.equal(dictionary.has(''), false);
  assert.equal(dictionary.has(undefined), false);
});

test('词库: hint 会排除已用词并遵守 limit', () => {
  const dictionary = createDictionary();
  const all = dictionary.hint('e', { limit: 10 });
  assert.ok(all.hints.length >= 2, 'E 开头词太少, 无法验证排除逻辑');
  assert.ok(all.hints.every((word) => word.startsWith('e')));

  const filtered = dictionary.hint('e', { limit: 2, usedWords: [all.hints[0]] });
  assert.equal(filtered.hints.length, 2);
  assert.ok(!filtered.hints.includes(all.hints[0]), '已使用的词不应出现在提示里');
  assert.equal(filtered.totalAvailable, all.totalAvailable - 1);
});

// ================================================================== 限流器单测

test('限流: 默认窗口与配额就是设计文档的 10 次/分钟', () => {
  const limiter = createRateLimiter();
  const stats = limiter.getStats();
  assert.equal(stats.max, 10);
  assert.equal(stats.windowMs, 60000);
  assert.equal(stats.trustProxyHeader, false);
});

test('限流: 固定窗口内前 max 次放行, 第 max+1 次才是 429 (无 off-by-one)', () => {
  let now = 1700000000000;
  const limiter = createRateLimiter({ max: 3, windowMs: 60000, clock: () => now });

  let passed = 0;
  for (let i = 0; i < 3; i += 1) {
    const res = fakeRes();
    let nextCalled = false;
    limiter(fakeReq('1.2.3.4'), res, () => { nextCalled = true; });
    assert.equal(nextCalled, true, `第 ${i + 1} 个请求应当放行`);
    assert.equal(res.statusCode, 0);
    passed += 1;
  }
  assert.equal(passed, 3);

  const blocked = fakeRes();
  let blockedNext = false;
  limiter(fakeReq('1.2.3.4'), blocked, () => { blockedNext = true; });
  assert.equal(blockedNext, false, '超出配额不应继续处理');
  assert.equal(blocked.statusCode, 429);
  assert.equal(blocked.body.error, 'RateLimitExceeded');
  assert.equal(blocked.body.message, 'Too many requests. Please slow down.');
  assert.ok(Number(blocked.body.retryAfter) >= 1);
  assert.equal(blocked.headers['x-ratelimit-remaining'], '0');
  assert.ok(Number(blocked.headers['retry-after']) >= 1);

  // 窗口内继续请求仍然 429
  const stillBlocked = fakeRes();
  limiter(fakeReq('1.2.3.4'), stillBlocked, () => {});
  assert.equal(stillBlocked.statusCode, 429);

  // 另一个 IP 有独立配额
  const otherIp = fakeRes();
  let otherNext = false;
  limiter(fakeReq('5.6.7.8'), otherIp, () => { otherNext = true; });
  assert.equal(otherNext, true);

  // 窗口滑过后恢复
  now += 60000 + 1;
  const afterWindow = fakeRes();
  let afterNext = false;
  limiter(fakeReq('1.2.3.4'), afterWindow, () => { afterNext = true; });
  assert.equal(afterNext, true, '窗口过期后应重新放行');

  const stats = limiter.getStats();
  assert.equal(stats.blocked, 2);
  assert.equal(stats.allowed, 5);
});

test('限流: 默认忽略 X-Forwarded-For (防止伪造请求头绕过)', () => {
  const limiter = createRateLimiter({ max: 1, windowMs: 60000 });
  const first = fakeRes();
  limiter(fakeReq('9.9.9.9', { 'x-forwarded-for': '1.1.1.1' }), first, () => {});
  assert.equal(first.statusCode, 0);

  const second = fakeRes();
  limiter(fakeReq('9.9.9.9', { 'x-forwarded-for': '2.2.2.2' }), second, () => {});
  assert.equal(second.statusCode, 429, '换了伪造的 XFF 也不该获得新配额');

  // 显式信任代理时才按 XFF 分桶
  const trusting = createRateLimiter({ max: 1, windowMs: 60000, trustProxyHeader: true });
  const a = fakeRes();
  trusting(fakeReq('9.9.9.9', { 'x-forwarded-for': '1.1.1.1' }), a, () => {});
  const b = fakeRes();
  let bNext = false;
  trusting(fakeReq('9.9.9.9', { 'x-forwarded-for': '2.2.2.2' }), b, () => { bNext = true; });
  assert.equal(a.statusCode, 0);
  assert.equal(bNext, true);
});

test('限流: 配置可在运行时调整并重置, 便于运维与测试', () => {
  const limiter = createRateLimiter({ max: 1 });
  limiter(fakeReq('7.7.7.7'), fakeRes(), () => {});
  assert.equal(fakeRes().statusCode, 0);

  limiter.configure({ max: 5 });
  const res = fakeRes();
  let next = false;
  limiter(fakeReq('7.7.7.7'), res, () => { next = true; });
  assert.equal(next, true, '提高配额后应重新放行');

  limiter.reset();
  assert.equal(limiter.getStats().allowed, 0);
  assert.equal(limiter.getStats().trackedKeys, 0);
});

test('限流: 默认小时档就是设计文档的 100 次/小时', () => {
  const limiter = createRateLimiter();
  const stats = limiter.getStats();
  assert.equal(stats.hourlyMax, 100);
  assert.equal(stats.hourWindowMs, 3600000);
});

test('限流: 分钟档充足但小时档耗尽 -> 429, Retry-After 取最晚解锁的小时窗口', () => {
  let now = 1700000000000;
  const limiter = createRateLimiter({
    max: 2,
    windowMs: 60000,
    hourlyMax: 4,
    hourWindowMs: 3600000,
    clock: () => now
  });

  // 4 个请求分处两个分钟窗口: 分钟档一次都没超限
  for (let round = 0; round < 2; round += 1) {
    for (let i = 0; i < 2; i += 1) {
      let next = false;
      limiter(fakeReq('4.4.4.4'), fakeRes(), () => { next = true; });
      assert.equal(next, true, `第 ${round * 2 + i + 1} 个请求应当放行`);
    }
    now += 60001;
  }

  // 第 5 个: 分钟档是全新的(0/2), 但小时档 4/4 已满
  const res = fakeRes();
  let blockedNext = false;
  limiter(fakeReq('4.4.4.4'), res, () => { blockedNext = true; });
  assert.equal(blockedNext, false, '小时档超限不应继续处理');
  assert.equal(res.statusCode, 429);
  assert.equal(res.body.error, 'RateLimitExceeded');
  assert.equal(res.body.scope, 'hour');
  assert.equal(res.headers['x-ratelimit-scope'], 'hour');
  // 分钟窗口只剩 ~59 秒, 若按分钟档给 Retry-After 就会误导客户端提前重试再撞 429
  assert.ok(
    Number(res.body.retryAfter) > 3000,
    `retryAfter 应反映小时窗口, 实际 ${res.body.retryAfter}`
  );
  assert.equal(Number(res.headers['x-ratelimit-remaining']), 1, '分钟档仍有余额');
  assert.equal(res.headers['x-ratelimit-hour-remaining'], '0');

  const stats = limiter.getStats();
  assert.equal(stats.blocked, 1);
  assert.equal(stats.blockedByHour, 1);
  assert.equal(stats.blockedByMinute, 0);
  assert.equal(stats.allowed, 4);

  // 小时窗口滑过后恢复
  now += 3600000;
  let recoveredNext = false;
  limiter(fakeReq('4.4.4.4'), fakeRes(), () => { recoveredNext = true; });
  assert.equal(recoveredNext, true, '小时窗口过期后应重新放行');
});

test('限流: 分钟档超限时 scope=minute, 与小时档互不干扰', () => {
  let now = 1700000000000;
  const limiter = createRateLimiter({
    max: 1, windowMs: 60000, hourlyMax: 100, hourWindowMs: 3600000, clock: () => now
  });

  limiter(fakeReq('8.8.8.8'), fakeRes(), () => {});
  const res = fakeRes();
  limiter(fakeReq('8.8.8.8'), res, () => {});
  assert.equal(res.statusCode, 429);
  assert.equal(res.body.scope, 'minute');
  assert.ok(Number(res.body.retryAfter) <= 60, '分钟档的 Retry-After 不应超过一分钟');

  const stats = limiter.getStats();
  assert.equal(stats.blockedByMinute, 1);
  assert.equal(stats.blockedByHour, 0);
});

test('限流: hourlyMax=0 关闭小时档, 且不再下发小时档响应头', () => {
  let now = 1700000000000;
  const limiter = createRateLimiter({
    max: 2, windowMs: 60000, hourlyMax: 0, hourWindowMs: 3600000, clock: () => now
  });

  // 5 个请求分处 3 个分钟窗口: 小时档关闭后, 累计超过 100 次也不会被拦
  for (let i = 0; i < 5; i += 1) {
    const res = fakeRes();
    let next = false;
    limiter(fakeReq('6.6.6.6'), res, () => { next = true; });
    assert.equal(next, true, `第 ${i + 1} 个请求不应被小时档拦下`);
    assert.equal(res.headers['x-ratelimit-hour-limit'], undefined, '关闭小时档不应下发该响应头');
    if (i % 2 === 1) {
      now += 60001; // 每放行 2 个翻一个分钟窗口, 正好贴着分钟档的额度
    }
  }

  const stats = limiter.getStats();
  assert.equal(stats.allowed, 5);
  assert.equal(stats.blocked, 0, '小时档关闭后不应有任何 429');
  assert.equal(stats.hourTrackedKeys, 0);
});

// ================================================================== 既有端点回归

test('GET /health: 200 healthy, 并暴露词链与限流配置', async () => {
  const res = await get('/health');
  assert.equal(res.status, 200, res.raw);
  assert.equal(res.body.status, 'healthy');
  assert.ok(res.body.wordChain, 'health 应包含词链服务统计');
  assert.equal(res.body.wordChain.dictionarySize > 0, true);
  assert.equal(res.body.configuration.wordChainRateLimit.max, 10);
  assert.equal(res.body.configuration.wordChainRateLimit.windowMs, 60000);
  assert.equal(res.body.configuration.wordChainRateLimit.hourlyMax, 100);
  assert.equal(res.body.configuration.wordChainRateLimit.hourWindowMs, 3600000);
  assert.equal(res.body.configuration.wordChainRateLimit.trustProxyHeader, false);
  assert.ok(res.body.requestId, 'health 应回显 requestId');
});

test('X-Request-ID: 客户端传入则回显, 不传则生成', async () => {
  const echoed = await get('/health', { 'X-Request-ID': 'probe-req-001' });
  assert.equal(echoed.headers['x-request-id'], 'probe-req-001');

  const generated = await get('/health');
  assert.match(String(generated.headers['x-request-id']), /^req-\d+-[a-z0-9]+$/);
});

test('GET /metrics: 包含词链与限流统计', async () => {
  await newGame(['MetricsA', 'MetricsB']);
  const res = await get('/metrics');
  assert.equal(res.status, 200);
  assert.ok(res.body.errors && typeof res.body.errors.total === 'number');
  assert.ok(res.body.wordChain.games >= 1);
  assert.equal(res.body.rateLimit.max, 1000000);
  assert.equal(res.body.rateLimit.hourlyMax, 1000000, '功能测试期间小时档也必须被抬高, 否则会产生非预期 429');
  assert.ok(res.body.rateLimit.allowed > 0);
});

test('GET /simulate-error: 429/500/network 循环与 attempt 计数保持一致', async () => {
  const first = await get('/simulate-error');
  assert.ok([429, 500, 503].includes(first.status));
  const start = first.body.attempt;
  assert.equal(typeof start, 'number');

  for (let k = 0; k < 8; k += 1) {
    const res = await get('/simulate-error');
    const attempt = start + 1 + k;
    const expected = attempt % 4 === 0 ? 429 : (attempt % 3 === 0 ? 500 : 503);
    assert.equal(res.body.attempt, attempt, '计数器应当单调递增');
    assert.equal(res.status, expected, `第 ${attempt} 次应当返回 ${expected}`);
  }
});

test('fetchWithRetry: 退避重试真的会重试 (修复 const delay 崩溃)', async () => {
  let hits = 0;
  const flaky = http.createServer((req, res) => {
    hits += 1;
    if (hits < 3) {
      res.writeHead(503, { 'content-type': 'application/json' });
      res.end('{"error":"simulated 503"}');
      return;
    }
    res.writeHead(200, { 'content-type': 'application/json' });
    res.end('{"ok":true}');
  });
  await new Promise((resolve) => flaky.listen(0, '127.0.0.1', resolve));

  try {
    const response = await fetchWithRetry(
      `http://127.0.0.1:${flaky.address().port}/flaky`,
      {},
      3,
      10
    );
    assert.equal(response.status, 200);
    assert.equal(response.data.ok, true);
    assert.equal(hits, 3, '应当在第 3 次尝试成功');
  } finally {
    await new Promise((resolve) => flaky.close(resolve));
  }
});

test('GET /api/game-status: 上游模拟失败时返回结构化错误而不是未捕获异常', async () => {
  circuitBreakers.gameService.failureCount = 0;
  circuitBreakers.gameService.state = 'CLOSED';

  const res = await get('/api/game-status');
  // /simulate-error 只有 429/500/503 三种结果, 因此该端点必然走降级/错误分支;
  // 关键是"返回结构化 JSON 且进程不崩"。
  assert.ok([400, 429, 500, 503].includes(res.status), `意外状态码: ${res.status} ${res.raw}`);
  assert.equal(typeof res.body.message, 'string');
  assert.ok(res.body.errorType, '应当带上错误分类');
});

test('日志: 请求会被写入 logs/combined.log', async () => {
  const probeId = `log-probe-${Date.now()}`;
  // 前面的错误路径测试会把降级模式打开, 此时 /health 按设计返回 503;
  // 这里只关心"请求是否落盘", 所以先复位降级状态, 让断言只依赖日志行为。
  degradedMode.enabled = false;
  const res = await get('/health', { 'X-Request-ID': probeId });
  assert.equal(res.status, 200);

  const hit = await waitFor(() => {
    if (!fs.existsSync(COMBINED_LOG)) {
      return false;
    }
    return fs.readFileSync(COMBINED_LOG, 'utf8').includes(probeId);
  }, 5000);
  assert.ok(hit, `combined.log 中应出现 ${probeId}`);
});

// ================================================================== 词链: 创建

test('POST /api/v1/games: 201 + Location + 初始状态', async () => {
  const res = await post('/api/v1/games', { players: [{ name: 'Alice' }, { name: 'Bob' }] });
  assert.equal(res.status, 201, res.raw);
  assert.equal(res.headers.location, `/api/v1/games/${res.body.gameId}`);
  assert.equal(res.body.status, 'active');
  assert.equal(res.body.players.length, 2);
  assert.equal(res.body.currentTurnIndex, 0);
  assert.deepEqual(res.body.wordChain, []);
  assert.equal(res.body.requiredLetter, null);
  assert.equal(res.body.maxChainLength, 10);
  assert.equal(res.body.hasPendingChallenge, false);
  assert.equal(res.body.winner, undefined);
  assert.ok(res.body.players.every((p) => p.score === 0));
  assert.ok(res.body.players.every((p) => typeof p.id === 'string' && p.id.length > 0));
  assert.ok(res.body.createdAt);
});

test('POST /api/v1/games: 玩家数必须在 2-4 之间', async () => {
  const tooFew = await post('/api/v1/games', { players: [{ name: 'Solo' }] });
  assert.equal(tooFew.status, 400);
  assert.equal(tooFew.body.error, 'ValidationError');
  assert.equal(tooFew.body.details[0].field, 'players');

  const tooMany = await post('/api/v1/games', {
    players: [{ name: 'A' }, { name: 'B' }, { name: 'C' }, { name: 'D' }, { name: 'E' }]
  });
  assert.equal(tooMany.status, 400);
  assert.equal(tooMany.body.details[0].field, 'players');

  const notArray = await post('/api/v1/games', {});
  assert.equal(notArray.status, 400);
  assert.equal(notArray.body.error, 'ValidationError');
});

test('POST /api/v1/games: 空名/重名/重复 id 一律 400', async () => {
  const empty = await post('/api/v1/games', { players: [{ name: '  ' }, { name: 'Bob' }] });
  assert.equal(empty.status, 400);
  assert.equal(empty.body.details[0].field, 'players[0].name');

  const duplicate = await post('/api/v1/games', { players: [{ name: 'Bob' }, { name: 'bob' }] });
  assert.equal(duplicate.status, 400);
  assert.match(duplicate.body.details[0].message, /Duplicate player name/);

  const duplicateId = await post('/api/v1/games', {
    players: [{ id: 'x', name: 'A' }, { id: 'x', name: 'B' }]
  });
  assert.equal(duplicateId.status, 400);
  assert.match(duplicateId.body.details[0].message, /Duplicate player id/);
});

test('POST /api/v1/games: 也接受裸字符串玩家名(宽容输入)', async () => {
  const res = await post('/api/v1/games', { players: ['Zoe', 'Yan'] });
  assert.equal(res.status, 201, res.raw);
  assert.deepEqual(res.body.players.map((p) => p.name), ['Zoe', 'Yan']);
});

// ================================================================== 词链: 提交单词

test('POST /words: 第一个词不限首字母, 计分并轮转', async () => {
  const game = await newGame();
  const [alice, bob] = game.players;

  const res = await play(game.gameId, alice.id, 'Apple');
  assert.equal(res.status, 200, res.raw);
  assert.equal(res.body.word, 'apple', '应归一化为小写');
  assert.deepEqual(res.body.wordChain, ['apple']);
  assert.equal(res.body.chainLength, 1);
  assert.equal(res.body.currentTurnIndex, 1);
  assert.equal(res.body.currentPlayerId, bob.id);
  assert.equal(res.body.requiredLetter, 'e');
  assert.equal(res.body.scoreAwarded, 5);
  assert.equal(res.body.players[0].score, 5);
  assert.equal(res.body.status, 'active');
});

test('POST /words: 不是自己的回合 → 403 Forbidden + 当前回合信息', async () => {
  const game = await newGame();
  const [alice] = game.players;
  await play(game.gameId, alice.id, 'apple');

  const res = await play(game.gameId, alice.id, 'engine');
  assert.equal(res.status, 403);
  assert.equal(res.body.error, 'Forbidden');
  assert.equal(res.body.currentTurnIndex, 1);
  assert.equal(res.body.playerId, alice.id);
});

test('POST /words: 词不在词库 → 400', async () => {
  const game = await newGame();
  const res = await play(game.gameId, game.players[0].id, 'zzzznotaword');
  assert.equal(res.status, 400);
  assert.equal(res.body.error, 'ValidationError');
  assert.equal(res.body.details[0].field, 'word');
  assert.match(res.body.details[0].message, /not in the dictionary/);
});

test('POST /words: 空值/非字母/超长 → 400 details=word', async () => {
  const game = await newGame();
  const playerId = game.players[0].id;

  const empty = await play(game.gameId, playerId, '   ');
  assert.equal(empty.status, 400);
  assert.equal(empty.body.details[0].field, 'word');

  const missing = await post(`/api/v1/games/${game.gameId}/words`, { playerId });
  assert.equal(missing.status, 400);

  const digits = await play(game.gameId, playerId, 'abc123');
  assert.equal(digits.status, 400);
  assert.match(digits.body.details[0].message, /letters only/);

  const tooLong = await play(game.gameId, playerId, 'a'.repeat(30));
  assert.equal(tooLong.status, 400);
  assert.match(tooLong.body.details[0].message, /length/);
});

test('POST /words: 首字母不衔接 → 400 + previousWord/requiredLetter', async () => {
  const game = await newGame();
  const [alice, bob] = game.players;
  await play(game.gameId, alice.id, 'apple');

  const res = await play(game.gameId, bob.id, 'tiger');
  assert.equal(res.status, 400);
  assert.equal(res.body.error, 'ValidationError');
  assert.equal(res.body.previousWord, 'apple');
  assert.equal(res.body.requiredLetter, 'e');
  assert.equal(res.body.actualLetter, 't');
  assert.match(res.body.details[0].message, /must start with "e"/);
});

test('POST /words: 同一个词不能在同一条链里重复使用', async () => {
  const game = await newGame();
  const [alice, bob] = game.players;

  const first = await play(game.gameId, alice.id, 'eagle'); // e...e, 首尾同字母
  assert.equal(first.status, 200, first.raw);

  const duplicate = await play(game.gameId, bob.id, 'eagle');
  assert.equal(duplicate.status, 400);
  assert.match(duplicate.body.details[0].message, /already been used/);
});

test('POST /words: gameId / playerId 不存在 → 404', async () => {
  const missingGame = await post('/api/v1/games/does-not-exist/words', {
    playerId: 'whoever',
    word: 'apple'
  });
  assert.equal(missingGame.status, 404);
  assert.equal(missingGame.body.error, 'NotFound');
  assert.equal(missingGame.body.resource, 'game');

  const game = await newGame();
  const missingPlayer = await play(game.gameId, 'ghost-player', 'apple');
  assert.equal(missingPlayer.status, 404);
  assert.equal(missingPlayer.body.resource, 'player');
});

// ================================================================== 词链: 查询与提示

test('GET /api/v1/games/:gameId: 返回 history 与 challenges', async () => {
  const game = await newGame();
  const [alice, bob] = game.players;
  await play(game.gameId, alice.id, 'apple');
  await play(game.gameId, bob.id, 'engine');
  await post(`/api/v1/games/${game.gameId}/challenges`, {
    challengerId: alice.id,
    challengedWordIndex: 1
  });

  const res = await get(`/api/v1/games/${game.gameId}`);
  assert.equal(res.status, 200);
  assert.deepEqual(res.body.wordChain, ['apple', 'engine']);
  assert.equal(res.body.history.length, 2);
  assert.equal(res.body.history[1].word, 'engine');
  assert.equal(res.body.history[1].playerId, bob.id);
  assert.equal(res.body.challenges.length, 1);
  assert.equal(res.body.challenges[0].challengeStatus, 'pending');
  assert.equal(res.body.hasPendingChallenge, true);
  assert.equal(res.body.pendingChallengeId, res.body.challenges[0].challengeId);
});

test('GET /api/v1/games: 支持 limit 与 status 过滤', async () => {
  const game = await newGame(['ListA', 'ListB']);
  await play(game.gameId, game.players[0].id, 'apple');

  const limited = await get('/api/v1/games?limit=1');
  assert.equal(limited.status, 200);
  assert.equal(limited.body.games.length, 1);
  assert.ok(limited.body.total >= 1);

  const active = await get('/api/v1/games?status=active&limit=200');
  assert.ok(active.body.games.every((item) => item.status === 'active'));

  const completed = await get('/api/v1/games?status=completed&limit=200');
  assert.ok(completed.body.games.every((item) => item.status === 'completed'));
  assert.ok(completed.body.total <= active.body.total);
});

test('GET /api/v1/games/:gameId/hint: 给出当前必须衔接的首字母候选', async () => {
  const game = await newGame();
  const [alice] = game.players;

  const beforeAnyWord = await get(`/api/v1/games/${game.gameId}/hint`);
  assert.equal(beforeAnyWord.status, 200);
  assert.equal(beforeAnyWord.body.requiredLetter, null);
  assert.equal(beforeAnyWord.body.hints.length, 3);

  await play(game.gameId, alice.id, 'apple');
  const afterApple = await get(`/api/v1/games/${game.gameId}/hint?limit=5`);
  assert.equal(afterApple.body.requiredLetter, 'e');
  assert.ok(afterApple.body.hints.length >= 1);
  assert.ok(afterApple.body.hints.every((word) => word.startsWith('e')));
  assert.ok(!afterApple.body.hints.includes('apple'), '已用词不应作为提示');
  assert.equal(afterApple.body.nextPlayerId, game.players[1].id);

  const missing = await get('/api/v1/games/nope/hint');
  assert.equal(missing.status, 404);
});

test('完整一局: 链满 maxChainLength → completed, 最高分获胜; 结束后拒绝新词', async () => {
  const game = await newGame(['ChainA', 'ChainB']);
  const [chainA, chainB] = game.players;
  const script = [
    [chainA, 'apple'],
    [chainB, 'engine'],
    [chainA, 'eagle'],
    [chainB, 'echo'],
    [chainA, 'ocean'],
    [chainB, 'nest'],
    [chainA, 'tiger'],
    [chainB, 'rainbow'],
    [chainA, 'walnut'],
    [chainB, 'tree']
  ];

  let last = null;
  for (const [player, word] of script) {
    const res = await play(game.gameId, player.id, word);
    assert.equal(res.status, 200, `提交 ${word} 失败: ${res.raw}`);
    last = res.body;
  }

  assert.equal(last.status, 'completed');
  assert.equal(last.chainLength, 10);
  assert.equal(last.isDraw, false);
  assert.equal(last.winner.name, 'ChainA');
  assert.deepEqual(last.wordChain.slice(0, 3), ['apple', 'engine', 'eagle']);

  const scores = {};
  for (const player of last.players) {
    scores[player.name] = player.score;
  }
  assert.equal(scores.ChainA, 26);
  assert.equal(scores.ChainB, 25);

  const rejected = await play(game.gameId, chainA.id, 'ember');
  assert.equal(rejected.status, 400);
  assert.match(rejected.body.message, /not active/);
});

// ================================================================== 词链: 质疑与裁决

test('POST /challenges: 201 pending; 质疑自己的词 → 403; 重复质疑 → 400', async () => {
  const game = await newGame();
  const [alice, bob] = game.players;
  await play(game.gameId, alice.id, 'apple');

  const own = await post(`/api/v1/games/${game.gameId}/challenges`, {
    challengerId: alice.id,
    challengedWordIndex: 0
  });
  assert.equal(own.status, 403);
  assert.match(own.body.message, /cannot challenge your own word/);

  const created = await post(`/api/v1/games/${game.gameId}/challenges`, {
    challengerId: bob.id,
    challengedWordIndex: 0
  });
  assert.equal(created.status, 201, created.raw);
  assert.equal(created.body.challengeStatus, 'pending');
  assert.equal(created.body.challengedWord, 'apple');
  assert.equal(created.body.challengedWordIndex, 0);
  assert.ok(created.body.challengeId);

  const again = await post(`/api/v1/games/${game.gameId}/challenges`, {
    challengerId: bob.id,
    challengedWordIndex: 0
  });
  assert.equal(again.status, 400);
  assert.match(again.body.message, /already pending/);
});

test('POST /challenges: 索引越界 → 400', async () => {
  const game = await newGame();
  const [alice, bob] = game.players;
  await play(game.gameId, alice.id, 'apple');

  const negative = await post(`/api/v1/games/${game.gameId}/challenges`, {
    challengerId: bob.id,
    challengedWordIndex: -1
  });
  assert.equal(negative.status, 400);
  assert.equal(negative.body.details[0].field, 'challengedWordIndex');

  const outOfRange = await post(`/api/v1/games/${game.gameId}/challenges`, {
    challengerId: bob.id,
    challengedWordIndex: 5
  });
  assert.equal(outOfRange.status, 400);
});

test('待裁决期间禁止继续提交单词, 且提示接口也暂停', async () => {
  const game = await newGame();
  const [alice, bob] = game.players;
  await play(game.gameId, alice.id, 'apple');
  const challenge = await post(`/api/v1/games/${game.gameId}/challenges`, {
    challengerId: bob.id,
    challengedWordIndex: 0
  });
  assert.equal(challenge.status, 201);

  const blocked = await play(game.gameId, bob.id, 'engine');
  assert.equal(blocked.status, 400);
  assert.equal(blocked.body.challengeId, challenge.body.challengeId);

  const hint = await get(`/api/v1/games/${game.gameId}/hint`);
  assert.equal(hint.status, 400);
});

test('POST /challenges/:id/resolve: 只有被质疑者能裁决 → 否则 403', async () => {
  const game = await newGame();
  const [alice, bob] = game.players;
  await play(game.gameId, alice.id, 'apple');
  const challenge = await post(`/api/v1/games/${game.gameId}/challenges`, {
    challengerId: bob.id,
    challengedWordIndex: 0
  });

  const wrongResolver = await post(
    `/api/v1/games/${game.gameId}/challenges/${challenge.body.challengeId}/resolve`,
    { playerId: bob.id, isValid: false }
  );
  assert.equal(wrongResolver.status, 403);
  assert.match(wrongResolver.body.message, /Only the challenged player/);
});

test('裁决失败(词其实合法) → challenge_failed, 回合轮转到质疑者, 质疑记为 rejected', async () => {
  const game = await newGame();
  const [alice, bob] = game.players;
  await play(game.gameId, alice.id, 'apple');
  await play(game.gameId, bob.id, 'engine'); // 轮转到 alice

  const challenge = await post(`/api/v1/games/${game.gameId}/challenges`, {
    challengerId: alice.id,
    challengedWordIndex: 1
  });
  assert.equal(challenge.status, 201);

  const resolved = await post(
    `/api/v1/games/${game.gameId}/challenges/${challenge.body.challengeId}/resolve`,
    { playerId: bob.id, isValid: true }
  );
  assert.equal(resolved.status, 200, resolved.raw);
  assert.equal(resolved.body.challengeResult, 'challenge_failed');
  assert.equal(resolved.body.serverValidated, true);
  assert.equal(resolved.body.mismatch, false);
  assert.equal(resolved.body.status, 'active');
  assert.deepEqual(resolved.body.wordChain, ['apple', 'engine']);
  assert.equal(resolved.body.currentPlayerId, alice.id, '质疑失败后由质疑者继续');
  assert.equal(resolved.body.challenge.challengeStatus, 'rejected');

  const again = await post(
    `/api/v1/games/${game.gameId}/challenges/${challenge.body.challengeId}/resolve`,
    { playerId: bob.id, isValid: true }
  );
  assert.equal(again.status, 400);
  assert.match(again.body.message, /already been resolved/);
});

test('裁决: 不存在的 challengeId → 404', async () => {
  const game = await newGame();
  const res = await post(`/api/v1/games/${game.gameId}/challenges/ghost/resolve`, {
    playerId: game.players[0].id,
    isValid: true
  });
  assert.equal(res.status, 404);
  assert.equal(res.body.resource, 'challenge');
});

// ================================================================== 服务层: 客户端谎报与平局

test('服务层: 客户端谎报 isValid 时以服务端复核为准 (mismatch=true)', () => {
  const words = new Set(['apple', 'eagle', 'engine', 'ocean', 'nest']);
  const mutableDictionary = {
    get size() { return words.size; },
    has(raw) { return words.has(String(raw).trim().toLowerCase()); },
    hint() { return { nextLetter: null, hints: [], totalAvailable: 0 }; },
    remove(word) { words.delete(word); }
  };

  let seq = 0;
  const service = createGameService({
    dictionary: mutableDictionary,
    idGenerator: (prefix) => `${prefix}-${++seq}`
  });

  const game = service.createGame({ players: [{ name: 'A' }, { name: 'B' }] });
  const [a, b] = game.players;
  service.playWord(game.gameId, { playerId: a.id, word: 'eagle' });   // e -> e
  service.playWord(game.gameId, { playerId: b.id, word: 'engine' });  // e -> e

  // 让链上的 engine 在服务端复核时变成非法词
  mutableDictionary.remove('engine');

  const challenge = service.challengeWord(game.gameId, {
    challengerId: a.id,
    challengedWordIndex: 1
  });
  assert.equal(challenge.challengeStatus, 'pending');

  const resolved = service.resolveChallenge(game.gameId, challenge.challengeId, {
    playerId: b.id,
    isValid: true // 客户端谎称合法
  });

  assert.equal(resolved.challengeResult, 'challenge_succeeded');
  assert.equal(resolved.serverValidated, false);
  assert.equal(resolved.mismatch, true, '谎报应被记录为 mismatch');
  assert.equal(resolved.status, 'forfeited');
  assert.equal(resolved.winner.id, a.id);
  assert.deepEqual(resolved.wordChain, ['eagle'], '被质疑词及其之后应被撤回');
  assert.equal(resolved.challenge.challengeStatus, 'accepted');
});

test('服务层: 平分时 winner 缺省并标记 isDraw', () => {
  const words = new Set(['ant', 'tea', 'ear']);
  let seq = 0;
  const service = createGameService({
    dictionary: {
      get size() { return words.size; },
      has(raw) { return words.has(String(raw).trim().toLowerCase()); },
      hint() { return { nextLetter: null, hints: [], totalAvailable: 0 }; }
    },
    maxChainLength: 2,
    idGenerator: (prefix) => `${prefix}-${++seq}`
  });

  const game = service.createGame({ players: [{ name: 'A' }, { name: 'B' }] });
  const [a, b] = game.players;
  const afterAnt = service.playWord(game.gameId, { playerId: a.id, word: 'ant' });
  assert.equal(afterAnt.status, 'active');

  const afterTea = service.playWord(game.gameId, { playerId: b.id, word: 'tea' });
  assert.equal(afterTea.status, 'completed');
  assert.equal(afterTea.isDraw, true);
  assert.equal(afterTea.winner, undefined);
  assert.deepEqual(afterTea.players.map((p) => p.score), [3, 3]);
});

// ================================================================== 词链: 加入 / 离开 / 认输 / 历史

const del = (urlPath) => request('DELETE', urlPath);

test('POST /join: 首词落地前可以补员, 新玩家能正常轮转与得分', async () => {
  const game = await newGame(); // Alice, Bob
  const [alice, bob] = game.players;

  const joined = await post(`/api/v1/games/${game.gameId}/join`, { playerName: 'Carol' });
  assert.equal(joined.status, 201, joined.raw);
  assert.equal(joined.body.gameId, game.gameId);
  assert.deepEqual(joined.body.players.map((p) => p.name), ['Alice', 'Bob', 'Carol']);
  assert.deepEqual(joined.body.wordChain, []);
  assert.equal(joined.body.currentTurnIndex, 0);
  assert.equal(joined.body.player.name, 'Carol');
  assert.ok(
    String(joined.headers.location).endsWith(`/api/v1/games/${game.gameId}/players/${joined.body.player.id}`),
    `Location 应指向新玩家资源, 实际 ${joined.headers.location}`
  );

  // 第 4 人也能加进来, 之后满员
  const fourth = await post(`/api/v1/games/${game.gameId}/join`, { playerName: 'Dave' });
  assert.equal(fourth.status, 201, fourth.raw);
  assert.equal(fourth.body.playerCount, 4);

  const fifth = await post(`/api/v1/games/${game.gameId}/join`, { playerName: 'Eve' });
  assert.equal(fifth.status, 409, fifth.raw);
  assert.equal(fifth.body.error, 'Conflict');
  assert.equal(fifth.body.maxPlayers, 4);

  // 补进来的人轮到自己时能出词并得分
  const carol = joined.body.player;
  await play(game.gameId, alice.id, 'apple');
  await play(game.gameId, bob.id, 'engine');
  const byCarol = await play(game.gameId, carol.id, 'eagle');
  assert.equal(byCarol.status, 200, byCarol.raw);
  assert.equal(byCarol.body.players.find((p) => p.id === carol.id).score, 5);
});

test('POST /join: 兼容 name 别名; 重名 409 / 空名 400 / 首词之后 403 / 未知对局 404', async () => {
  const aliasGame = await newGame(['Ann', 'Bob']);
  const aliased = await post(`/api/v1/games/${aliasGame.gameId}/join`, { name: 'Cid' });
  assert.equal(aliased.status, 201, aliased.raw);
  assert.equal(aliased.body.player.name, 'Cid');

  const game = await newGame();
  // 名字大小写与首尾空格不敏感
  const dup = await post(`/api/v1/games/${game.gameId}/join`, { playerName: '  BOB ' });
  assert.equal(dup.status, 409, dup.raw);
  assert.equal(dup.body.error, 'Conflict');
  assert.match(dup.body.message, /Duplicate player name/);

  const empty = await post(`/api/v1/games/${game.gameId}/join`, { playerName: '   ' });
  assert.equal(empty.status, 400, empty.raw);
  assert.equal(empty.body.details[0].field, 'playerName');

  const missingGame = await post('/api/v1/games/nope/join', { playerName: 'Zed' });
  assert.equal(missingGame.status, 404, missingGame.raw);
  assert.equal(missingGame.body.resource, 'game');

  await play(game.gameId, game.players[0].id, 'apple');
  const tooLate = await post(`/api/v1/games/${game.gameId}/join`, { playerName: 'Carol' });
  assert.equal(tooLate.status, 403, tooLate.raw);
  assert.equal(tooLate.body.error, 'Forbidden');
  assert.equal(tooLate.body.chainLength, 1);
});

test('DELETE /players/:id: 两人局有人离席 → success:true, 留下的人按对手弃赛获胜', async () => {
  const game = await newGame();
  const [alice, bob] = game.players;
  await play(game.gameId, alice.id, 'apple');

  const left = await del(`/api/v1/games/${game.gameId}/players/${bob.id}`);
  assert.equal(left.status, 200, left.raw);
  assert.equal(left.body.success, true);
  assert.equal(left.body.ended, true);
  assert.equal(left.body.status, 'forfeited');
  assert.equal(left.body.endedBy, 'player_left');
  assert.equal(left.body.winner.id, alice.id);
  assert.equal(left.body.leftPlayerId, bob.id);
  assert.deepEqual(left.body.players.map((p) => p.id), [alice.id]);
  assert.deepEqual(left.body.departures.map((d) => [d.playerId, d.reason]), [[bob.id, 'left']]);

  // 终局之后不能再出词
  const after = await play(game.gameId, alice.id, 'engine');
  assert.equal(after.status, 400, after.raw);
  assert.match(after.body.message, /not active/);
});

test('DELETE /players/:id: 三人局离席后对局继续, 回合指针不错位', async () => {
  const game = await newGame(['Ann', 'Bob', 'Cid']);
  const [ann, bob, cid] = game.players;
  await play(game.gameId, ann.id, 'apple'); // 轮到 Bob

  const left = await del(`/api/v1/games/${game.gameId}/players/${bob.id}`);
  assert.equal(left.status, 200, left.raw);
  assert.equal(left.body.ended, false);
  assert.equal(left.body.status, 'active');
  assert.equal(left.body.currentTurnIndex, 1);
  assert.equal(left.body.currentPlayerId, cid.id, 'Bob 离席后应顺延到 Cid, 不能退回 Ann');

  const byCid = await play(game.gameId, cid.id, 'engine');
  assert.equal(byCid.status, 200, byCid.raw);
  assert.equal(byCid.body.currentPlayerId, ann.id);

  // 离席的人不再是这张桌子的玩家
  const ghost = await play(game.gameId, bob.id, 'eagle');
  assert.equal(ghost.status, 404, ghost.raw);
  assert.equal(ghost.body.resource, 'player');
});

test('DELETE /players/:id: 待裁决质疑期间 400; 未知玩家 404; 裁决恢复后才允许离席', async () => {
  const game = await newGame();
  const [alice, bob] = game.players;
  await play(game.gameId, alice.id, 'apple');
  await play(game.gameId, bob.id, 'engine');
  const challenge = await post(`/api/v1/games/${game.gameId}/challenges`, {
    challengerId: alice.id,
    challengedWordIndex: 1
  });
  assert.equal(challenge.status, 201, challenge.raw);

  const blocked = await del(`/api/v1/games/${game.gameId}/players/${bob.id}`);
  assert.equal(blocked.status, 400, blocked.raw);
  assert.equal(blocked.body.error, 'ValidationError');
  assert.equal(blocked.body.challengeId, challenge.body.challengeId);

  const ghost = await del(`/api/v1/games/${game.gameId}/players/ghost`);
  assert.equal(ghost.status, 404, ghost.raw);
  assert.equal(ghost.body.resource, 'player');

  const missingGame = await del('/api/v1/games/nope/players/x');
  assert.equal(missingGame.status, 404, missingGame.raw);
  assert.equal(missingGame.body.resource, 'game');

  // 质疑成立(词其实合法) -> 对局回到 active, 此时才允许离席
  const resolved = await post(
    `/api/v1/games/${game.gameId}/challenges/${challenge.body.challengeId}/resolve`,
    { playerId: bob.id, isValid: true }
  );
  assert.equal(resolved.status, 200, resolved.raw);
  assert.equal(resolved.body.challengeResult, 'challenge_failed');

  const leftNow = await del(`/api/v1/games/${game.gameId}/players/${bob.id}`);
  assert.equal(leftNow.status, 200, leftNow.raw);
  assert.equal(leftNow.body.ended, true);

  // 终局之后不能再离席
  const afterEnd = await del(`/api/v1/games/${game.gameId}/players/${alice.id}`);
  assert.equal(afterEnd.status, 400, afterEnd.raw);
  assert.match(afterEnd.body.message, /not active/);
});

test('POST /forfeit: 主动认输立即结束整局, 对手获胜', async () => {
  const game = await newGame(['Fiona', 'Gus']);
  const [fiona, gus] = game.players;
  await play(game.gameId, fiona.id, 'apple'); // Fiona 5 分

  const forfeit = await post(`/api/v1/games/${game.gameId}/forfeit`, {
    playerId: gus.id,
    reason: 'bandwidth dropped'
  });
  assert.equal(forfeit.status, 200, forfeit.raw);
  assert.equal(forfeit.body.success, true);
  assert.equal(forfeit.body.ended, true);
  assert.equal(forfeit.body.status, 'forfeited');
  assert.equal(forfeit.body.forfeitedBy, gus.id);
  assert.equal(forfeit.body.endedBy, 'player_forfeited');
  assert.equal(forfeit.body.winner.id, fiona.id);
  assert.equal(forfeit.body.isDraw, false);
  assert.deepEqual(forfeit.body.departures.map((d) => d.reason), ['forfeited']);

  // 认输的人已不在牌桌上, 对局也不再 active
  const replay = await play(game.gameId, gus.id, 'engine');
  assert.equal(replay.status, 404, replay.raw);
  const stillPlaying = await play(game.gameId, fiona.id, 'engine');
  assert.equal(stillPlaying.status, 400, stillPlaying.raw);
});

test('POST /forfeit: 三人局有人认输同样结束整局, 胜者是剩余玩家里分数最高的', async () => {
  const game = await newGame(['Hal', 'Ivy', 'Jo']);
  const [hal, ivy, jo] = game.players;
  await play(game.gameId, hal.id, 'apple'); // Hal 5 分
  await play(game.gameId, ivy.id, 'engine'); // Ivy 6 分
  await play(game.gameId, jo.id, 'ember'); // Jo 5 分

  const forfeit = await post(`/api/v1/games/${game.gameId}/forfeit`, { playerId: jo.id });
  assert.equal(forfeit.status, 200, forfeit.raw);
  assert.equal(forfeit.body.ended, true);
  assert.equal(forfeit.body.status, 'forfeited');
  assert.equal(forfeit.body.endedBy, 'player_forfeited');
  assert.deepEqual(forfeit.body.players.map((p) => p.name), ['Hal', 'Ivy']);
  assert.equal(forfeit.body.winner.id, ivy.id, 'Ivy 6 分是剩余玩家里最高的');
  assert.equal(forfeit.body.isDraw, false);
  assert.deepEqual(forfeit.body.departures.map((d) => [d.name, d.reason, d.score]), [['Jo', 'forfeited', 5]]);
});

test('POST /forfeit: 认输后剩余玩家平分 → 判和, winner 缺省', async () => {
  const game = await newGame(['Kim', 'Lee', 'Mo']);
  const [kim, lee, mo] = game.players;
  await play(game.gameId, kim.id, 'apple'); // 5 分
  await play(game.gameId, lee.id, 'ember'); // 5 分

  const forfeit = await post(`/api/v1/games/${game.gameId}/forfeit`, { playerId: mo.id });
  assert.equal(forfeit.status, 200, forfeit.raw);
  assert.equal(forfeit.body.status, 'forfeited');
  assert.equal(forfeit.body.winner, undefined);
  assert.equal(forfeit.body.isDraw, true, '认输后剩余玩家平分应判和');
});

test('POST /forfeit: 待裁决质疑期间不能认输; 未知玩家 404', async () => {
  const game = await newGame();
  const [alice, bob] = game.players;
  await play(game.gameId, alice.id, 'apple');
  const challenge = await post(`/api/v1/games/${game.gameId}/challenges`, {
    challengerId: bob.id,
    challengedWordIndex: 0
  });
  assert.equal(challenge.status, 201, challenge.raw);

  const blocked = await post(`/api/v1/games/${game.gameId}/forfeit`, { playerId: alice.id });
  assert.equal(blocked.status, 400, blocked.raw);
  assert.equal(blocked.body.challengeId, challenge.body.challengeId);

  const ghost = await post(`/api/v1/games/${game.gameId}/forfeit`, { playerId: 'ghost' });
  assert.equal(ghost.status, 404, ghost.raw);
  assert.equal(ghost.body.resource, 'player');
});

test('GET /history: 词链带作者与时间, 质疑与事件流水齐全', async () => {
  const game = await newGame();
  const [alice, bob] = game.players;
  await play(game.gameId, alice.id, 'apple');
  await play(game.gameId, bob.id, 'engine');
  const challenge = await post(`/api/v1/games/${game.gameId}/challenges`, {
    challengerId: alice.id,
    challengedWordIndex: 1
  });
  await post(`/api/v1/games/${game.gameId}/challenges/${challenge.body.challengeId}/resolve`, {
    playerId: bob.id,
    isValid: true
  });

  const res = await get(`/api/v1/games/${game.gameId}/history`);
  assert.equal(res.status, 200, res.raw);
  assert.equal(res.body.gameId, game.gameId);
  assert.deepEqual(res.body.wordChain.map((entry) => entry.word), ['apple', 'engine']);
  assert.deepEqual(res.body.wordChain.map((entry) => entry.playerId), [alice.id, bob.id]);
  assert.deepEqual(res.body.wordChain.map((entry) => entry.playerName), ['Alice', 'Bob']);
  assert.ok(res.body.wordChain.every((entry) => typeof entry.submittedAt === 'string'));
  assert.equal(res.body.chainLength, 2);
  assert.equal(res.body.challenges.length, 1);
  assert.equal(res.body.challenges[0].challengeId, challenge.body.challengeId);
  assert.equal(res.body.challenges[0].challengeResult, 'challenge_failed');

  const types = res.body.events.map((event) => event.type);
  assert.deepEqual(types, [
    'game_created',
    'word_played',
    'word_played',
    'challenge_raised',
    'challenge_resolved'
  ]);
  assert.deepEqual(res.body.events.map((event) => event.seq), [1, 2, 3, 4, 5]);
});

test('服务层 GET /history: 作者离席后仍能回溯当时提交的词是谁写的', () => {
  const service = createGameService({ maxChainLength: 10 });
  const game = service.createGame({ players: [{ name: 'Nia' }, { name: 'Om' }, { name: 'Pim' }] });
  service.playWord(game.gameId, { playerId: game.players[0].id, word: 'apple' });
  service.playWord(game.gameId, { playerId: game.players[1].id, word: 'engine' });

  const before = service.getHistory(game.gameId);
  assert.deepEqual(before.wordChain.map((entry) => entry.word), ['apple', 'engine']);
  assert.deepEqual(before.wordChain.map((entry) => entry.playerName), ['Nia', 'Om']);

  const left = service.leaveGame(game.gameId, game.players[1].id);
  assert.equal(left.ended, false, '三人局走掉一人还应继续');

  const afterLeave = service.getHistory(game.gameId);
  assert.equal(
    afterLeave.wordChain[1].playerName,
    'Om',
    '作者已离席时仍要能回溯当时写下的词是谁提交的'
  );
  assert.deepEqual(afterLeave.departures.map((d) => [d.name, d.reason]), [['Om', 'left']]);
});

test('服务层 GET /history: 质疑成立后撤回的词不在 wordChain, 但 events 里能还原', () => {
  // 可注入的词库: 提交时放行 engine, 裁决时改判非法 —— 用来走通 challenge_succeeded 分支
  const allowed = new Set(['apple', 'engine']);
  const service = createGameService({ maxChainLength: 10, dictionary: { has: (w) => allowed.has(w) } });
  const game = service.createGame({ players: [{ name: 'Nia' }, { name: 'Om' }] });
  service.playWord(game.gameId, { playerId: game.players[0].id, word: 'apple' });
  service.playWord(game.gameId, { playerId: game.players[1].id, word: 'engine' });

  const raised = service.challengeWord(game.gameId, {
    challengerId: game.players[0].id,
    challengedWordIndex: 1
  });
  assert.equal(raised.challengedWord, 'engine');

  allowed.delete('engine');
  const resolved = service.resolveChallenge(game.gameId, raised.challengeId, {
    playerId: game.players[1].id,
    isValid: true // 被质疑者谎报合法, 以服务端复核为准
  });
  assert.equal(resolved.challengeResult, 'challenge_succeeded');

  const history = service.getHistory(game.gameId);
  assert.deepEqual(history.wordChain.map((entry) => entry.word), ['apple'], '撤回的词要从链上抹掉');
  assert.equal(history.chainLength, 1);
  assert.equal(history.status, 'forfeited');
  assert.equal(history.endedBy, 'challenge_succeeded');
  assert.equal(history.winner.id, game.players[0].id);

  const played = history.events.filter((event) => event.type === 'word_played');
  assert.deepEqual(played.map((event) => event.word), ['apple', 'engine'], '事件流水保留完整过程');
  assert.deepEqual(
    history.events.map((event) => event.type),
    ['game_created', 'word_played', 'word_played', 'challenge_raised', 'challenge_resolved', 'game_forfeited']
  );
  const lastEvent = history.events[history.events.length - 1];
  assert.equal(lastEvent.reason, 'challenge_succeeded');
  assert.deepEqual(
    lastEvent.revokedWords.map((w) => [w.index, w.word, w.playerId]),
    [[1, 'engine', game.players[1].id]],
    '被撤回的词要在事件里留下痕迹'
  );
});

test('GET /history: 不存在的对局 → 404', async () => {
  const res = await get('/api/v1/games/nope/history');
  assert.equal(res.status, 404, res.raw);
  assert.equal(res.body.error, 'NotFound');
  assert.equal(res.body.resource, 'game');
});

test('POST /challenges: 按设计文档写 playerId 也能质疑 (challengerId 的别名)', async () => {
  const game = await newGame();
  const [alice, bob] = game.players;
  await play(game.gameId, alice.id, 'apple');
  await play(game.gameId, bob.id, 'engine');

  const byAlias = await post(`/api/v1/games/${game.gameId}/challenges`, {
    playerId: alice.id,
    challengedWordIndex: 1
  });
  assert.equal(byAlias.status, 201, byAlias.raw);
  assert.equal(byAlias.body.challengerId, alice.id);
  assert.equal(byAlias.body.playerId, alice.id);
  assert.equal(byAlias.body.challengeStatus, 'pending');

  // 两个字段都不给 -> 404 resource=player (与 playWord 缺 playerId 的行为一致)
  const missing = await post(`/api/v1/games/${game.gameId}/challenges`, { challengedWordIndex: 1 });
  assert.equal(missing.status, 404, missing.raw);
  assert.equal(missing.body.resource, 'player');
});

// ================================================================== 第二轮: 分页 / 增量 / 文档契约

test('GET /api/v1/games: 游标分页逐页翻不重不漏, total 稳定', async () => {
  const newest = await newGame(['PageNewA', 'PageNewB']);

  const first = await get('/api/v1/games?limit=2');
  assert.equal(first.status, 200, first.raw);
  assert.equal(first.body.games.length, 2);
  assert.equal(first.body.limit, 2);
  assert.equal(first.body.count, 2);
  assert.equal(first.body.hasMore, true);
  assert.ok(first.body.nextCursor, '还有下一页时必须给出 nextCursor');
  assert.equal(first.body.games[0].gameId, newest.gameId, '列表应按开局时间倒序, 最新开局在最前');
  assert.ok(first.body.remaining <= first.body.total);

  const second = await get(`/api/v1/games?limit=2&cursor=${encodeURIComponent(first.body.nextCursor)}`);
  assert.equal(second.status, 200, second.raw);
  assert.equal(second.body.total, first.body.total, '翻页过程中 total(状态过滤后的全量)不应变化');
  assert.ok(second.body.remaining < first.body.remaining, '第二页的 remaining 必须比第一页小');
  const overlap = second.body.games.filter(
    (game) => first.body.games.some((earlier) => earlier.gameId === game.gameId)
  );
  assert.deepEqual(overlap, [], '第二页不能重复第一页的条目');

  // 一直翻到底: 不能出现重复, 且恰好覆盖全量
  const seen = new Set(first.body.games.concat(second.body.games).map((game) => game.gameId));
  let cursor = second.body.nextCursor;
  let guard = 0;
  while (cursor && guard < 200) {
    const page = await get(`/api/v1/games?limit=2&cursor=${encodeURIComponent(cursor)}`);
    assert.equal(page.status, 200, page.raw);
    for (const item of page.body.games) {
      assert.equal(seen.has(item.gameId), false, `游标翻页出现重复条目: ${item.gameId}`);
      seen.add(item.gameId);
    }
    cursor = page.body.hasMore ? page.body.nextCursor : null;
    guard += 1;
  }
  assert.equal(seen.size, first.body.total, '翻完之后应当恰好覆盖全量对局, 不重不漏');
});

test('GET /api/v1/games: 非法 cursor → 400, 而不是静默从头开始', async () => {
  const bad = await get('/api/v1/games?cursor=not-a-cursor');
  assert.equal(bad.status, 400, bad.raw);
  assert.equal(bad.body.error, 'ValidationError');
  assert.equal(bad.body.details[0].field, 'cursor');

  const oversized = await get('/api/v1/games?cursor=99999999999999999999999999');
  assert.equal(oversized.status, 400);
  assert.equal(oversized.body.details[0].field, 'cursor');

  // 空的 cursor 视为没传(便于客户端直接拼 ?cursor=)
  const empty = await get('/api/v1/games?cursor=&limit=1');
  assert.equal(empty.status, 200);
  assert.equal(empty.body.games.length, 1);
});

test('GET /history: ?since/?limit 增量拉事件, latestSeq/nextSince 可续拉', async () => {
  const game = await newGame(['EvA', 'EvB']);
  await play(game.gameId, game.players[0].id, 'apple');
  await play(game.gameId, game.players[1].id, 'engine');

  const full = await get(`/api/v1/games/${game.gameId}/history`);
  assert.equal(full.status, 200, full.raw);
  assert.equal(full.body.latestSeq, full.body.eventsTotal, '统计出来的事件总数应与最新 seq 一致');
  assert.ok(full.body.eventsTotal >= 3, '至少 game_created + 2 次出词');
  assert.equal(full.body.hasMoreEvents, false);
  assert.equal(full.body.nextSince, full.body.latestSeq);

  const sinceOne = await get(`/api/v1/games/${game.gameId}/history?since=1`);
  assert.equal(sinceOne.status, 200, sinceOne.raw);
  assert.ok(sinceOne.body.events.every((event) => event.seq > 1), 'since 之后不该再出现旧事件');
  assert.equal(sinceOne.body.events.length, full.body.eventsTotal - 1);
  assert.equal(sinceOne.body.nextSince, full.body.latestSeq);
  assert.equal(sinceOne.body.hasMoreEvents, false, '没有 limit 时应当一次给完');

  const limited = await get(`/api/v1/games/${game.gameId}/history?since=1&limit=1`);
  assert.equal(limited.body.events.length, 1);
  assert.equal(limited.body.hasMoreEvents, true, '被 limit 截断时必须告知还有更多');
  assert.equal(limited.body.nextSince, limited.body.events[0].seq);

  // 用 nextSince 续拉, 接上一次没给完的部分
  const rest = await get(`/api/v1/games/${game.gameId}/history?since=${limited.body.nextSince}`);
  assert.ok(rest.body.events.length > 0);
  assert.ok(rest.body.events.every((event) => event.seq > limited.body.nextSince));
  assert.equal(rest.body.events[rest.body.events.length - 1].seq, full.body.latestSeq);

  // 写错的游标要报错, 而不是悄悄给全量
  const badSince = await get(`/api/v1/games/${game.gameId}/history?since=abc`);
  assert.equal(badSince.status, 400, badSince.raw);
  assert.equal(badSince.body.details[0].field, 'since');
});

test('403 响应带设计文档的 requiredRole; 裁决响应带 gameStatus 别名', async () => {
  const game = await newGame(['RoleA', 'RoleB']);
  const [alice, bob] = game.players;

  // 设计文档 403: "Player trying to play when it's not their turn"
  const outOfTurn = await play(game.gameId, bob.id, 'apple');
  assert.equal(outOfTurn.status, 403, outOfTurn.raw);
  assert.equal(outOfTurn.body.requiredRole, 'player');

  await play(game.gameId, alice.id, 'apple');

  // 设计文档 §2 的角色: 质疑者必须是"对手", 不能是词作者
  const ownWord = await post(`/api/v1/games/${game.gameId}/challenges`, {
    playerId: alice.id,
    challengedWordIndex: 0
  });
  assert.equal(ownWord.status, 403, ownWord.raw);
  assert.equal(ownWord.body.requiredRole, 'challenger');

  const raised = await post(`/api/v1/games/${game.gameId}/challenges`, {
    playerId: bob.id,
    challengedWordIndex: 0
  });
  assert.equal(raised.status, 201, raised.raw);

  // 只有被质疑的一方(Defender)能裁决: 质疑者自己说了不算
  const challengerResolves = await post(
    `/api/v1/games/${game.gameId}/challenges/${raised.body.challengeId}/resolve`,
    { playerId: bob.id, isValid: true }
  );
  assert.equal(challengerResolves.status, 403, challengerResolves.raw);
  assert.equal(challengerResolves.body.requiredRole, 'defender');

  const resolved = await post(
    `/api/v1/games/${game.gameId}/challenges/${raised.body.challengeId}/resolve`,
    { playerId: alice.id, isValid: true }
  );
  assert.equal(resolved.status, 200, resolved.raw);
  assert.equal(resolved.body.gameStatus, resolved.body.status, '设计文档 §7 的 gameStatus 与 status 同值');
  assert.equal(resolved.body.gameStatus, 'active');
});

test('POST /api/v1/games: 设计文档 §1 的 { playerName } 单人开桌, 之后 join 补员', async () => {
  const created = await post('/api/v1/games', { playerName: '  Solo  ' });
  assert.equal(created.status, 201, created.raw);
  assert.equal(created.body.players.length, 1);
  assert.equal(created.body.players[0].name, 'Solo');
  assert.equal(created.body.status, 'active');
  assert.equal(created.body.currentTurnIndex, 0);
  assert.deepEqual(created.body.wordChain, []);
  assert.ok(
    String(created.headers.location).endsWith(`/api/v1/games/${created.body.gameId}`),
    'Location 头应指向新对局'
  );

  const joined = await post(`/api/v1/games/${created.body.gameId}/join`, { playerName: 'Duo' });
  assert.equal(joined.status, 201, joined.raw);
  assert.equal(joined.body.players.length, 2);

  // 单人也能直接出首词(链上还没有词, 不需要衔接字母)
  const played = await play(created.body.gameId, created.body.players[0].id, 'apple');
  assert.equal(played.status, 200, played.raw);

  // players 数组的 2..4 人契约没有被悄悄放宽
  const stillGuarded = await post('/api/v1/games', { players: [{ name: 'Solo' }] });
  assert.equal(stillGuarded.status, 400, stillGuarded.raw);
  assert.equal(stillGuarded.body.details[0].field, 'players');

  // 空 playerName 按字段报错, 与 join 的字段口径一致
  const emptyName = await post('/api/v1/games', { playerName: '   ' });
  assert.equal(emptyName.status, 400, emptyName.raw);
  assert.equal(emptyName.body.details[0].field, 'playerName');
});

test('CORS: 白名单来源放行并暴露限流响应头, 其它来源不加头', async () => {
  const allowed = await get('/api/v1/games?limit=1', { Origin: 'https://client.example' });
  assert.equal(allowed.status, 200, allowed.raw);
  assert.equal(allowed.headers['access-control-allow-origin'], 'https://client.example');
  assert.equal(allowed.headers.vary, 'Origin');
  // 浏览器默认读不到这些头, 不暴露的话客户端无法按 Retry-After 自己退避
  assert.match(allowed.headers['access-control-expose-headers'], /Retry-After/);
  assert.match(allowed.headers['access-control-expose-headers'], /X-RateLimit-Hour-Remaining/);

  const rejected = await get('/api/v1/games?limit=1', { Origin: 'https://evil.example' });
  assert.equal(rejected.status, 200, '白名单外的来源不加跨域头即可, 不必额外报错');
  assert.equal(rejected.headers['access-control-allow-origin'], undefined);

  const sameOrigin = await get('/api/v1/games?limit=1');
  assert.equal(sameOrigin.headers['access-control-allow-origin'], undefined);
});

test('CORS: 预检 OPTIONS 返回 204 且不消耗限流配额', async () => {
  wordChainRateLimiter.reset();
  const before = wordChainRateLimiter.getStats().allowed;

  const preflight = await request('OPTIONS', '/api/v1/games', undefined, {
    Origin: 'https://client.example',
    'Access-Control-Request-Method': 'POST',
    'Access-Control-Request-Headers': 'content-type'
  });

  assert.equal(preflight.status, 204, preflight.raw);
  assert.equal(preflight.headers['access-control-allow-origin'], 'https://client.example');
  assert.match(preflight.headers['access-control-allow-methods'], /POST/);
  assert.equal(preflight.headers['access-control-allow-headers'], 'content-type');
  assert.ok(Number(preflight.headers['access-control-max-age']) > 0);

  // 关键: 浏览器每次写操作前都要预检; 若预检也计入配额, 10 次/分钟会被浏览器自己撞满
  assert.equal(
    wordChainRateLimiter.getStats().allowed,
    before,
    '预检请求不能占用每分钟/每小时配额'
  );
  wordChainRateLimiter.reset();
});

test('CORS: 未配置时完全关闭; 配置了 * 也不会连带放开 Cookie', async () => {
  const disabled = createCorsMiddleware();
  assert.equal(disabled.cors.enabled, false);
  const res = fakeRes();
  let next = false;
  disabled(
    fakeReq('1.2.3.4', { origin: 'https://client.example', 'access-control-request-method': 'POST' }),
    res,
    () => { next = true; }
  );
  assert.equal(next, true, '关闭时应当直接放行');
  assert.equal(res.headers['access-control-allow-origin'], undefined);
  assert.equal(res.statusCode, 0);

  const wildcard = createCorsMiddleware({ origin: '*', credentials: true });
  assert.deepEqual(wildcard.cors.origins, ['*']);
  assert.equal(wildcard.cors.allowCredentials, false, "通配符与凭据不能共存(浏览器会直接拒绝)");
  assert.equal(createCorsMiddleware({ origin: 'none' }).cors.enabled, false);
});

test('CORS: /health 暴露当前跨域配置', async () => {
  const res = await get('/health');
  assert.equal(res.status, 200, res.raw);
  const cors = res.body.configuration.wordChainCors;
  assert.equal(cors.enabled, true);
  assert.deepEqual(cors.origins, ['https://client.example']);
  assert.equal(cors.allowCredentials, false);
  assert.ok(cors.maxAgeSeconds > 0);
});

// ================================================================== 限流: 真实 HTTP 路径

// 这两个限流用例是"预期内的 429", 用 allow429 包起来; 其余用例拿到 429 会被 after() 判红
test('HTTP: /api/v1 超出配额返回 429 + Retry-After, 窗口过后恢复', allow429(async () => {
  let fakeNow = 1700000000000;
  wordChainRateLimiter.reset();
  wordChainRateLimiter.configure({ max: 2, windowMs: 60000, clock: () => fakeNow });

  try {
    const first = await get('/api/v1/games?limit=1');
    const second = await get('/api/v1/games?limit=1');
    assert.equal(first.status, 200);
    assert.equal(second.status, 200);
    assert.equal(first.headers['x-ratelimit-limit'], '2');

    const third = await get('/api/v1/games?limit=1');
    assert.equal(third.status, 429, third.raw);
    assert.equal(third.body.error, 'RateLimitExceeded');
    assert.equal(third.body.message, 'Too many requests. Please slow down.');
    assert.ok(Number(third.body.retryAfter) >= 1);
    assert.ok(Number(third.headers['retry-after']) >= 1);
    assert.equal(third.headers['x-ratelimit-remaining'], '0');
    // 小时额度还很充足, 拦下来的是分钟档
    assert.equal(third.body.scope, 'minute');
    assert.equal(third.headers['x-ratelimit-scope'], 'minute');

    // 伪造 X-Forwarded-For 不能绕过 (默认不信任代理头)
    const spoofed = await get('/api/v1/games?limit=1', { 'X-Forwarded-For': '203.0.113.7' });
    assert.equal(spoofed.status, 429);

    // 其它端点(不在 /api/v1 下)不受限流影响: 既不 429, 也不带限流响应头。
    // (降级状态会污染 /health 的状态码, 与本用例无关, 先复位)
    degradedMode.enabled = false;
    const health = await get('/health');
    assert.equal(health.status, 200);
    assert.equal(health.headers['x-ratelimit-limit'], undefined);

    // 窗口推进后恢复
    fakeNow += 60001;
    const afterWindow = await get('/api/v1/games?limit=1');
    assert.equal(afterWindow.status, 200, afterWindow.raw);

    const limiterStats = wordChainRateLimiter.getStats();
    assert.equal(limiterStats.blocked, 2, '只应有 2 次预期内的 429');
    assert.equal(limiterStats.blockedByMinute, 2);
    assert.equal(limiterStats.blockedByHour, 0);
  } finally {
    wordChainRateLimiter.configure({
      max: 1000000,
      windowMs: 60000,
      hourlyMax: 1000000,
      hourWindowMs: 3600000,
      clock: () => Date.now()
    });
    wordChainRateLimiter.reset();
  }
}));

test('HTTP: 分钟额度充足但小时额度耗尽 -> 429 且 scope=hour, Retry-After 按小时窗口', allow429(async () => {
  let fakeNow = 1700000000000;
  wordChainRateLimiter.reset();
  wordChainRateLimiter.configure({
    max: 1000000,
    windowMs: 60000,
    hourlyMax: 3,
    hourWindowMs: 3600000,
    clock: () => fakeNow
  });

  try {
    const allowed = [];
    for (let i = 0; i < 3; i += 1) {
      allowed.push(await get(`/api/v1/games?limit=${i + 1}`));
    }
    assert.deepEqual(allowed.map((res) => res.status), [200, 200, 200]);
    assert.equal(allowed[0].headers['x-ratelimit-hour-limit'], '3');
    assert.equal(allowed[0].headers['x-ratelimit-hour-remaining'], '2');
    assert.equal(allowed[2].headers['x-ratelimit-hour-remaining'], '0');

    const blocked = await get('/api/v1/games?limit=1');
    assert.equal(blocked.status, 429, blocked.raw);
    assert.equal(blocked.body.scope, 'hour');
    assert.equal(blocked.headers['x-ratelimit-scope'], 'hour');
    // 分钟档还很充足(100 万), 所以 Retry-After 必须按小时窗口给: 约 1 小时, 而不是 60 秒
    assert.ok(
      Number(blocked.body.retryAfter) > 3000,
      `retryAfter 应当反映小时窗口剩余, 实际 ${blocked.body.retryAfter}`
    );
    assert.ok(
      Number(blocked.headers['x-ratelimit-remaining']) > 900000,
      '分钟档不应是瓶颈'
    );

    // 只等分钟窗口翻篇 -> 仍然 429 (这正是"按 Retry-After 重试却再撞 429"的场景)
    fakeNow += 60001;
    const stillBlocked = await get('/api/v1/games?limit=1');
    assert.equal(stillBlocked.status, 429);
    assert.equal(stillBlocked.body.scope, 'hour');

    // 按服务端给的 retryAfter 等待 -> 一次就恢复
    fakeNow += Number(blocked.body.retryAfter) * 1000;
    const recovered = await get('/api/v1/games?limit=1');
    assert.equal(recovered.status, 200, recovered.raw);

    const stats = wordChainRateLimiter.getStats();
    assert.equal(stats.blocked, 2);
    assert.equal(stats.blockedByHour, 2);
    assert.equal(stats.blockedByMinute, 0);
  } finally {
    wordChainRateLimiter.configure({
      max: 1000000,
      windowMs: 60000,
      hourlyMax: 1000000,
      hourWindowMs: 3600000,
      clock: () => Date.now()
    });
    wordChainRateLimiter.reset();
  }
}));

// ================================================================== 第三轮: 429 code / burst 档 / 机器人 / 名字净化
// 本段把这一轮新增的能力固化成回归防线:
//   - 限流 429 响应体的 JSON 数字 code: 429400 (设计文档 #### 429400 - Rate Limiting);
//   - "可疑请求模式 / bot 检测" 突发档 (默认关闭, 只能显式打开);
//   - 机器人玩家 isBot (设计文档 Data Structures 的 Player.isBot, 单人也能玩) 与 bot_stuck 判负语义;
//   - 玩家名净化 (设计文档 Security Considerations #6 XSS prevention)。
// 口径: 预期内的 429 一律用 allow429(...) 包住, 其余用例拿到 429 会被 after() 判红。

test('限流: 突发档默认关闭(burstMax=0), 429 错误码常量是数字 429400', () => {
  // 局部 require: 只为本用例取常量, 不改动文件顶部既有的 require 块
  const { RATE_LIMIT_ERROR_CODE, DEFAULT_BURST_MAX, DEFAULT_BURST_WINDOW_MS } = require('../src/rate-limit');
  assert.strictEqual(RATE_LIMIT_ERROR_CODE, 429400);
  assert.strictEqual(DEFAULT_BURST_WINDOW_MS, 5000);
  // 默认关闭是承重设计: 默认一旦非 0, 正常功能流量(毫秒级连打)会被打成大面积非预期 429
  assert.strictEqual(DEFAULT_BURST_MAX, 0, '突发档默认必须关闭');

  const defaults = createRateLimiter().getStats();
  assert.strictEqual(defaults.burstMax, 0);
  assert.strictEqual(defaults.burstWindowMs, 5000);
  assert.strictEqual(defaults.burstTrackedKeys, 0);

  // 默认配置下连打 50 次: 既不建突发桶, 也不下发任何 burst 响应头 (分钟档另抬, 避免撞上默认 10 次)
  const relaxed = createRateLimiter({ max: 100000, hourlyMax: 100000 });
  for (let i = 0; i < 50; i += 1) {
    const res = fakeRes();
    let next = false;
    relaxed(fakeReq('10.20.30.40'), res, () => { next = true; });
    assert.strictEqual(next, true, `第 ${i + 1} 次不应被拒绝`);
    assert.strictEqual(res.statusCode, 0);
    assert.strictEqual(res.headers['x-ratelimit-burst-limit'], undefined);
  }
  const stats = relaxed.getStats();
  assert.strictEqual(stats.blocked, 0);
  assert.strictEqual(stats.blockedByBurst, 0);
  assert.strictEqual(stats.burstTrackedKeys, 0, '关闭时不建突发桶');
});

test('GET /health 与 /metrics: 暴露突发档默认配置与 blockedByBurst', async () => {
  // 上游失败用例会打开降级模式, 这里先复位, 让断言只依赖限流配置
  degradedMode.enabled = false;
  const health = await get('/health');
  assert.equal(health.status, 200, health.raw);
  const rateLimit = health.body.configuration.wordChainRateLimit;
  assert.equal(rateLimit.burstMax, 0, '突发档默认关闭必须体现在 /health');
  assert.equal(rateLimit.burstWindowMs, 5000);

  const metrics = await get('/metrics');
  assert.equal(metrics.status, 200, metrics.raw);
  assert.equal(metrics.body.rateLimit.burstMax, 0);
  assert.equal(metrics.body.rateLimit.burstWindowMs, 5000);
  assert.equal(typeof metrics.body.rateLimit.blockedByBurst, 'number');
  assert.equal(typeof metrics.body.rateLimit.burstTrackedKeys, 'number');
});

test('HTTP: 突发档(可疑请求模式)第 3 个请求 429 scope=burst code=429400, 窗口过后恢复', allow429(async () => {
  let fakeNow = 1700000000000;
  wordChainRateLimiter.reset();
  wordChainRateLimiter.configure({
    max: 1000000,
    windowMs: 60000,
    hourlyMax: 1000000,
    hourWindowMs: 3600000,
    burstMax: 2,
    burstWindowMs: 1000,
    clock: () => fakeNow
  });
  const corsHeaders = { Origin: 'https://client.example' };

  try {
    const first = await get('/api/v1/games?limit=1', corsHeaders);
    const second = await get('/api/v1/games?limit=1', corsHeaders);
    assert.equal(first.status, 200, first.raw);
    assert.equal(second.status, 200, second.raw);
    assert.equal(first.headers['x-ratelimit-burst-limit'], '2');
    assert.equal(first.headers['x-ratelimit-burst-remaining'], '1');
    assert.equal(second.headers['x-ratelimit-burst-remaining'], '0');

    // burst 三头由 cors.js 暴露, 否则浏览器读不到剩余量 (200 与 429 都要暴露)
    assert.match(first.headers['access-control-expose-headers'], /X-RateLimit-Burst-Limit/);
    assert.match(first.headers['access-control-expose-headers'], /X-RateLimit-Burst-Remaining/);
    assert.match(first.headers['access-control-expose-headers'], /X-RateLimit-Burst-Reset/);

    // 第 3 个请求: 窗口内已放行 2 个 -> 429
    const third = await get('/api/v1/games?limit=1', corsHeaders);
    assert.equal(third.status, 429, third.raw);
    assert.equal(third.body.error, 'RateLimitExceeded');
    assert.equal(third.body.scope, 'burst');
    assert.equal(third.headers['x-ratelimit-scope'], 'burst');
    assert.strictEqual(third.body.code, 429400, 'code 必须是 JSON 数字 429400');
    assert.equal(third.body.retryAfter, 1, 'Retry-After 按该突发窗口的剩余时间');
    assert.ok(Number(third.headers['retry-after']) >= 1);
    assert.match(third.headers['access-control-expose-headers'], /X-RateLimit-Burst-Remaining/);

    // 伪造 X-Forwarded-For 不能绕过突发档
    const spoofed = await get(
      '/api/v1/games?limit=1',
      { 'X-Forwarded-For': '203.0.113.9', Origin: 'https://client.example' }
    );
    assert.equal(spoofed.status, 429);

    const stats = wordChainRateLimiter.getStats();
    assert.equal(stats.blocked, 2, '只应有 2 次预期内的 429');
    assert.equal(stats.blockedByBurst, 2);
    assert.equal(stats.blockedByMinute, 0);
    assert.equal(stats.blockedByHour, 0);
    assert.equal(stats.burstTrackedKeys, 1);

    // 窗口推进后恢复
    fakeNow += 1001;
    const recovered = await get('/api/v1/games?limit=1', corsHeaders);
    assert.equal(recovered.status, 200, recovered.raw);
  } finally {
    wordChainRateLimiter.configure({
      max: 1000000,
      windowMs: 60000,
      hourlyMax: 1000000,
      hourWindowMs: 3600000,
      burstMax: 0,
      burstWindowMs: 5000,
      clock: () => Date.now()
    });
    wordChainRateLimiter.reset();
  }
}));

test('HTTP: 三档同时耗尽时 scope 与 Retry-After 取最晚解锁的档 (hour)', allow429(async () => {
  let fakeNow = 1700000000000;
  wordChainRateLimiter.reset();
  wordChainRateLimiter.configure({
    max: 1,
    windowMs: 60000,
    hourlyMax: 1,
    hourWindowMs: 3600000,
    burstMax: 1,
    burstWindowMs: 1000,
    clock: () => fakeNow
  });

  try {
    const allowed = await get('/api/v1/games?limit=1');
    assert.equal(allowed.status, 200, allowed.raw);
    assert.equal(allowed.headers['x-ratelimit-burst-limit'], '1');

    const blocked = await get('/api/v1/games?limit=1');
    assert.equal(blocked.status, 429, blocked.raw);
    assert.equal(blocked.body.scope, 'hour', '要按最晚解锁的档给 Retry-After, 否则客户端按时重试还会再撞 429');
    assert.equal(blocked.headers['x-ratelimit-scope'], 'hour');
    assert.equal(blocked.body.retryAfter, 3600);
    assert.strictEqual(blocked.body.code, 429400);

    const stats = wordChainRateLimiter.getStats();
    assert.equal(stats.blocked, 1);
    assert.equal(stats.blockedByHour, 1);
    assert.equal(stats.blockedByMinute, 0, '被计数的是实际治理的那一档');
    assert.equal(stats.blockedByBurst, 0);
  } finally {
    wordChainRateLimiter.configure({
      max: 1000000,
      windowMs: 60000,
      hourlyMax: 1000000,
      hourWindowMs: 3600000,
      burstMax: 0,
      burstWindowMs: 5000,
      clock: () => Date.now()
    });
    wordChainRateLimiter.reset();
  }
}));

test('HTTP: 限流 429 响应体带 JSON 数字 code 429400 (不是字符串)', allow429(async () => {
  let fakeNow = 1700000000000;
  wordChainRateLimiter.reset();
  wordChainRateLimiter.configure({
    max: 1,
    windowMs: 60000,
    hourlyMax: 1000000,
    hourWindowMs: 3600000,
    clock: () => fakeNow
  });

  try {
    const allowed = await get('/api/v1/games?limit=1');
    assert.equal(allowed.status, 200, allowed.raw);
    const blocked = await get('/api/v1/games?limit=1');
    assert.equal(blocked.status, 429, blocked.raw);
    assert.strictEqual(blocked.body.code, 429400, 'code 必须是 JSON 数字 429400');
    assert.equal(typeof blocked.body.code, 'number');
    assert.match(blocked.raw, /"code":429400/, '原始 JSON 里不能是字符串 "429400"');
    // 既有字段的语义一个不变, 只多了 code
    assert.equal(blocked.body.error, 'RateLimitExceeded');
    assert.equal(blocked.body.message, 'Too many requests. Please slow down.');
    assert.equal(blocked.body.scope, 'minute');
    assert.ok(Number(blocked.body.retryAfter) >= 1);
    const stats = wordChainRateLimiter.getStats();
    assert.equal(stats.blocked, 1);
    assert.equal(stats.blockedByMinute, 1);
    assert.equal(stats.blockedByBurst, 0);
  } finally {
    wordChainRateLimiter.configure({
      max: 1000000,
      windowMs: 60000,
      hourlyMax: 1000000,
      hourWindowMs: 3600000,
      burstMax: 0,
      burstWindowMs: 5000,
      clock: () => Date.now()
    });
    wordChainRateLimiter.reset();
  }
}));

// ---------------------------------------------------------------- 机器人玩家 isBot

test('机器人: isBot 只在布尔 true 时生效, 玩家对象在创建/查询/加入三处形状一致', async () => {
  const playerKeys = (player) => Object.keys(player).sort();

  const created = await post('/api/v1/games', {
    players: [
      { name: 'BoolTrue', isBot: true },
      { name: 'BoolString', isBot: 'true' },
      { name: 'BoolNumber', isBot: 1 }
    ]
  });
  assert.equal(created.status, 201, created.raw);
  assert.deepEqual(created.body.players.map((p) => p.isBot), [true, false, false]);
  for (const player of created.body.players) {
    assert.deepEqual(playerKeys(player), ['id', 'isBot', 'name', 'score']);
  }

  // getGame 的玩家对象与创建响应同形
  const fetched = await get(`/api/v1/games/${created.body.gameId}`);
  assert.equal(fetched.status, 200, fetched.raw);
  assert.deepEqual(fetched.body.players.map((p) => p.isBot), [true, false, false]);
  for (const player of fetched.body.players) {
    assert.deepEqual(playerKeys(player), ['id', 'isBot', 'name', 'score']);
  }

  // join 也可以补一个机器人进来
  const lobby = (await post('/api/v1/games', { playerName: 'JoinHost' })).body;
  const joined = await post(`/api/v1/games/${lobby.gameId}/join`, { playerName: 'LateBot', isBot: true });
  assert.equal(joined.status, 201, joined.raw);
  assert.equal(joined.body.player.isBot, true);
  assert.deepEqual(playerKeys(joined.body.player), ['id', 'isBot', 'name', 'score']);
  assert.equal(joined.body.players[1].isBot, true, 'join 响应里的 players 也带 isBot');
});

test('机器人: 座位 0 是机器人时开局请求内就自动出首词 (链长是 1, 不是 0)', async () => {
  const created = await post('/api/v1/games', {
    players: [{ name: 'SeatZeroBot', isBot: true }, { name: 'SeatOneHuman' }]
  });
  assert.equal(created.status, 201, created.raw);
  assert.equal(created.body.status, 'active');
  assert.equal(created.body.chainLength, 1, '机器人首位必须在创建请求内出首词, 否则没人能推进');
  assert.equal(created.body.wordChain.length, 1);
  assert.equal(created.body.currentTurnIndex, 1);
  assert.equal(created.body.currentPlayerId, created.body.players[1].id);
  assert.equal(created.body.requiredLetter, created.body.wordChain[0].slice(-1));

  const history = await get(`/api/v1/games/${created.body.gameId}/history`);
  const played = history.body.events.filter((event) => event.type === 'word_played');
  assert.equal(played.length, 1);
  assert.equal(played[0].bot, true, 'word_played 事件带 bot 标记');
  assert.equal(played[0].playerId, created.body.players[0].id);
  assert.equal(played[0].word, created.body.wordChain[0]);
});

test('机器人: 单人 lobby + isBot 在创建请求内打满链条并 completed', async () => {
  const created = await post('/api/v1/games', { playerName: 'SoloBot', isBot: true });
  assert.equal(created.status, 201, created.raw);
  // 桌上只有机器人: 回合永远回到它自己, 因此在创建请求内就把链打满
  assert.equal(created.body.status, 'completed');
  assert.equal(created.body.endedBy, 'chain_completed');
  assert.equal(created.body.chainLength, 10);
  assert.equal(created.body.chainLength, created.body.maxChainLength);
  assert.equal(created.body.players.length, 1);
  assert.equal(created.body.players[0].isBot, true);
  assert.equal(created.body.isDraw, false);
  assert.equal(created.body.winner.id, created.body.players[0].id);
  assert.equal(created.body.winner.isBot, true);

  const history = await get(`/api/v1/games/${created.body.gameId}/history`);
  const played = history.body.events.filter((event) => event.type === 'word_played');
  assert.equal(played.length, 10);
  for (const event of played) {
    assert.equal(event.bot, true);
  }
  const completed = history.body.events.find((event) => event.type === 'game_completed');
  assert.equal(completed.scores.length, 1);
  assert.equal(completed.scores[0].isBot, true, '结算 scores 也带 isBot');
});

test('机器人: 人机对局自动续打到终局, word_played 与结算 scores 都带 isBot', async () => {
  const dictionary = createDictionary();
  const created = await post('/api/v1/games', {
    players: [{ name: 'HumVsBotHuman' }, { name: 'HumVsBotBot', isBot: true }]
  });
  assert.equal(created.status, 201, created.raw);
  const human = created.body.players[0];
  assert.equal(created.body.chainLength, 0, '机器人在座位 1, 开局不该替人类先手');
  assert.equal(created.body.currentPlayerId, human.id);

  // 人类照提示出词; 机器人会在同一个请求里自动续打, 因此每轮链长 +2
  const script = [];
  let view = created.body;
  let guard = 0;
  while (view.status === 'active' && guard < 12) {
    guard += 1;
    const required = view.requiredLetter;
    const pick = dictionary.hint(required === null ? '' : required, { limit: 1, usedWords: view.wordChain });
    assert.ok(pick.hints.length > 0, '人类这边也必须有词可出');
    script.push(pick.hints[0]);
    const res = await play(view.gameId, human.id, pick.hints[0]);
    assert.equal(res.status, 200, res.raw);
    view = res.body;
    if (view.status === 'active') {
      assert.equal(view.chainLength, script.length * 2, '每轮人类出 1 词、机器人自动出 1 词');
      assert.equal(view.currentPlayerId, human.id, '机器人打完回合必须交回人类');
    }
  }

  assert.equal(view.status, 'completed');
  assert.equal(view.endedBy, 'chain_completed');
  assert.equal(view.chainLength, 10);
  // 本词库下人类 5 词 30 分、机器人 5 词 30 分(与既有 '完整一局' 用例一样依赖词库内容)
  assert.equal(view.isDraw, true, '平分时 winner 缺省');
  assert.equal(view.winner, undefined);

  const history = await get(`/api/v1/games/${view.gameId}/history`);
  const played = history.body.events.filter((event) => event.type === 'word_played');
  assert.equal(played.length, 10);
  played.forEach((event, index) => {
    assert.equal(event.bot, index % 2 === 1, `第 ${index + 1} 个词应当是${index % 2 === 1 ? '机器人' : '人类'}出的`);
  });
  const completed = history.body.events.find((event) => event.type === 'game_completed');
  assert.equal(completed.scores.length, 2);
  for (const entry of completed.scores) {
    assert.equal(typeof entry.isBot, 'boolean');
  }
});

test('服务层: 机器人卡死由服务端判负, 卡死方分数最高也不得夺冠', () => {
  // 注入小词表: 机器人先出一手 9 分长词, 等人类把链带到 'o' 时它无词可出,
  // 而此刻它的分数(9)高于两位人类 —— 旧实现会把这个卡死的机器人加冕, 本用例钉住修复后的语义。
  const service = createGameService({
    dictionary: createDictionary({ words: ['ant', 'tarantula', 'atom', 'mango'] }),
    maxChainLength: 10
  });
  const created = service.createGame({
    players: [{ name: 'StuckA' }, { name: 'StuckBot', isBot: true }, { name: 'StuckC' }]
  });
  const [humanA, bot, humanC] = created.players;

  const afterFirst = service.playWord(created.gameId, { playerId: humanA.id, word: 'ant' });
  assert.equal(afterFirst.chainLength, 2, '机器人自动接上 tarantula');
  assert.equal(afterFirst.wordChain[1], 'tarantula');
  service.playWord(created.gameId, { playerId: humanC.id, word: 'atom' });

  const final = service.playWord(created.gameId, { playerId: humanA.id, word: 'mango' });
  assert.ok(!final.players.some((p) => p.id === bot.id), '卡死方必须先被移出在座');
  assert.equal(final.status, 'forfeited');
  assert.equal(final.endedBy, 'bot_stuck');
  assert.equal(final.departures.length, 1);
  assert.deepEqual(final.departures[0], {
    playerId: bot.id,
    name: 'StuckBot',
    score: 9,
    isBot: true,
    reason: 'bot_stuck',
    at: final.departures[0].at
  });
  // 卡死瞬间 机器人 9 分 > 人类 A 8 分 > 人类 C 4 分
  assert.equal(final.winner.id, humanA.id);
  assert.notEqual(final.winner.id, bot.id, '卡死方不得夺冠');
  assert.equal(final.winner.isBot, false);
  assert.equal(final.isDraw, false);

  const history = service.getHistory(created.gameId, {});
  assert.ok(!history.events.some((event) => event.type === 'game_completed'), '收尾是 bot_stuck, 不是链满');
  const forfeited = history.events.find((event) => event.type === 'game_forfeited');
  assert.equal(forfeited.reason, 'bot_stuck');
  assert.equal(forfeited.departingPlayerId, bot.id);
  assert.equal(forfeited.departingPlayerIsBot, true);
  assert.equal(forfeited.winnerId, humanA.id);
  assert.equal(final.winner.id, forfeited.winnerId, '响应 winner 与事件 winnerId 必须一致');
  assert.equal(forfeited.scores.length, 2);
  for (const entry of forfeited.scores) {
    assert.equal(typeof entry.isBot, 'boolean');
  }
  const botWords = history.events.filter((event) => event.type === 'word_played' && event.bot === true);
  assert.equal(botWords.length, 1);
  assert.equal(botWords[0].playerId, bot.id);
  assert.equal(botWords[0].word, 'tarantula');
});

test('机器人 Defender: 质疑机器人由服务端代裁(不冻结对局), 真人无法借机器人通道代裁', async () => {
  const dictionary = createDictionary();
  const created = await post('/api/v1/games', {
    players: [{ name: 'ChalHuman' }, { name: 'ChalBot', isBot: true }]
  });
  assert.equal(created.status, 201, created.raw);
  const human = created.body.players[0];

  const opened = await play(created.body.gameId, human.id, 'apple');
  assert.equal(opened.status, 200, opened.raw);
  assert.equal(opened.body.chainLength, 2, '人类出词后机器人自动续打');
  const botWordIndex = 1;

  // 质疑机器人的词: 同一个请求内由服务端代裁, 不会留下 pending(旧的实现会永久冻结对局)
  const challenged = await post(`/api/v1/games/${created.body.gameId}/challenges`, {
    playerId: human.id,
    challengedWordIndex: botWordIndex
  });
  assert.equal(challenged.status, 201, challenged.raw);
  assert.equal(challenged.body.resolvedByBot, true);
  assert.equal(challenged.body.challengeResult, 'challenge_failed');
  assert.equal(challenged.body.serverValidated, true);
  assert.equal(challenged.body.mismatch, false, '代裁只看服务端复核, 不看客户端口径');
  assert.equal(challenged.body.challenge.challengeStatus, 'rejected');
  assert.equal(challenged.body.hasPendingChallenge, false);
  assert.equal(challenged.body.status, 'active');

  const state = await get(`/api/v1/games/${created.body.gameId}`);
  assert.equal(state.body.hasPendingChallenge, false, '代裁后不得留下 pending');
  assert.equal(state.body.currentPlayerId, human.id, '质疑失败后回合回到质疑者');
  // 对局没有被冻结: 质疑者立刻可以继续出词
  const pick = dictionary.hint(state.body.requiredLetter, { limit: 1, usedWords: state.body.wordChain });
  assert.ok(pick.hints.length > 0);
  const resumed = await play(created.body.gameId, human.id, pick.hints[0]);
  assert.equal(resumed.status, 200, resumed.raw);

  // 反例: 真人 Defender 伪造 body.isBot 不会走机器人通道(也不会因此被 403)
  const humans = (await post('/api/v1/games', {
    players: [{ name: 'JudgeAda' }, { name: 'JudgeBob' }]
  })).body;
  const played = await play(humans.gameId, humans.players[0].id, 'apple');
  assert.equal(played.status, 200, played.raw);
  const pending = await post(`/api/v1/games/${humans.gameId}/challenges`, {
    playerId: humans.players[1].id,
    challengedWordIndex: 0
  });
  assert.equal(pending.status, 201, pending.raw);
  assert.equal(pending.body.challengeStatus, 'pending');
  const resolved = await post(
    `/api/v1/games/${humans.gameId}/challenges/${pending.body.challengeId}/resolve`,
    { playerId: humans.players[0].id, isValid: true, isBot: true }
  );
  assert.equal(resolved.status, 200, resolved.raw);
  assert.equal(resolved.body.challengeResult, 'challenge_failed');
  assert.equal(resolved.body.resolvedByBot, undefined, '真人不能借机器人通道代裁');
});

// ---------------------------------------------------------------- 玩家名净化

test('玩家名净化: 剔除控制字符、折叠空白, 正常名原样放行', async () => {
  const created = await post('/api/v1/games', {
    players: [{ name: 'Al\u0000i\u001Fce\u007F' }, { name: '  A   B  ' }, { name: '词链玩家' }]
  });
  assert.equal(created.status, 201, created.raw);
  assert.deepEqual(created.body.players.map((player) => player.name), ['Alice', 'A B', '词链玩家']);
  assert.equal(created.body.players[0].isBot, false);

  const fetched = await get(`/api/v1/games/${created.body.gameId}`);
  assert.deepEqual(fetched.body.players.map((player) => player.name), ['Alice', 'A B', '词链玩家']);

  // 重名比较按净化后的名字: 'A B' 与 'A    B' 折叠后同名
  const duplicate = await post('/api/v1/games', {
    players: [{ name: 'A B' }, { name: 'A    B' }]
  });
  assert.equal(duplicate.status, 400);
  assert.match(duplicate.body.details[0].message, /Duplicate player name/);
});

test('玩家名净化: HTML/属性元字符与脚本关键字直接 400, 且不回显原始脏名', async () => {
  const cases = [
    ['<script>alert(1)</script>', /U\+003C/],
    ['A<script>B', /U\+003C/],
    ['Tom & Jerry', /U\+0026/],
    ['A"B', /U\+0022/],
    ["A'B", /U\+0027/],
    ['A`B', /U\+0060/],
    ['A=B', /U\+003D/],
    ['javascript:alert(1)', /offending sequence "javascript:"/i],
    ['vbscript:msgbox(1)', /offending sequence "vbscript:"/i],
    ['data:text/html,<b>', /offending sequence "data:"/i]
  ];

  for (const [dirty, pattern] of cases) {
    const res = await post('/api/v1/games', { players: [{ name: dirty }, { name: 'CleanPeer' }] });
    assert.equal(res.status, 400, `脏名应当被拒绝: ${JSON.stringify(dirty)} -> ${res.raw}`);
    assert.equal(res.body.error, 'ValidationError');
    assert.equal(res.body.details[0].field, 'players[0].name');
    // 顶层 message 是笼统的 'Invalid request parameters', 具体原因在 details[0].message
    assert.match(res.body.details[0].message, /HTML metacharacters or script content/);
    assert.match(res.body.details[0].message, pattern);
    assert.ok(!res.body.details[0].message.includes(dirty), '错误信息不回显原始脏名');
    assert.ok(!res.body.message.includes(dirty), '顶层 message 同样不回显脏名');
  }

  // 关键回归: 元字符是"整体拒绝", 不是静默剥离成 AscriptB 之后再 201
  const stripped = await post('/api/v1/games', { players: [{ name: 'A<script>B' }, { name: 'AscriptB' }] });
  assert.equal(stripped.status, 400, stripped.raw);
  assert.match(stripped.body.details[0].message, /U\+003C/);
});

test('玩家名净化: 空名/纯空白 400, 40 字符为上限', async () => {
  const blank = await post('/api/v1/games', { players: [{ name: '   ' }, { name: 'BlankPeer' }] });
  assert.equal(blank.status, 400, blank.raw);
  assert.equal(blank.body.details[0].field, 'players[0].name');
  assert.match(blank.body.details[0].message, /Player name cannot be empty/);

  const nonString = await post('/api/v1/games', { players: [{ name: 42 }, { name: 'NonStringPeer' }] });
  assert.equal(nonString.status, 400, nonString.raw);
  assert.match(nonString.body.details[0].message, /Player name cannot be empty/);

  const boundary = await post('/api/v1/games', { players: [{ name: 'a'.repeat(40) }, { name: 'BoundaryPeer' }] });
  assert.equal(boundary.status, 201, boundary.raw);
  assert.equal(boundary.body.players[0].name, 'a'.repeat(40));

  const tooLong = await post('/api/v1/games', { players: [{ name: 'a'.repeat(41) }, { name: 'BoundaryPeer' }] });
  assert.equal(tooLong.status, 400, tooLong.raw);
  assert.equal(tooLong.body.details[0].field, 'players[0].name');
  assert.match(tooLong.body.details[0].message, /cannot exceed 40 characters/);
});

test('POST /join: 玩家名同样走净化(元字符 400 / 控制字符剔除)', async () => {
  const lobby = (await post('/api/v1/games', { playerName: 'JoinSanitizeHost' })).body;

  const metachar = await post(`/api/v1/games/${lobby.gameId}/join`, { playerName: 'Bad<Name>' });
  assert.equal(metachar.status, 400, metachar.raw);
  assert.equal(metachar.body.details[0].field, 'playerName');
  assert.match(metachar.body.details[0].message, /U\+003C/);
  assert.ok(!metachar.body.details[0].message.includes('Bad<Name>'));

  const blank = await post(`/api/v1/games/${lobby.gameId}/join`, { playerName: ' \t\n ' });
  assert.equal(blank.status, 400, blank.raw);
  assert.match(blank.body.details[0].message, /Player name cannot be empty/);

  const joined = await post(`/api/v1/games/${lobby.gameId}/join`, { playerName: '  Late\u0007Bot  ' });
  assert.equal(joined.status, 201, joined.raw);
  assert.equal(joined.body.player.name, 'LateBot');
  assert.equal(joined.body.player.isBot, false, 'isBot 只在布尔 true 时生效');
  assert.equal(joined.body.players.length, 2);
});

// ================================================================== 404

test('/api/v1 下的未注册路径返回 404 JSON', async () => {
  const res = await get('/api/v1/unknown-endpoint');
  assert.equal(res.status, 404);
  assert.equal(res.body.error, 'NotFound');
});

test('全局未注册路径返回 404 JSON', async () => {
  const res = await get('/definitely-not-here');
  assert.equal(res.status, 404);
  assert.match(res.body.message, /Cannot GET/);
});

test('baseUrl 变量可用于自检端口解析', () => {
  assert.match(baseUrl, /^http:\/\/127\.0\.0\.1:\d+$/);
});

// ================================================================== 启动回归
// 背景: 修复前 server.js 写成 winston.format.printf(consoleFormat) —— 把已经构造好的
// printf 格式又喂给 printf(), logform 于是把对象当模板函数, 第一次往控制台写日志就抛
// "TypeError: this.template is not a function"。本测试套件默认移除了控制台 transport,
// 所以单靠请求级用例是发现不了这个崩溃的; 下面两条专门守住这一点。

test('日志: 控制台 printf 格式可正常渲染 (回归: printf(printf) 抛 this.template)', async () => {
  const winston = require('winston');
  const { PassThrough } = require('node:stream');
  const stream = new PassThrough();
  let captured = '';
  stream.on('data', (chunk) => { captured += chunk.toString(); });

  const probe = winston.createLogger({
    level: 'info',
    format: winston.format.combine(
      winston.format.timestamp(),
      winston.format.errors({ stack: true }),
      winston.format.json()
    ),
    transports: [
      new winston.transports.Stream({
        stream,
        format: winston.format.combine(winston.format.colorize(), consoleFormat)
      })
    ]
  });

  probe.info('format-probe', { requestId: 'fmt-1' });
  await new Promise((resolve) => setImmediate(resolve));
  probe.close();

  assert.match(captured, /format-probe/, '控制台格式应当输出消息内容');
  assert.match(captured, /fmt-1/, '控制台格式应当带上 requestId');
});

test('启动回归: node server.js 能真正监听并服务 (不再启动即崩)', async () => {
  const { spawn } = require('node:child_process');
  const bootPort = 34567;

  const child = spawn(process.execPath, ['server.js'], {
    cwd: path.join(__dirname, '..'),
    env: Object.assign({}, process.env, { PORT: String(bootPort) }),
    stdio: ['ignore', 'pipe', 'pipe']
  });

  let output = '';
  child.stdout.on('data', (chunk) => { output += chunk.toString(); });
  child.stderr.on('data', (chunk) => { output += chunk.toString(); });

  function probeHealth() {
    return new Promise((resolve, reject) => {
      const req = http.get({ host: '127.0.0.1', port: bootPort, path: '/health' }, (res) => {
        let raw = '';
        res.setEncoding('utf8');
        res.on('data', (chunk) => { raw += chunk; });
        res.on('end', () => resolve({ status: res.statusCode, body: raw }));
      });
      req.on('error', reject);
      req.setTimeout(2000, () => req.destroy(new Error('probe timeout')));
    });
  }

  try {
    const deadline = Date.now() + 15000;
    let health = null;
    let exited = false;

    while (Date.now() < deadline) {
      if (child.exitCode !== null) {
        exited = true;
        break;
      }
      try {
        health = await probeHealth();
        if (health.status === 200) break;
      } catch (error) {
        // 还没起来, 继续等
      }
      await new Promise((resolve) => setTimeout(resolve, 200));
    }

    assert.equal(exited, false, `子进程提前退出(启动即崩):\n${output}`);
    assert.ok(health && health.status === 200, `15s 内没有拿到 /health 200:\n${output}`);
    assert.match(health.body, /"status":"healthy"/);
    assert.match(health.body, /"dictionarySize":\d+/);
  } finally {
    child.kill();
    await new Promise((resolve) => {
      if (child.exitCode !== null) {
        return resolve();
      }
      const timer = setTimeout(() => {
        child.kill('SIGKILL');
        resolve();
      }, 3000);
      child.once('exit', () => {
        clearTimeout(timer);
        resolve();
      });
    });
  }
});