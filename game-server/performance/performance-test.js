// Performance testing and optimization for game server
const axios = require('axios');
const http = require('http');
const https = require('https');
const { performance } = require('perf_hooks');

// Configuration
const config = {
  targetUrl: 'http://localhost:3000',
  requestsPerSecond: 100,
  durationSeconds: 60,
  concurrency: 10,
  warmupRequests: 100,
};

// Results tracking
let results = {
  totalRequests: 0,
  successfulRequests: 0,
  failedRequests: 0,
  totalLatency: 0,
  minLatency: Infinity,
  maxLatency: 0,
  latencyDistribution: {},
  errorTypes: {},
};

// Performance metrics
const performanceMetrics = {
  cpuUsage: 0,
  memoryUsage: 0,
  throughput: 0,
  errorRate: 0,
  avgLatency: 0,
  p95Latency: 0,
  p99Latency: 0,
};

// Load test function
async function runLoadTest() {
  console.log('Starting load test...');
  console.log(`Target: ${config.targetUrl}`);
  console.log(`Requests per second: ${config.requestsPerSecond}`);
  console.log(`Duration: ${config.durationSeconds}s`);
  console.log(`Concurrency: ${config.concurrency}`);
  console.log('='.repeat(60));

  const startTime = performance.now();
  let completedRequests = 0;

  // Create request queue
  const requestQueue = [];
  for (let i = 0; i < config.requestsPerSecond; i++) {
    requestQueue.push(i);
  }

  // Warmup
  console.log('\nWarming up...');
  for (let i = 0; i < config.warmupRequests; i++) {
    await makeRequest('/health');
    completedRequests++;
  }
  console.log(`Warmup complete. ${completedRequests} requests sent.`);

  // Main load test
  console.log('\nStarting load test...');
  const testStartTime = performance.now();

  await Promise.all(
    Array.from({ length: config.concurrency }).map(async () => {
      while (completedRequests < config.requestsPerSecond * config.durationSeconds) {
        const requestIndex = requestQueue.shift();
        await makeRequest(`/health?request=${requestIndex}`);
        requestQueue.push(requestIndex);
        completedRequests++;
      }
    })
  );

  const testEndTime = performance.now();
  const duration = (testEndTime - testStartTime) / 1000;

  // Calculate results
  results.totalRequests = completedRequests;
  results.successfulRequests = results.totalRequests - results.failedRequests;
  results.throughput = results.totalRequests / duration;
  results.errorRate = (results.failedRequests / results.totalRequests) * 100;
  results.avgLatency = results.totalLatency / results.totalRequests;

  // Calculate percentiles
  calculatePercentiles();

  // Print results
  printResults();

  // Performance recommendations
  provideRecommendations();
}

// Make a single request
async function makeRequest(path) {
  const startTime = performance.now();

  try {
    const response = await axios.get(`${config.targetUrl}${path}`, {
      timeout: 5000,
      validateStatus: () => true, // Don't throw on non-2xx status codes
    });

    const latency = performance.now() - startTime;

    // Update results
    results.totalRequests++;
    results.successfulRequests++;
    results.totalLatency += latency;

    if (latency < results.minLatency) results.minLatency = latency;
    if (latency > results.maxLatency) results.maxLatency = latency;

    // Update latency distribution
    const latencyBucket = Math.floor(latency / 100);
    results.latencyDistribution[latencyBucket] = (results.latencyDistribution[latencyBucket] || 0) + 1;

    // Update error types
    if (response.status >= 400) {
      results.errorTypes[response.status] = (results.errorTypes[response.status] || 0) + 1;
    }
  } catch (error) {
    results.failedRequests++;

    const latency = performance.now() - startTime;
    results.totalLatency += latency;

    if (latency < results.minLatency) results.minLatency = latency;
    if (latency > results.maxLatency) results.maxLatency = latency;

    const errorType = error.response?.status || error.code || 'UNKNOWN';
    results.errorTypes[errorType] = (results.errorTypes[errorType] || 0) + 1;
  }
}

// Calculate percentiles
function calculatePercentiles() {
  const latencies = Object.keys(results.latencyDistribution)
    .map(Number)
    .sort((a, b) => a - b);

  const total = latencies.length;

  // P95
  const p95Index = Math.floor(total * 0.95);
  results.p95Latency = latencies[p95Index];

  // P99
  const p99Index = Math.floor(total * 0.99);
  results.p99Latency = latencies[p99Index];
}

