// Security middleware for game server
const helmet = require('helmet');
const rateLimit = require('express-rate-limit');
const xss = require('xss-clean');
const mongoSanitize = require('express-mongo-sanitize');
const morgan = require('morgan');
const validator = require('express-validator');

// Helmet security headers
const helmetConfig = helmet({
  contentSecurityPolicy: {
    directives: {
      defaultSrc: ["'self'"],
      scriptSrc: ["'self'", "'unsafe-inline'"],
      styleSrc: ["'self'", "'unsafe-inline'"],
      imgSrc: ["'self'", 'data:', 'https:'],
      connectSrc: ["'self'"],
      fontSrc: ["'self'"],
      objectSrc: ["'none'"],
      mediaSrc: ["'self'"],
      frameSrc: ["'none'"],
      baseUri: ["'self'"],
      formAction: ["'self'"],
    },
  },
  hsts: {
    maxAge: 31536000,
    includeSubDomains: true,
    preload: true,
  },
});

// Rate limiting
const createRateLimiter = (options = {}) => {
  return rateLimit({
    windowMs: options.windowMs || 15 * 60 * 1000, // 15 minutes
    max: options.max || 100, // limit each IP to 100 requests per windowMs
    message: {
      error: 'Too many requests from this IP, please try again later',
      retryAfter: Math.ceil(options.windowMs / 1000),
    },
    standardHeaders: true,
    legacyHeaders: false,
    // Skip rate limiting for health check and metrics endpoints
    skip: (req) => {
      return req.path === '/health' || req.path === '/metrics';
    },
  });
};

// Rate limiters
const generalRateLimiter = createRateLimiter({
  windowMs: 15 * 60 * 1000,
  max: 100,
  message: 'Too many requests, please try again later',
});

const apiRateLimiter = createRateLimiter({
  windowMs: 1 * 60 * 1000, // 1 minute
  max: 60, // 60 requests per minute
  message: 'Too many API requests, please slow down',
});

const authRateLimiter = createRateLimiter({
  windowMs: 15 * 60 * 1000,
  max: 10, // 10 attempts per 15 minutes
  message: 'Too many authentication attempts, please try again later',
});

// Input validation middleware
const validateRequest = (req, res, next) => {
  const errors = validator.validationResult(req);
  if (!errors.isEmpty()) {
    return res.status(400).json({
      error: 'Validation failed',
      details: errors.array(),
    });
  }
  next();
};

// Sanitize inputs
const sanitizeInputs = (req, res, next) => {
  // MongoDB injection prevention
  req.body = mongoSanitize(req.body);
  req.query = mongoSanitize(req.query);
  req.params = mongoSanitize(req.params);

  // XSS prevention
  if (req.body) {
    req.body = xss(req.body);
  }
  if (req.query) {
    req.query = xss(req.query);
  }
  if (req.params) {
    req.params = xss(req.params);
  }

  next();
};

// Security headers middleware
const addSecurityHeaders = (req, res, next) => {
  res.setHeader('X-Content-Type-Options', 'nosniff');
  res.setHeader('X-Frame-Options', 'DENY');
  res.setHeader('X-XSS-Protection', '1; mode=block');
  res.setHeader('Referrer-Policy', 'strict-origin-when-cross-origin');
  res.setHeader('Permissions-Policy', 'geolocation=(), microphone=(), camera=()');
  next();
};

// Request ID middleware
const requestID = (req, res, next) => {
  req.requestId = req.headers['x-request-id'] || req.headers['x-request-id'] || crypto.randomUUID();
  res.setHeader('X-Request-ID', req.requestId);
  next();
};

// CORS middleware
const corsConfig = (req, res, next) => {
  res.header('Access-Control-Allow-Origin', process.env.ALLOWED_ORIGINS || '*');
  res.header('Access-Control-Allow-Methods', 'GET, POST, PUT, DELETE, OPTIONS');
  res.header('Access-Control-Allow-Headers', 'Content-Type, Authorization, X-Request-ID');
  res.header('Access-Control-Allow-Credentials', 'true');

  // Handle preflight requests
  if (req.method === 'OPTIONS') {
    return res.sendStatus(200);
  }

  next();
};

// Audit logging middleware
const auditLog = (req, res, next) => {
  const start = Date.now();

  res.on('finish', () => {
    const duration = Date.now() - start;
    const logData = {
      requestId: req.requestId,
      method: req.method,
      path: req.path,
      statusCode: res.statusCode,
      ip: req.ip,
      userAgent: req.get('user-agent'),
      duration: `${duration}ms`,
      timestamp: new Date().toISOString(),
    };

    // Only log failed requests (4xx, 5xx) to audit log
    if (res.statusCode >= 400) {
      logger.error('Audit log - Failed request', logData);
    } else {
      logger.info('Audit log - Successful request', logData);
    }
  });

  next();
};

// Rate limit by IP
const rateLimitByIP = (limit, windowMs) => {
  return rateLimit({
    windowMs,
    max: limit,
    keyGenerator: (req) => req.ip,
    message: 'Too many requests from this IP',
    standardHeaders: true,
    legacyHeaders: false,
    skip: (req) => req.path === '/health' || req.path === '/metrics',
  });
};

module.exports = {
  helmetConfig,
  generalRateLimiter,
  apiRateLimiter,
  authRateLimiter,
  validateRequest,
  sanitizeInputs,
  addSecurityHeaders,
  requestID,
  corsConfig,
  auditLog,
  rateLimitByIP,
};
