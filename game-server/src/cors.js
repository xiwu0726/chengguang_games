'use strict';

/**
 * CORS 中间件 —— 设计文档 Security Considerations #4
 * "CORS configuration for frontend integration"。
 *
 * 默认**关闭**: 不发任何 CORS 头, 与加这个模块之前的行为完全一致(同源部署本来就不需要)。
 * 用 WORD_CHAIN_CORS_ORIGIN 打开: 逗号分隔的白名单, 或 `*` 允许任意来源。
 *
 * 一个与 429 直接相关的设计点: 预检请求(OPTIONS + Access-Control-Request-Method)
 * 在这里就返回 204, **不会往下走进限流器**。浏览器每次跨域写操作前都要先发一个 preflight,
 * 如果把它计入配额, 10 次/分钟很快被 preflight 吃光, 客户端就会看到"我没发几个请求却 429 了"。
 */

const DEFAULT_ALLOWED_METHODS = 'GET, POST, DELETE, OPTIONS';
const DEFAULT_ALLOWED_HEADERS = 'Content-Type, X-Request-Id';
const DEFAULT_MAX_AGE_SECONDS = 600;

/**
 * 需要暴露给浏览器脚本的响应头。
 * 默认浏览器只让 JS 读到少数"安全列表"头; Retry-After / 剩余额度读不到的话,
 * 客户端就无法自己退避, 只能撞第二次 429。
 */
const EXPOSED_HEADERS = [
  'Location',
  'Retry-After',
  'X-Request-Id',
  'X-RateLimit-Limit',
  'X-RateLimit-Remaining',
  'X-RateLimit-Reset',
  'X-RateLimit-Scope',
  'X-RateLimit-Hour-Limit',
  'X-RateLimit-Hour-Remaining',
  'X-RateLimit-Hour-Reset',
  'X-RateLimit-Burst-Limit',
  'X-RateLimit-Burst-Remaining',
  'X-RateLimit-Burst-Reset'
].join(', ');

function toHeaderList(value, fallback) {
  if (Array.isArray(value)) {
    const joined = value.map((item) => String(item).trim()).filter(Boolean).join(', ');
    return joined.length > 0 ? joined : fallback;
  }
  if (typeof value === 'string' && value.trim().length > 0) {
    return value.trim();
  }
  return fallback;
}

function toPositiveInt(value, fallback) {
  const parsed = Number(value);
  if (!Number.isFinite(parsed) || parsed <= 0) {
    return fallback;
  }
  return Math.floor(parsed);
}

/**
 * 解析来源白名单。
 * @returns {{wildcard: boolean, origins: Set<string>}|null} null 表示未启用
 */
function normalizeOrigins(raw) {
  if (raw === undefined || raw === null || raw === false) {
    return null;
  }
  const items = (Array.isArray(raw) ? raw : String(raw))
    .toString()
    .split(',')
    .map((item) => item.trim().toLowerCase())
    .filter((item) => item.length > 0 && item !== 'null' && item !== 'undefined');

  if (items.length === 0 || items.every((item) => item === 'none')) {
    return null;
  }
  if (items.includes('*')) {
    return { wildcard: true, origins: new Set() };
  }
  return { wildcard: false, origins: new Set(items) };
}

/**
 * @param {object} [options]
 * @param {string|string[]|false} [options.origin]        白名单; '*' 或空/未配置 = 关闭
 * @param {string|string[]} [options.methods]             预检允许的方法
 * @param {string|string[]} [options.headers]             预检允许的请求头(默认回显客户端请求的头)
 * @param {number} [options.maxAgeSeconds]                预检缓存时长, 默认 600
 * @param {boolean} [options.credentials]                 是否允许携带 Cookie(与 '*' 互斥)
 */
function createCorsMiddleware(options) {
  const opts = options || {};
  const parsed = normalizeOrigins(opts.origin);
  const methods = toHeaderList(opts.methods, DEFAULT_ALLOWED_METHODS);
  const headers = toHeaderList(opts.headers, DEFAULT_ALLOWED_HEADERS);
  const maxAgeSeconds = toPositiveInt(opts.maxAgeSeconds, DEFAULT_MAX_AGE_SECONDS);
  // '*' 与凭据不能共存(浏览器会直接拒绝这种组合), 所以白名单模式下才允许 credentials
  const credentials = opts.credentials === true && parsed !== null && !parsed.wildcard;

  const describe = () => ({
    enabled: parsed !== null,
    origins: parsed === null ? [] : (parsed.wildcard ? ['*'] : Array.from(parsed.origins)),
    allowCredentials: credentials,
    maxAgeSeconds
  });

  if (parsed === null) {
    const disabled = function corsDisabled(req, res, next) {
      return next();
    };
    disabled.cors = describe();
    return disabled;
  }

  const middleware = function cors(req, res, next) {
    const origin = req.headers ? req.headers.origin : undefined;
    // 没有 Origin 就不是跨域请求(同源页面、curl、服务端调用), 一律不加头
    if (typeof origin !== 'string' || origin.length === 0) {
      return next();
    }

    const key = origin.toLowerCase();
    const allowed = parsed.wildcard ? '*' : (parsed.origins.has(key) ? origin : null);
    if (allowed === null) {
      // 白名单外的来源: 不发 CORS 头, 浏览器会自行拦下响应; 服务端不必额外报错
      return next();
    }

    res.setHeader('Access-Control-Allow-Origin', allowed);
    if (allowed !== '*') {
      // 按 Origin 变化的响应头必须声明 Vary, 否则共享缓存会把 A 站的头发给 B 站
      res.setHeader('Vary', 'Origin');
      if (credentials) {
        res.setHeader('Access-Control-Allow-Credentials', 'true');
      }
    }
    res.setHeader('Access-Control-Expose-Headers', EXPOSED_HEADERS);

    const requestedMethod = req.headers ? req.headers['access-control-request-method'] : undefined;
    if (req.method === 'OPTIONS' && typeof requestedMethod === 'string' && requestedMethod.length > 0) {
      res.setHeader('Access-Control-Allow-Methods', methods);
      const requestedHeaders = req.headers['access-control-request-headers'];
      res.setHeader(
        'Access-Control-Allow-Headers',
        typeof requestedHeaders === 'string' && requestedHeaders.length > 0 ? requestedHeaders : headers
      );
      res.setHeader('Access-Control-Max-Age', String(maxAgeSeconds));
      res.statusCode = 204;
      return res.end();
    }

    return next();
  };

  middleware.cors = describe();
  return middleware;
}

module.exports = {
  createCorsMiddleware,
  normalizeOrigins,
  EXPOSED_HEADERS,
  DEFAULT_ALLOWED_METHODS,
  DEFAULT_ALLOWED_HEADERS,
  DEFAULT_MAX_AGE_SECONDS
};