// Print results
function printResults() {
  console.log('\n' + '='.repeat(60));
  console.log('LOAD TEST RESULTS');
  console.log('='.repeat(60));

  console.log('\nGeneral Metrics:');
  console.log(`  Total Requests:     ${results.totalRequests}`);
  console.log(`  Successful Requests: ${results.successfulRequests}`);
  console.log(`  Failed Requests:     ${results.failedRequests}`);
  console.log(`  Error Rate:          ${results.errorRate.toFixed(2)}%`);
  console.log(`  Throughput:          ${results.throughput.toFixed(2)} req/s`);
  console.log(`  Duration:            ${(results.totalLatency / 1000).toFixed(2)}s`);

  console.log('\nLatency Metrics:');
  console.log(`  Average Latency:     ${results.avgLatency.toFixed(2)}ms`);
  console.log(`  Min Latency:         ${results.minLatency.toFixed(2)}ms`);
  console.log(`  Max Latency:         ${results.maxLatency.toFixed(2)}ms`);
  console.log(`  P95 Latency:         ${results.p95Latency.toFixed(2)}ms`);
  console.log(`  P99 Latency:         ${results.p99Latency.toFixed(2)}ms`);

  if (Object.keys(results.errorTypes).length > 0) {
    console.log('\nError Types:');
    for (const [status, count] of Object.entries(results.errorTypes)) {
      const percentage = ((count / results.totalRequests) * 100).toFixed(2);
      console.log(`  ${status}: ${count} (${percentage}%)`);
    }
  }

  console.log('\n' + '='.repeat(60));
}

// Provide performance recommendations
function provideRecommendations() {
  console.log('\nPERFORMANCE RECOMMENDATIONS');
  console.log('='.repeat(60));

  const recommendations = [];

  // Error rate
  if (results.errorRate > 1) {
    recommendations.push({
      severity: 'high',
      message: 'High error rate detected',
      suggestion: 'Investigate and fix the failing requests',
    });
  }

  // Latency
  if (results.avgLatency > 100) {
    recommendations.push({
      severity: 'medium',
      message: 'High average latency detected',
      suggestion: 'Optimize database queries, consider caching, or reduce payload sizes',
    });
  }

  if (results.p95Latency > 500) {
    recommendations.push({
      severity: 'high',
      message: 'High P95 latency detected',
      suggestion: 'Review slow queries, implement connection pooling, or add caching',
    });
  }

  if (results.p99Latency > 1000) {
    recommendations.push({
      severity: 'critical',
      message: 'Very high P99 latency detected',
      suggestion: 'Critical performance issue - immediate investigation required',
    });
  }

  // Throughput
  if (results.throughput < 50) {
    recommendations.push({
      severity: 'medium',
      message: 'Low throughput detected',
      suggestion: 'Review server resources, optimize code, or scale infrastructure',
    });
  }

  // Print recommendations
  if (recommendations.length === 0) {
    console.log('\n✓ All performance metrics are within acceptable ranges!');
  } else {
    recommendations.forEach((rec, index) => {
      console.log(`\n${index + 1}. [${rec.severity.toUpperCase()}] ${rec.message}`);
      console.log(`   ${rec.suggestion}`);
    });
  }

  console.log('\n' + '='.repeat(60));
}

// Performance monitoring and optimization functions
function monitorPerformance() {
  // Monitor memory usage
  const memoryUsage = process.memoryUsage();
  const memoryInfo = {
    rss: `${Math.round(memoryUsage.rss / 1024 / 1024)} MB`,
    heapTotal: `${Math.round(memoryUsage.heapTotal / 1024 / 1024)} MB`,
    heapUsed: `${Math.round(memoryUsage.heapUsed / 1024 / 1024)} MB`,
    external: `${Math.round(memoryUsage.external / 1024 / 1024)} MB`,
  };

  // Monitor CPU usage (simplified)
  const cpuUsage = process.cpuUsage();

  console.log('\nPERFORMANCE MONITORING');
  console.log('='.repeat(60));
  console.log('\nMemory Usage:');
  Object.entries(memoryInfo).forEach(([key, value]) => {
    console.log(`  ${key}: ${value}`);
  });
  console.log('\nCPU Usage:');
  console.log(`  User: ${(cpuUsage.user / 1000).toFixed(2)}ms`);
  console.log(`  System: ${(cpuUsage.system / 1000).toFixed(2)}ms`);
  console.log('='.repeat(60));
}

// Optimize server performance
function optimizePerformance() {
  console.log('\nPERFORMANCE OPTIMIZATION');
  console.log('='.repeat(60));

  const optimizations = [
    {
      category: 'Database',
      recommendations: [
        'Implement connection pooling',
        'Add database indexes',
        'Use prepared statements',
        'Implement query caching',
      ],
    },
    {
      category: 'Application',
      recommendations: [
        'Implement object pooling',
        'Use lazy loading',
        'Optimize loops and algorithms',
        'Minimize garbage collection',
      ],
    },
    {
      category: 'Network',
      recommendations: [
        'Enable HTTP/2',
        'Implement response compression',
        'Use CDN for static assets',
        'Optimize asset sizes',
      ],
    },
    {
      category: 'Infrastructure',
      recommendations: [
        'Scale horizontally',
        'Use load balancers',
        'Implement caching layers',
        'Monitor and auto-scale',
      ],
    },
  ];

  optimizations.forEach((category) => {
    console.log(`\n${category.category}:`);
    category.recommendations.forEach((rec) => {
      console.log(`  - ${rec}`);
    });
  });

  console.log('\n' + '='.repeat(60));
}

// Run load test
if (require.main === module) {
  runLoadTest().catch((error) => {
    console.error('Load test failed:', error);
    process.exit(1);
  });
}

module.exports = {
  runLoadTest,
  monitorPerformance,
  optimizePerformance,
  config,
  results,
  performanceMetrics,
};
