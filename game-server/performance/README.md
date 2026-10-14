# Performance Testing and Optimization

This module provides comprehensive performance testing and optimization tools for the game server.

> Note: the file name `performance-test.js` matches Node's built-in test-file glob (`*-test.js`),
> so a bare `node --test` would try to execute this load-test script as a unit test.
> Run the real suite with `npm test`, and this load test with `npm run test:perf`.

## Features

### 1. Load Testing
- Concurrent request handling
- Configurable requests per second
- Configurable duration
- Configurable concurrency
- Warmup period
- Detailed results reporting

### 2. Performance Monitoring
- Memory usage tracking
- CPU usage monitoring
- Real-time metrics

### 3. Performance Recommendations
- Automatic recommendations based on metrics
- Categorized by severity
- Actionable suggestions

## Installation

Install required dependencies:

```bash
npm install axios
```

## Usage

### Running Load Tests

```javascript
const { runLoadTest } = require('./performance/performance-test');

// Run default load test
runLoadTest();
```

### Configuring Load Test

```javascript
const { runLoadTest, config } = require('./performance/performance-test');

// Configure the load test
config.requestsPerSecond = 200;
config.durationSeconds = 120;
config.concurrency = 20;
config.warmupRequests = 200;

runLoadTest();
```

### Monitoring Performance

```javascript
const { monitorPerformance } = require('./performance/performance-test');

// Monitor current performance
monitorPerformance();
```

### Optimizing Performance

```javascript
const { optimizePerformance } = require('./performance/performance-test');

// Get optimization recommendations
optimizePerformance();
```

## Load Test Results

The load test provides the following metrics:

### General Metrics
- Total requests
- Successful requests
- Failed requests
- Error rate
- Throughput (req/s)
- Duration

### Latency Metrics
- Average latency
- Min latency
- Max latency
- P95 latency
- P99 latency

### Error Types
- Breakdown by HTTP status code
- Percentage of errors

### Recommendations

Based on the results, the system provides recommendations:

- **Critical**: Immediate action required
- **High**: Should be addressed soon
- **Medium**: Should be addressed
- **Low**: Nice to have

## Performance Optimization

### Database Optimization

1. **Connection Pooling**
   - Use connection pools to reduce connection overhead
   - Configure appropriate pool size

2. **Indexes**
   - Add indexes to frequently queried columns
   - Use composite indexes for complex queries

3. **Caching**
   - Implement query caching
   - Use Redis or Memcached

4. **Prepared Statements**
   - Use prepared statements to prevent SQL injection
   - Improve query performance

### Application Optimization

1. **Object Pooling**
   - Reuse objects instead of creating new ones
   - Reduce memory allocation

2. **Lazy Loading**
   - Load resources on-demand
   - Reduce initial load time

3. **Algorithm Optimization**
   - Optimize loops and algorithms
   - Use efficient data structures

4. **Garbage Collection**
   - Minimize memory allocation
   - Use object pooling

### Network Optimization

1. **HTTP/2**
   - Enable HTTP/2 for multiplexing
   - Reduce latency

2. **Compression**
   - Enable response compression
   - Reduce payload size

3. **CDN**
   - Use CDN for static assets
   - Reduce server load

4. **Asset Optimization**
   - Optimize image sizes
   - Minify CSS and JavaScript

### Infrastructure Optimization

1. **Horizontal Scaling**
   - Scale horizontally to handle more load
   - Use load balancers

2. **Caching Layers**
   - Implement multi-level caching
   - Reduce database load

3. **Auto-scaling**
   - Implement auto-scaling based on load
   - Automatically scale resources

4. **Monitoring**
   - Monitor system metrics
   - Proactively identify issues

## Performance Benchmarks

### Target Metrics

| Metric | Target | Warning | Critical |
|--------|--------|---------|----------|
| Throughput | > 100 req/s | 50-100 req/s | < 50 req/s |
| Avg Latency | < 100ms | 100-500ms | > 500ms |
| P95 Latency | < 500ms | 500-1000ms | > 1000ms |
| P99 Latency | < 1000ms | 1000-2000ms | > 2000ms |
| Error Rate | < 1% | 1-5% | > 5% |

### Running Benchmarks

```javascript
const { runLoadTest } = require('./performance/performance-test');

// Run benchmark
runLoadTest();
```

## Integration with CI/CD

### Continuous Performance Testing

```javascript
// In your CI/CD pipeline
const { runLoadTest } = require('./performance/performance-test');

async function runPerformanceTests() {
  try {
    await runLoadTest();
    console.log('Performance tests passed!');
  } catch (error) {
    console.error('Performance tests failed:', error);
    process.exit(1);
  }
}

runPerformanceTests();
```

## Performance Testing Best Practices

1. **Test Under Load**
   - Test with realistic load
   - Test peak load scenarios
   - Test gradual load increase

2. **Monitor Resources**
   - Monitor CPU, memory, network
   - Identify bottlenecks

3. **Test Regularly**
   - Run tests on every commit
   - Track performance trends
   - Catch regressions early

4. **Compare Results**
   - Compare before and after optimizations
   - Track improvement
   - Validate optimizations

5. **Document Results**
   - Keep records of performance tests
   - Document optimization results
   - Share findings with the team

## Troubleshooting

### High Latency

**Possible Causes:**
- Database queries are slow
- Network latency
- Insufficient resources
- Code inefficiencies

**Solutions:**
- Optimize database queries
- Add caching
- Scale infrastructure
- Optimize code

### High Error Rate

**Possible Causes:**
- Server overload
- Resource exhaustion
- Network issues
- Code bugs

**Solutions:**
- Scale resources
- Implement rate limiting
- Fix bugs
- Improve error handling

### Low Throughput

**Possible Causes:**
- Bottlenecks
- Resource constraints
- Inefficient code
- Network limitations

**Solutions:**
- Identify and fix bottlenecks
- Scale resources
- Optimize code
- Use CDNs

## License

MIT
