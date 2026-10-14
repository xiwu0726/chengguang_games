// Security requirements and middleware setup
const express = require('express');
const helmet = require('helmet');
const rateLimit = require('express-rate-limit');
const xss = require('xss-clean');
const mongoSanitize = require('express-mongo-sanitize');
const validator = require('express-validator');

// Security middleware
const security = require('./middleware');

// Apply security middleware to app
const applySecurityMiddleware = (app) => {
  // Helmet security headers
  app.use(security.helmetConfig);

  // Security headers
  app.use(security.addSecurityHeaders);

  // Request ID tracking
  app.use(security.requestID);

  // CORS
  app.use(security.corsConfig);

  // MongoDB injection prevention
  app.use(mongoSanitize());

  // XSS prevention
  app.use(xss());

  // Input validation
  app.use(validator);

  // Rate limiting
  app.use('/api', security.apiRateLimiter);
  app.use('/auth', security.authRateLimiter);

  // General rate limiting
  app.use(security.generalRateLimiter);

  // Audit logging
  app.use(security.auditLog);

  // Security headers (additional)
  app.use((req, res, next) => {
    // HSTS
    if (req.secure || req.headers['x-forwarded-proto'] === 'https') {
      res.setHeader('Strict-Transport-Security', 'max-age=31536000; includeSubDomains; preload');
    }

    // Prevent clickjacking
    res.setHeader('X-Frame-Options', 'DENY');

    // Prevent MIME type sniffing
    res.setHeader('X-Content-Type-Options', 'nosniff');

    // XSS protection
    res.setHeader('X-XSS-Protection', '1; mode=block');

    // Referrer policy
    res.setHeader('Referrer-Policy', 'strict-origin-when-cross-origin');

    // Permissions policy
    res.setHeader('Permissions-Policy', 'geolocation=(), microphone=(), camera=()');

    // Content Security Policy
    res.setHeader('Content-Security-Policy', "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; connect-src 'self'; font-src 'self'; object-src 'none'; media-src 'self'; frame-src 'none'; base-uri 'self'; form-action 'self'");

    next();
  });
};

// API authentication middleware
const authenticateAPI = (req, res, next) => {
  const authHeader = req.headers.authorization;

  if (!authHeader || !authHeader.startsWith('Bearer ')) {
    return res.status(401).json({
      error: 'Unauthorized',
      message: 'Access token is required',
    });
  }

  const token = authHeader.substring(7); // Remove 'Bearer ' prefix

  // Validate token (simplified - in production use JWT verification)
  try {
    // Here you would verify the JWT token
    // For now, we'll just check if the token exists
    req.user = {
      id: 'user-123',
      token: token,
    };

    next();
  } catch (error) {
    return res.status(401).json({
      error: 'Unauthorized',
      message: 'Invalid or expired token',
    });
  }
};

// API key authentication
const authenticateAPIKey = (req, res, next) => {
  const apiKey = req.headers['x-api-key'];

  if (!apiKey) {
    return res.status(401).json({
      error: 'Unauthorized',
      message: 'API key is required',
    });
  }

  // Validate API key (simplified - in production use a secure key store)
  const validAPIKeys = process.env.API_KEYS?.split(',') || ['dev-api-key'];

  if (!validAPIKeys.includes(apiKey)) {
    return res.status(403).json({
      error: 'Forbidden',
      message: 'Invalid API key',
    });
  }

  req.apiKey = apiKey;
  next();
};

// Check if the request is from a trusted IP
const isTrustedIP = (ip) => {
  const trustedIPs = process.env.TRUSTED_IPS?.split(',') || [];

  // Allow localhost and IPv4 localhost
  if (ip === '127.0.0.1' || ip === '::1' || ip === 'localhost') {
    return true;
  }

  return trustedIPs.includes(ip);
};

// IP whitelist middleware
const ipWhitelist = (req, res, next) => {
  const clientIP = req.ip || req.connection.remoteAddress;

  if (!isTrustedIP(clientIP)) {
    return res.status(403).json({
      error: 'Forbidden',
      message: 'Your IP address is not allowed to access this resource',
    });
  }

  next();
};

// Input validation rules
const validationRules = {
  // Email validation
  email: [
    validator.body('email')
      .trim()
      .isEmail()
      .normalizeEmail()
      .withMessage('Please provide a valid email address'),
  ],

  // Password validation
  password: [
    validator.body('password')
      .isLength({ min: 8, max: 128 })
      .withMessage('Password must be between 8 and 128 characters'),
    validator.body('password')
      .matches(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)/)
      .withMessage('Password must contain at least one uppercase letter, one lowercase letter, and one digit'),
  ],

  // Username validation
  username: [
    validator.body('username')
      .trim()
      .isLength({ min: 3, max: 32 })
      .withMessage('Username must be between 3 and 32 characters'),
    validator.body('username')
      .matches(/^[a-zA-Z0-9_]+$/)
      .withMessage('Username can only contain letters, numbers, and underscores'),
  ],

  // URL validation
  url: [
    validator.body('url')
      .trim()
      .isURL()
      .withMessage('Please provide a valid URL'),
  ],

  // IP address validation
  ipAddress: [
    validator.body('ipAddress')
      .trim()
      .isIP()
      .withMessage('Please provide a valid IP address'),
  ],

  // Integer validation
  integer: [
    validator.body('value')
      .isInt()
      .withMessage('Please provide a valid integer'),
  ],

  // Positive integer validation
  positiveInteger: [
    validator.body('value')
      .isInt({ min: 1 })
      .withMessage('Please provide a valid positive integer'),
  ],
};

module.exports = {
  applySecurityMiddleware,
  authenticateAPI,
  authenticateAPIKey,
  ipWhitelist,
  validationRules,
  isTrustedIP,
};
