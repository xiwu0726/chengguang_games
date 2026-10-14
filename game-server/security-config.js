// Security configuration for game server
const security = require('./security/requirements');

// Apply security middleware to the app
security.applySecurityMiddleware(app);

// Apply authentication middleware to protected routes
app.get('/api/game-status', security.authenticateAPI, (req, res) => {
  res.json({
    status: 'online',
    message: 'Server is running',
    errorType: null,
    requestId: req.requestId,
    user: req.user
  });
});

// Apply API key authentication to public API
app.get('/api/public', security.authenticateAPIKey, (req, res) => {
  res.json({
    message: 'Public API endpoint',
    apiKey: req.apiKey
  });
});

// Apply IP whitelist to admin routes
app.get('/admin', security.ipWhitelist, (req, res) => {
  res.json({
    message: 'Welcome to admin panel'
  });
});

// Example of using custom rate limiting
const { rateLimitByIP } = require('./security/requirements');

// 1000 requests per hour per IP for the health check endpoint
app.get('/health', rateLimitByIP(1000, 60 * 60 * 1000), (req, res) => {
  res.json({
    status: 'healthy',
    timestamp: new Date().toISOString(),
    uptime: process.uptime(),
    requestId: req.requestId,
    user: req.user
  });
});
