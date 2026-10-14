# Security Module

This module provides comprehensive security features for the game server, including:

- **Helmet** - Security headers configuration
- **Rate Limiting** - API rate limiting with different strategies
- **XSS Prevention** - Cross-Site Scripting protection
- **Input Sanitization** - MongoDB injection and XSS prevention
- **Input Validation** - Request validation with express-validator
- **Authentication** - API token and API key authentication
- **Audit Logging** - Logging of all requests for security auditing
- **CORS** - Cross-Origin Resource Sharing configuration

## Installation

Install the required dependencies:

```bash
npm install helmet express-rate-limit xss-clean express-mongo-sanitize express-validator morgan
```

## Usage

### Apply Security Middleware

```javascript
const express = require('express');
const { applySecurityMiddleware } = require('./security/requirements');

const app = express();

// Apply all security middleware
applySecurityMiddleware(app);

// Your routes here
app.get('/api/game-status', authenticateAPI, (req, res) => {
  res.json({ status: 'online' });
});
```

### Rate Limiting

Different rate limiters are applied to different routes:

- **General Rate Limiter**: 100 requests per 15 minutes (all routes)
- **API Rate Limiter**: 60 requests per 1 minute (`/api/*`)
- **Auth Rate Limiter**: 10 attempts per 15 minutes (`/auth/*`)

### Input Validation

Use the predefined validation rules:

```javascript
const { validationRules } = require('./security/requirements');

app.post('/api/users', validationRules.email, validationRules.password, validateRequest, (req, res) => {
  // Process user registration
});
```

### Authentication

#### API Token Authentication

```javascript
const { authenticateAPI } = require('./security/requirements');

app.get('/api/data', authenticateAPI, (req, res) => {
  // Access the authenticated user
  res.json({ user: req.user });
});
```

#### API Key Authentication

```javascript
const { authenticateAPIKey } = require('./security/requirements');

app.get('/api/public', authenticateAPIKey, (req, res) => {
  // Access the authenticated API key
  res.json({ apiKey: req.apiKey });
});
```

### IP Whitelist

Restrict access to specific IP addresses:

```javascript
const { ipWhitelist } = require('./security/requirements');

app.get('/admin', ipWhitelist, (req, res) => {
  // Admin route - only accessible from trusted IPs
  res.json({ message: 'Welcome to admin panel' });
});
```

## Security Features

### 1. Security Headers

The following security headers are automatically applied:

- `Content-Security-Policy` - Prevents XSS attacks
- `X-Content-Type-Options` - Prevents MIME type sniffing
- `X-Frame-Options` - Prevents clickjacking
- `X-XSS-Protection` - Enables browser XSS filtering
- `Strict-Transport-Security` - Enforces HTTPS
- `Referrer-Policy` - Controls referrer information
- `Permissions-Policy` - Controls browser features

### 2. Rate Limiting

Rate limiting prevents abuse and DDoS attacks:

```javascript
const { rateLimitByIP } = require('./security/requirements');

// 1000 requests per hour per IP
app.use('/api', rateLimitByIP(1000, 60 * 60 * 1000));
```

### 3. Input Sanitization

MongoDB injection and XSS attacks are prevented:

```javascript
app.use(mongoSanitize()); // Prevents MongoDB injection
app.use(xss()); // Prevents XSS attacks
```

### 4. Audit Logging

All requests are logged for security auditing:

```javascript
// Failed requests (4xx, 5xx) are logged to audit log
// Successful requests are logged to info log
```

## Environment Variables

Configure security settings using environment variables:

```bash
# Allowed origins for CORS
ALLOWED_ORIGINS=http://localhost:3000,https://yourdomain.com

# API keys (comma-separated)
API_KEYS=your-api-key-1,your-api-key-2

# Trusted IPs (comma-separated)
TRUSTED_IPS=127.0.0.1,192.168.1.100

# Enable audit logging
AUDIT_LOG_ENABLED=true
```

## Security Best Practices

1. **Always use HTTPS** - Enforce HSTS in production
2. **Use strong authentication** - Implement JWT or OAuth2
3. **Regularly update dependencies** - Keep packages up to date
4. **Validate all inputs** - Always validate user inputs
5. **Log security events** - Monitor for suspicious activity
6. **Use environment variables** - Never hardcode secrets
7. **Implement rate limiting** - Prevent abuse
8. **Regular security audits** - Review and update security measures

## Testing

Test security features:

```bash
# Test rate limiting
curl -I http://localhost:3000/api/game-status

# Test authentication
curl -H "Authorization: Bearer your-token" http://localhost:3000/api/data

# Test validation
curl -X POST http://localhost:3000/api/users \
  -H "Content-Type: application/json" \
  -d '{"email":"invalid-email","password":"short"}'
```

## Security Checklist

- [x] Helmet security headers
- [x] Rate limiting
- [x] XSS prevention
- [x] MongoDB injection prevention
- [x] Input validation
- [x] API authentication
- [x] Audit logging
- [x] CORS configuration
- [x] IP whitelist
- [x] Security headers

## License

MIT
