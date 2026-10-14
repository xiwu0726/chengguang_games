const express = require('express');
const winston = require('winston');
const axios = require('axios');

const {
  createRateLimiter,
  DEFAULT_WINDOW_MS,
  DEFAULT_MAX,
  DEFAULT_HOUR_WINDOW_MS,
  DEFAULT_HOURLY_MAX,
  DEFAULT_BURST_WINDOW_MS,
  DEFAULT_BURST_MAX
} = require('./src/rate-limit');
const { createGameService } = require('./src/word-chain/game-service');
const { createWordChainRouter } = require('./src/word-chain/routes');
const { createCorsMiddleware } = require('./src/cors');

/** 环境变量解析: 未设置或非法时回退默认; 允许显式 0 (表示关闭该档) */
function parseQuotaEnv(raw, fallback) {
  if (raw === undefined || raw === null || raw === '') {
    return fallback;
  }
  const parsed = Number(raw);
  if (!Number.isFinite(parsed) || parsed < 0) {
    return fallback;
  }
  return Math.floor(parsed);
}

// Configuration
const config = {
  port: process.env.PORT || 3000,
  maxRetries: 5,
  initialDelayMs: 1000,
  maxDelayMs: 16000,
  logLevel: process.env.LOG_LEVEL || 'info',
  environment: process.env.NODE_ENV || 'development',
  requestIdHeader: 'X-Request-ID',
  enableAuditLog: process.env.ENABLE_AUDIT_LOG === 'true',
  enablePerformanceLog: process.env.ENABLE_PERFORMANCE_LOG === 'true',
  logFormat: process.env.LOG_FORMAT || 'json', // json or pretty
  // 词链接龙 (game-design/word-chain-game-design.md)
  wordChain: {
    // 429400: 同一 IP 每分钟 10 次 (WORD_CHAIN_RATE_LIMIT_MAX=0 可关闭该档)
    rateLimitWindowMs: parseQuotaEnv(process.env.WORD_CHAIN_RATE_LIMIT_WINDOW_MS, DEFAULT_WINDOW_MS),
    rateLimitMax: parseQuotaEnv(process.env.WORD_CHAIN_RATE_LIMIT_MAX, DEFAULT_MAX),
    // 429400 第二档: 同一 IP 每小时 100 次 (WORD_CHAIN_RATE_LIMIT_HOURLY_MAX=0 可关闭)
    rateLimitHourWindowMs: parseQuotaEnv(
      process.env.WORD_CHAIN_RATE_LIMIT_HOUR_WINDOW_MS,
      DEFAULT_HOUR_WINDOW_MS
    ),
    rateLimitHourlyMax: parseQuotaEnv(
      process.env.WORD_CHAIN_RATE_LIMIT_HOURLY_MAX,
      DEFAULT_HOURLY_MAX
    ),
    // 429400 第三档: 可疑请求模式(Bot detection)的突发观测窗
    // 默认关闭(WORD_CHAIN_RATE_LIMIT_BURST_MAX=0): 正常功能流量也是"毫秒级连打",
    // 默认开启会把功能用例打成非预期 429。只在确认要拦 bot 时显式开正数。
    rateLimitBurstWindowMs: parseQuotaEnv(
      process.env.WORD_CHAIN_RATE_LIMIT_BURST_WINDOW_MS,
      DEFAULT_BURST_WINDOW_MS
    ),
    rateLimitBurstMax: parseQuotaEnv(
      process.env.WORD_CHAIN_RATE_LIMIT_BURST_MAX,
      DEFAULT_BURST_MAX
    ),
    maxChainLength: Number(process.env.WORD_CHAIN_MAX_CHAIN_LENGTH) || 10,
    // CORS (设计文档 Security Considerations #4): 默认关闭, 不发任何跨域头。
    // WORD_CHAIN_CORS_ORIGIN 填逗号分隔白名单 (https://a.example,https://b.example) 或 * 打开。
    corsOrigin: process.env.WORD_CHAIN_CORS_ORIGIN || '',
    corsCredentials: process.env.WORD_CHAIN_CORS_CREDENTIALS === 'true',
    corsMaxAgeSeconds: parseQuotaEnv(process.env.WORD_CHAIN_CORS_MAX_AGE_SECONDS, 600),
    // 只有部署在可信网关之后才应开启(否则客户端可伪造 X-Forwarded-For 绕过限流)
    trustProxyHeader: process.env.TRUST_PROXY_HEADER === 'true'
  }
};

// 监听中的 http server (由 startServer 赋值), 供 internalBaseUrl() 解析真实端口
let httpServer = null;

/** 本服务自身的基础 URL —— 用真实绑定端口, 避免硬编码 3000 */
function internalBaseUrl() {
  const address = httpServer ? httpServer.address() : null;
  const port = address && typeof address === 'object' ? address.port : config.port;
  return `http://localhost:${port}`;
}

// Generate unique request ID
function generateRequestId() {
  return `req-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
}

// Request ID middleware
const requestIdMiddleware = (req, res, next) => {
  // 注意: Node 会把收到的请求头统一小写, 因此必须用 req.get() 做大小写无关读取,
  // 直接索引 req.headers['X-Request-ID'] 永远是 undefined, 客户端传入的 requestId 会被丢弃。
  req.requestId = req.get(config.requestIdHeader) || generateRequestId();
  res.setHeader(config.requestIdHeader, req.requestId);
  next();
};

// Custom log format for console
const consoleFormat = winston.format.printf(({ timestamp, level, message, requestId, ...metadata }) => {
  let msg = `${timestamp} [${level.toUpperCase()}] [${requestId}] ${message}`;
  if (Object.keys(metadata).length > 0) {
    msg += ` ${JSON.stringify(metadata)}`;
  }
  return msg;
});

// Request logger middleware
const requestLoggerMiddleware = (req, res, next) => {
  const startTime = Date.now();

  // Log request
  logger.info('Request received', {
    requestId: req.requestId,
    method: req.method,
    path: req.path,
    query: req.query,
    ip: req.ip,
    userAgent: req.get('user-agent')
  });

  // Log response
  res.on('finish', () => {
    const duration = Date.now() - startTime;

    // Performance logging
    if (config.enablePerformanceLog) {
      logger.info('Request completed', {
        requestId: req.requestId,
        method: req.method,
        path: req.path,
        statusCode: res.statusCode,
        duration: `${duration}ms`,
        userAgent: req.get('user-agent')
      });
    }

    // Audit logging
    if (config.enableAuditLog && (res.statusCode >= 400)) {
      logger.warn('Request failed', {
        requestId: req.requestId,
        method: req.method,
        path: req.path,
        statusCode: res.statusCode,
        duration: `${duration}ms`
      });
    }
  });

  next();
};

// Ensure logs directory exists (必须在 createLogger 之前, 否则首次写入会 ENOENT)
const fs = require('fs');
if (!fs.existsSync('logs')) {
  fs.mkdirSync('logs', { recursive: true });
}

// Configure Winston logger with advanced formatting
const logger = winston.createLogger({
  level: config.logLevel,
  format: winston.format.combine(
    winston.format.timestamp({ format: 'YYYY-MM-DD HH:mm:ss' }),
    winston.format.errors({ stack: true }),
    winston.format.json()
  ),
  transports: [
    // Error log with rotation
    new winston.transports.File({
      filename: 'logs/error.log',
      level: 'error',
      maxsize: 5242880, // 5MB
      maxFiles: 5,
      tailable: true
    }),
    // Combined log with rotation and performance logging
    new winston.transports.File({
      filename: 'logs/combined.log',
      maxsize: 5242880,
      maxFiles: 5,
      tailable: true
    }),
    // Performance log (separate file)
    new winston.transports.File({
      filename: 'logs/performance.log',
      level: 'info',
      maxsize: 5242880,
      maxFiles: 5,
      tailable: true
    }),
    // Audit log (separate file)
    new winston.transports.File({
      filename: 'logs/audit.log',
      level: 'warn',
      maxsize: 5242880,
      maxFiles: 5,
      tailable: true
    }),
    new winston.transports.Console({
      format: winston.format.combine(
        winston.format.colorize(),
        // 注意: consoleFormat 已经是 printf 格式, 这里不能再套一层 printf(...),
        // 否则 logform 会把 format 对象当成模板函数, 第一次写控制台就抛
        // "TypeError: this.template is not a function" 并让进程启动即退出。
        consoleFormat
      )
    })
  ]
});

const app = express();
app.use(express.json({ limit: '10mb' }));
app.use(requestIdMiddleware);
app.use(requestLoggerMiddleware);

// Circuit Breaker Implementation
class CircuitBreaker {
  constructor(name, failureThreshold, timeout) {
    this.name = name;
    this.failureThreshold = failureThreshold;
    this.timeout = timeout;
    this.failureCount = 0;
    this.lastFailureTime = null;
    this.state = 'CLOSED'; // CLOSED, OPEN, HALF_OPEN
  }

  recordFailure() {
    this.failureCount++;
    this.lastFailureTime = Date.now();
    logger.warn(`Circuit breaker [${this.name}] failure count: ${this.failureCount}/${this.failureThreshold}`, {
      requestId: generateRequestId(),
      context: 'circuit-breaker'
    });

    if (this.failureCount >= this.failureThreshold) {
      this.state = 'OPEN';
      logger.error(`Circuit breaker [${this.name}] OPENED - too many failures`, {
        requestId: generateRequestId(),
        context: 'circuit-breaker'
      });
    }
  }

  recordSuccess() {
    this.failureCount = 0;
    this.state = 'HALF_OPEN';
    logger.info(`Circuit breaker [${this.name}] success recorded, transitioning to HALF_OPEN`, {
      requestId: generateRequestId(),
      context: 'circuit-breaker'
    });
  }

  canAttempt() {
    const now = Date.now();
    switch (this.state) {
      case 'CLOSED':
        return true;
      case 'OPEN':
        if (now - this.lastFailureTime > this.timeout) {
          this.state = 'HALF_OPEN';
          logger.info(`Circuit breaker [${this.name}] attempting reset (HALF_OPEN)`, {
            requestId: generateRequestId(),
            context: 'circuit-breaker'
          });
          return true;
        }
        return false;
      case 'HALF_OPEN':
        return true;
      default:
        return false;
    }
  }

  getState() {
    return this.state;
  }
}

// Initialize circuit breakers for different services
const circuitBreakers = {
  gameService: new CircuitBreaker('gameService', 5, 30000),
  externalAPI: new CircuitBreaker('externalAPI', 5, 30000)
};

// Error classification
class ErrorClassifier {
  static classify(error) {
    if (error.response) {
      const status = error.response.status;
      if (status === 429) {
        return { type: 'RATE_LIMIT', code: '429', retryable: true, message: 'Too many requests' };
      } else if (status >= 500) {
        return { type: 'SERVER_ERROR', code: status.toString(), retryable: true, message: `Server error: ${status}` };
      } else if (status >= 400) {
        return { type: 'CLIENT_ERROR', code: status.toString(), retryable: false, message: `Client error: ${status}` };
      }
    } else if (error.code) {
      const code = error.code;
      if (code === 'ETIMEDOUT' || code === 'ECONNREFUSED' || code === 'ECONNRESET') {
        return { type: 'NETWORK_ERROR', code, retryable: true, message: `Network error: ${code}` };
      }
    }
    return { type: 'UNKNOWN_ERROR', code: 'UNKNOWN', retryable: false, message: error.message };
  }
}

// Retry logic with exponential backoff (1s, 2s, 4s, 8s, 16s)
async function fetchWithRetry(url, options = {}, maxRetries = null, delayMs = null) {
  const retries = maxRetries || config.maxRetries;
  // 注意: 必须是 let —— 退避延迟会在重试时被重新赋值(原实现用 const, 第二次尝试会抛
  // "Assignment to constant variable", 重试逻辑实际不可用)。
  let delay = delayMs || config.initialDelayMs;

  for (let attempt = 1; attempt <= retries; attempt++) {
    try {
      const response = await axios.get(url, {
        timeout: 5000,
        ...options
      });
      logger.info(`[Retry ${attempt}/${retries}] Successfully fetched ${url}`, {
        requestId: generateRequestId(),
        context: 'retry-logic'
      });
      return response;
    } catch (error) {
      const classified = ErrorClassifier.classify(error);
      logger.warn(`[Retry ${attempt}/${retries}] ${classified.type} (${classified.code}) for ${url}: ${classified.message}`, {
        requestId: generateRequestId(),
        context: 'retry-logic',
        attempt: attempt,
        maxRetries: retries
      });

      // Only retry on retryable errors
      if (!classified.retryable || attempt >= retries) {
        logger.error(`Max retries exceeded for ${url}`, {
          requestId: generateRequestId(),
          context: 'retry-logic',
          errorType: classified.type
        });
        throw error;
      }

      // Exponential backoff: 1s, 2s, 4s, 8s, 16s
      if (attempt < retries) {
        logger.info(`Retrying in ${delay}ms...`, {
          requestId: generateRequestId(),
          context: 'retry-logic',
          delay: `${delay}ms`
        });
        await new Promise(resolve => setTimeout(resolve, delay));
        delay = Math.min(delay * 2, config.maxDelayMs);
      }
    }
  }
}

// Degraded mode tracker
class DegradedMode {
  constructor(threshold) {
    this.threshold = threshold;
    this.failedRequests = 0;
    this.totalRequests = 0;
    this.enabled = false;
  }

  recordRequest(success) {
    this.totalRequests++;
    if (!success) {
      this.failedRequests++;
    }

    const failureRate = this.failedRequests / this.totalRequests;

    if (!this.enabled && failureRate >= this.threshold) {
      this.enabled = true;
      logger.warn(`Degraded mode ENABLED - failure rate: ${(failureRate * 100).toFixed(1)}%`, {
        requestId: generateRequestId(),
        context: 'degraded-mode',
        threshold: `${(this.threshold * 100).toFixed(0)}%`
      });
    } else if (this.enabled && failureRate < this.threshold / 2) {
      this.enabled = false;
      logger.info(`Degraded mode DISABLED - failure rate: ${(failureRate * 100).toFixed(1)}%`, {
        requestId: generateRequestId(),
        context: 'degraded-mode'
      });
    }
  }

  isEnabled() {
    return this.enabled;
  }

  getStats() {
    return {
      enabled: this.enabled,
      failureRate: this.failedRequests / this.totalRequests,
      failedRequests: this.failedRequests,
      totalRequests: this.totalRequests
    };
  }
}

const degradedMode = new DegradedMode(0.7);

// Simulated error simulation endpoint
let errorCount = 0;
app.get('/simulate-error', (req, res) => {
  errorCount++;
  const is429 = errorCount % 4 === 0;
  const is500 = errorCount % 3 === 0 && !is429;
  const errorType = is429 ? '429' : (is500 ? '500' : 'network');

  if (errorType === '429') {
    logger.warn(`Simulating 429 error (attempt ${errorCount})`, {
      requestId: req.requestId,
      context: 'simulation',
      errorType: 'rate-limit'
    });
    return res.status(429).json({
      error: 'Too Many Requests',
      message: 'Rate limit exceeded',
      attempt: errorCount,
      retryAfter: 60
    });
  } else if (errorType === '500') {
    logger.error(`Simulating 500 error (attempt ${errorCount})`, {
      requestId: req.requestId,
      context: 'simulation',
      errorType: 'server-error'
    });
    return res.status(500).json({
      error: 'Internal Server Error',
      message: 'Simulated 500 error',
      attempt: errorCount
    });
  } else {
    logger.error(`Simulating network error (attempt ${errorCount})`, {
      requestId: req.requestId,
      context: 'simulation',
      errorType: 'network-error'
    });
    return res.status(503).json({
      error: 'Service Unavailable',
      message: 'Simulated network error',
      attempt: errorCount
    });
  }
});

// API endpoint with circuit breaker and degraded mode
app.get('/api/game-status', async (req, res) => {
  degradedMode.recordRequest(false);
  const startTime = Date.now();

  try {
    logger.info('Game status request received', {
      requestId: req.requestId,
      context: 'api-endpoint'
    });

    // Check circuit breaker state
    if (!circuitBreakers.gameService.canAttempt()) {
      logger.error('Circuit breaker is OPEN - game service unavailable', {
        requestId: req.requestId,
        context: 'api-endpoint',
        circuitBreakerState: circuitBreakers.gameService.getState()
      });
      degradedMode.recordRequest(false);
      return res.status(503).json({
        status: 'degraded',
        message: 'Game service temporarily unavailable due to high error rate',
        state: circuitBreakers.gameService.getState(),
        degradedMode: degradedMode.getStats()
      });
    }

    // Try to fetch game status with retry logic
    // 不能硬编码 3000: 进程可能监听在其它端口(PORT 覆盖 / 测试用 0 号端口自动分配)。
    const response = await fetchWithRetry(`${internalBaseUrl()}/simulate-error`, {}, 3, 1000);

    degradedMode.recordRequest(true);
    const duration = Date.now() - startTime;
    logger.info('Game status retrieved successfully', {
      requestId: req.requestId,
      context: 'api-endpoint',
      duration: `${duration}ms`
    });

    res.json({
      status: 'online',
      message: 'Server is running',
      errorType: null,
      latency: duration,
      degradedMode: degradedMode.getStats()
    });
  } catch (error) {
    const classified = ErrorClassifier.classify(error);
    const duration = Date.now() - startTime;

    logger.error('Game status request failed', {
      requestId: req.requestId,
      context: 'api-endpoint',
      errorType: classified.type,
      errorCode: classified.code,
      message: classified.message,
      duration: `${duration}ms`
    });

    // Update circuit breaker and degraded mode
    if (classified.retryable) {
      circuitBreakers.gameService.recordFailure();
    } else {
      circuitBreakers.gameService.recordSuccess();
    }
    degradedMode.recordRequest(false);

    // Graceful degradation response
    const isDegraded = degradedMode.isEnabled();
    const statusCode = isDegraded ? 503 : classified.code;

    res.status(statusCode).json({
      status: isDegraded ? 'degraded' : 'error',
      message: classified.message,
      errorType: classified.type,
      errorCode: classified.code,
      continueExecution: true,
      degradedMode: degradedMode.getStats(),
      circuitBreaker: {
        name: 'gameService',
        state: circuitBreakers.gameService.getState()
      },
      latency: duration
    });
  }
});

// Health check endpoint with detailed status
app.get('/health', (req, res) => {
  const health = {
    status: degradedMode.isEnabled() ? 'degraded' : 'healthy',
    timestamp: new Date().toISOString(),
    uptime: process.uptime(),
    version: '1.0.0',
    requestId: req.requestId,
    circuitBreakers: {
      gameService: {
        state: circuitBreakers.gameService.getState(),
        failureCount: circuitBreakers.gameService.failureCount
      },
      externalAPI: {
        state: circuitBreakers.externalAPI.getState(),
        failureCount: circuitBreakers.externalAPI.failureCount
      }
    },
    degradedMode: degradedMode.getStats(),
    wordChain: wordChainService.stats(),
    configuration: {
      maxRetries: config.maxRetries,
      initialDelayMs: config.initialDelayMs,
      circuitBreakerThreshold: circuitBreakers.gameService.failureThreshold,
      requestIdHeader: config.requestIdHeader,
      enableAuditLog: config.enableAuditLog,
      enablePerformanceLog: config.enablePerformanceLog,
      wordChainRateLimit: {
        windowMs: config.wordChain.rateLimitWindowMs,
        max: config.wordChain.rateLimitMax,
        hourWindowMs: config.wordChain.rateLimitHourWindowMs,
        hourlyMax: config.wordChain.rateLimitHourlyMax,
        burstWindowMs: config.wordChain.rateLimitBurstWindowMs,
        burstMax: config.wordChain.rateLimitBurstMax,
        trustProxyHeader: config.wordChain.trustProxyHeader
      },
      wordChainCors: wordChainCors.cors
    }
  };

  res.status(health.status === 'healthy' ? 200 : 503).json(health);
});

// Metrics endpoint
app.get('/metrics', (req, res) => {
  res.json({
    errors: {
      total: errorCount,
      byType: {
        429: Math.floor(errorCount / 4),
        500: Math.floor(errorCount / 3),
        network: errorCount - Math.floor(errorCount / 4) - Math.floor(errorCount / 3)
      }
    },
    circuitBreakers: {
      gameService: circuitBreakers.gameService.getState(),
      externalAPI: circuitBreakers.externalAPI.getState()
    },
    degradedMode: degradedMode.getStats(),
    wordChain: wordChainService.stats(),
    rateLimit: wordChainRateLimiter.getStats()
  });
});

// ============================================================
// 词链接龙 API (game-design/word-chain-game-design.md)
// ============================================================
const wordChainRateLimiter = createRateLimiter({
  windowMs: config.wordChain.rateLimitWindowMs,
  max: config.wordChain.rateLimitMax,
  hourWindowMs: config.wordChain.rateLimitHourWindowMs,
  hourlyMax: config.wordChain.rateLimitHourlyMax,
  burstWindowMs: config.wordChain.rateLimitBurstWindowMs,
  burstMax: config.wordChain.rateLimitBurstMax,
  trustProxyHeader: config.wordChain.trustProxyHeader
});

const wordChainService = createGameService({
  maxChainLength: config.wordChain.maxChainLength
});

// CORS 必须在限流之前: 预检(OPTIONS)在这里就返回 204, 不占用每分钟/每小时配额
const wordChainCors = createCorsMiddleware({
  origin: config.wordChain.corsOrigin,
  credentials: config.wordChain.corsCredentials,
  maxAgeSeconds: config.wordChain.corsMaxAgeSeconds
});

app.use('/api/v1', wordChainCors);

app.use(
  '/api/v1',
  createWordChainRouter({
    service: wordChainService,
    rateLimiter: wordChainRateLimiter,
    logger,
    apiPrefix: '/api/v1'
  })
);

// Enhanced error handlers
app.use((err, req, res, next) => {
  const classified = ErrorClassifier.classify(err);
  logger.error('Unhandled error', {
    requestId: req.requestId,
    context: 'error-handler',
    errorType: classified.type,
    errorCode: classified.code,
    message: classified.message,
    path: req.path,
    method: req.method,
    stack: config.environment === 'development' ? err.stack : undefined
  });

  res.status(classified.code === 'UNKNOWN' ? 500 : classified.code).json({
    error: classified.message,
    errorType: classified.type,
    errorCode: classified.code,
    path: req.path,
    method: req.method
  });
});

// 429 error handler with rate limiting info
app.use((err, req, res, next) => {
  if (err.status === 429) {
    logger.warn('Rate limit exceeded', {
      requestId: req.requestId,
      context: 'rate-limit-handler',
      path: req.path,
      message: err.message
    });

    // Check if retry-after header is present
    const retryAfter = err.response?.headers?.['retry-after'] || 60;
    res.setHeader('Retry-After', retryAfter);

    return res.status(429).json({
      error: 'Too Many Requests',
      message: err.message,
      retryAfter: parseInt(retryAfter),
      path: req.path,
      method: req.method
    });
  }
  next(err);
});

// Server error handler (500)
app.use((err, req, res, next) => {
  if (err.status >= 500) {
    logger.error('Server error', {
      requestId: req.requestId,
      context: 'server-error-handler',
      path: req.path,
      message: err.message,
      stack: config.environment === 'development' ? err.stack : undefined
    });

    res.status(err.status || 500).json({
      error: 'Internal Server Error',
      message: err.message,
      path: req.path,
      method: req.method
    });
  }
  next(err);
});

// Network error handler
app.use((err, req, res, next) => {
  if (err.code && ['ETIMEDOUT', 'ECONNREFUSED', 'ECONNRESET'].includes(err.code)) {
    logger.error('Network error', {
      requestId: req.requestId,
      context: 'network-error-handler',
      path: req.path,
      message: err.message,
      code: err.code
    });

    res.status(503).json({
      error: 'Service Unavailable',
      message: err.message,
      errorType: 'NETWORK_ERROR',
      errorCode: err.code,
      path: req.path,
      method: req.method
    });
  }
  next(err);
});

// 404 handler
app.use((req, res) => {
  logger.warn('404 Not Found', {
    requestId: req.requestId,
    context: 'not-found-handler',
    path: req.path,
    method: req.method
  });
  res.status(404).json({
    error: 'Not Found',
    message: `Cannot ${req.method} ${req.path}`,
    path: req.path,
    method: req.method
  });
});

// Graceful shutdown handlers
const gracefulShutdown = (signal) => {
  logger.info(`${signal} received, initiating graceful shutdown`, {
    context: 'graceful-shutdown',
    requestId: generateRequestId()
  });
  logger.info('Circuit breakers state before shutdown', {
    requestId: generateRequestId(),
    context: 'graceful-shutdown',
    circuitBreakers: {
      gameService: circuitBreakers.gameService.getState(),
      externalAPI: circuitBreakers.externalAPI.getState()
    }
  });
  logger.info('Degraded mode before shutdown', {
    requestId: generateRequestId(),
    context: 'graceful-shutdown',
    degradedMode: degradedMode.getStats()
  });

  // Close circuit breakers
  Object.values(circuitBreakers).forEach(cb => {
    cb.failureCount = 0;
    cb.state = 'CLOSED';
  });
  degradedMode.enabled = false;

  // Give time for pending requests to complete
  setTimeout(() => {
    logger.info('Server shutdown complete', {
      requestId: generateRequestId(),
      context: 'graceful-shutdown'
    });
    process.exit(0);
  }, 2000);
};

process.on('SIGTERM', () => gracefulShutdown('SIGTERM'));
process.on('SIGINT', () => gracefulShutdown('SIGINT'));

// Start server
/**
 * 启动监听。port 传 0 时由系统分配空闲端口(测试用), 真实端口可通过返回的 server.address() 获取。
 * @param {number|string} [port]
 * @returns {Promise<import('http').Server>}
 */
function startServer(port) {
  const listenPort = port === undefined ? config.port : port;
  return new Promise((resolve, reject) => {
    httpServer = app.listen(listenPort, () => {
      logger.info('='.repeat(60));
      logger.info('Game Server Started', {
        context: 'server-startup',
        port: internalBaseUrl(),
        environment: config.environment,
        logLevel: config.logLevel,
        requestIdHeader: config.requestIdHeader,
        enableAuditLog: config.enableAuditLog,
        enablePerformanceLog: config.enablePerformanceLog,
        wordChainRateLimit: {
          windowMs: config.wordChain.rateLimitWindowMs,
          max: config.wordChain.rateLimitMax,
          hourWindowMs: config.wordChain.rateLimitHourWindowMs,
          hourlyMax: config.wordChain.rateLimitHourlyMax,
          burstWindowMs: config.wordChain.rateLimitBurstWindowMs,
          burstMax: config.wordChain.rateLimitBurstMax
        }
      });
      logger.info('='.repeat(60));
      resolve(httpServer);
    });
    httpServer.on('error', reject);
  });
}

// 只有在直接执行 `node server.js` 时才监听端口; 被测试 require 时由测试显式 startServer(0)。
if (require.main === module) {
  startServer();
}

module.exports = {
  app,
  startServer,
  internalBaseUrl,
  fetchWithRetry,
  config,
  logger,
  circuitBreakers,
  degradedMode,
  wordChainService,
  wordChainRateLimiter,
  wordChainCors,
  /** 控制台 printf 格式 (导出以便回归测试: 曾经被 printf(printf) 包错而启动即崩) */
  consoleFormat,
  /** 关闭 winston 文件句柄, 让测试进程可以正常退出 */
  closeLogger() {
    return new Promise((resolve) => {
      logger.close();
      resolve();
    });
  }
};
