# Troubleshooting Skill for EMScore

This skill provides systematic guidance for diagnosing and resolving common issues when working with the EMScore codebase.

## Diagnostic Approach

When encountering issues with EMScore, follow this structured approach:

1. **Gather Information**
   - What exactly is not working?
   - When did it start happening?
   - What changed before the issue appeared?
   - What error messages are displayed?
   - What are the steps to reproduce?

2. **Check Logs**
   - Application logs (backend, edge, plugins)
   - Docker container logs
   - Database logs
   - MQTT broker logs
   - System event logs

3. **Verify Environment**
   - Check service statuses
   - Validate configuration
   - Confirm resource availability
   - Check network connectivity

4. **Isolate the Problem**
   - Test components individually
   - Use minimal reproduction cases
   - Eliminate variables systematically
   - Check if issue persists across environments

5. **Implement and Test Fix**
   - Apply targeted solution
   - Verify fix resolves the issue
   - Ensure no regressions introduced
   - Monitor for recurrence

## Common Issue Categories

### 1. Database Connection Issues

#### Symptoms
- Application fails to start with database connection errors
- Timeout exceptions when querying data
- "Connection refused" or "timeout expired" errors
- Missing data in UI despite successful data ingestion

#### Diagnostic Steps
1. Check if PostgreSQL/TimescaleDB service is running:
   ```bash
   # Docker
   docker ps | grep timescaledb
   
   # System service
   systemctl status postgresql
   sudo -u postgres psql -l
   ```

2. Verify connection string in configuration:
   - Check src/EMSCore.Backend/appsettings.json
   - Check environment variables
   - Look for ConnectionStrings__DefaultConnection

3. Test database connectivity manually:
   ```bash
   # Using psql
   psql -h localhost -U postgres -d emscore -c "SELECT version();"
   
   # Using connection string from config
   psql "Host=localhost;Database=emscore;Username=postgres;Password=postgres" -c "SELECT 1;"
   ```

4. Check database user permissions:
   ```sql
   -- Login as postgres
   sudo -u postgres psql
   
   -- Check user exists and permissions
   \du
   \c emscore
   \dp
   ```

5. Verify TimescaleDB extension is installed:
   ```sql
   SELECT * FROM pg_extension WHERE extname = 'timescaledb';
   SELECT * FROM timescaledb_information.hypertables;
   ```

6. Check network connectivity:
   ```bash
   telnet localhost 5432
   nc -zv localhost 5432
   ```

#### Common Solutions
- Start database service: `docker-compose up -d timescaledb` or `systemctl start postgresql`
- Correct connection string in appsettings.json or environment variables
- Create missing database/user: `CREATE DATABASE emscore; CREATE USER ems_user WITH PASSWORD 'password';`
- Grant permissions: `GRANT ALL PRIVILEGES ON DATABASE emscore TO ems_user;`
- Install TimescaleDB extension: `CREATE EXTENSION IF NOT EXISTS timescaledb;`
- Fix firewall/network issues blocking port 5432
- Increase connection timeout in configuration

### 2. MQTT Connection Problems

#### Symptoms
- No telemetry data appearing in system
- MQTT connection rejected or disconnected messages
- High latency in data appearance
- "Connection refused" or "authorization failed" errors

#### Diagnostic Steps
1. Check if MQTT broker is running:
   ```bash
   # Docker
   docker ps | grep eclipse-mosquitto
   
   # System service
   systemctl status mosquitto
   ```

2. Verify broker accessibility:
   ```bash
   telnet localhost 1883
   nc -zv localhost 1883
   mosquitto_sub -h localhost -t "test/topic" -C 1
   ```

3. Check MQTT configuration in appsettings.json or environment variables:
   - Mqtt__BrokerHost
   - Mqtt__BrokerPort
   - Mqtt__Username
   - Mqtt__Password
   - Mqtt__UseTls
   - Mqtt__ClientId

4. Test MQTT publish/subscribe manually:
   ```bash
   # In one terminal
   mosquitto_sub -h localhost -t "ems/+/devices/+/measurements/+" -v
   
   # In another terminal
   mosquitto_pub -h localhost -t "ems/site-001/devices/solar-panel-001/measurements/power" -m '{"value":1500.5,"unit":"W","timestamp":"2024-01-01T12:00:00Z"}'
   ```

5. Check broker logs for connection attempts:
   ```bash
   docker-compose logs mqtt-broker
   # or
   journalctl -u mosquitto -f
   ```

6. Verify TLS settings if using encrypted connections:
   - Check certificate validity
   - Verify CA trust chain
   - Confirm protocol versions match

#### Common Solutions
- Start MQTT broker: `docker-compose up -d mqtt-broker` or `systemctl start mosquitto`
- Correct broker hostname/port in configuration
- Verify authentication credentials match broker configuration
- Ensure TLS certificates are valid and trusted
- Use unique client IDs for each connection
- Check firewall rules blocking port 1883 (or 8883 for TLS)
- Increase keep-alive interval if network is unstable
- Clear persistent sessions if experiencing subscription issues

### 3. Plugin Loading Failures

#### Symptoms
- Plugins not appearing in system
- Errors during startup about plugin loading
- Missing functionality that should be provided by plugins
- Plugin initialization exceptions in logs

#### Diagnostic Steps
1. Check plugin directory location:
   - Default: ./Plugins/ relative to executable
   - Configured path in configuration
   - Verify DLL files exist in expected location

2. Check plugin DLL validity:
   ```bash
   # Check if it's a valid .NET assembly
   dotnet ./Plugins/SomePlugin.dll
   
   # Check dependencies
   objdump -x ./Plugins/SomePlugin.dll | grep NEEDED
   ```

3. Verify .NET version compatibility:
   - Plugin compiled for .NET 10?
   - Check for missing dependencies
   - Look for version conflicts

4. Examine initialization logs:
   - Look for exceptions in PluginManager logs
   - Check InitializeAsync method execution
   - Verify service provider access during initialization

5. Check plugin attributes:
   - Verify EMSModuleAttribute is present
   - Check Name, Version, Category properties
   - Validate Dependencies property if used

6. Test plugin loading in isolation:
   - Create minimal host application
   - Attempt to load just the problematic plugin
   - Capture detailed exception information

#### Common Solutions
- Ensure plugin DLL is in correct Plugins/ directory
- Rebuild plugin targeting .NET 10
- Fix missing dependencies (nuget restore)
- Correct EMSModuleAttribute parameters
- Resolve version conflicts between plugin and host
- Fix exceptions in InitializeAsync method
- Ensure plugin doesn't have circular dependencies
- Check file permissions on plugin DLL
- Clear plugin cache if applicable

### 4. Performance Degradation

#### Symptoms
- Increased response times for API requests
- Slow data visualization in UI
- High CPU or memory usage
- Increasing latency in data processing
- Growing message queues

#### Diagnostic Steps
1. Identify which component is degraded:
   - Backend API response times
   - Edge system processing
   - Database query performance
   - Plugin execution time
   - Network latency

2. Check system resource usage:
   ```bash
   # Container stats
   docker stats
   
   # Host stats
   top
   htop
   
   # Specific process
   ps aux | grep dotnet
   ```

3. Monitor database performance:
   ```sql
   -- Check for slow queries
   SELECT query, mean_time, calls 
   FROM pg_stat_statements 
   ORDER BY mean_time DESC 
   LIMIT 10;
   
   -- Check connection usage
   SELECT * FROM pg_stat_activity;
   
   -- Check table sizes
   SELECT schemaname, tablename, pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) as size
   FROM pg_tables 
   WHERE schemaname = 'public'
   ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;
   
   -- Check TimescaleDB compression
   SELECT * FROM timescaledb_information.compressed_chunks;
   ```

4. Analyze application performance:
   - Use dotnet-trace for CPU profiling
   - Use dotnet-counters for real-time metrics
   - Check garbage collection frequency and duration
   - Monitor thread pool usage
   - Check for blocked threads or deadlocks

5. Review logging for performance warnings:
   - Look for slow query warnings
   - Check for allocation warnings
   - Monitor for exceptions causing retries

6. Trace message flow:
   - Check MQTT message rates
   - Monitor gRPC request/response times
   - Track internal message processing times
   - Verify message batching effectiveness

#### Common Solutions
- Optimize slow database queries (add indexes, rewrite queries)
- Enable TimescaleDB compression for older data
- Increase database connection pool size
- Add caching for frequently accessed data
- Optimize serialization/deserialization (consider protobuf or System.Text.Json options)
- Fix memory leaks (dispose objects properly, unsubscribe from events)
- Reduce object allocations in hot paths (use object pooling, StringBuilder)
- Increase processing parallelism where appropriate
- Scale horizontally (add more instances)
- Optimize plugin implementations
- Review and adjust QoS levels for MQTT
- Increase message batch sizes where appropriate
- Clean up old data according to retention policies
- Defragment database indexes periodically

### 5. Security-Related Issues

#### Symptoms
- Authentication failures
- Authorization errors (403 Forbidden)
- TLS/SSL handshake failures
- Certificate validation errors
- Unexpected access to protected resources

#### Diagnostic Steps
1. Check authentication configuration:
   - Verify JWT settings (secret key, issuer, audience)
   - Check local account credentials
   - Validate Keycloak/OIDC configuration if used
   - Review token expiration settings

2. Test authentication endpoints directly:
   ```bash
   # Test token endpoint
   curl -X POST http://localhost:8080/auth/token \
        -H "Content-Type: application/x-www-form-urlencoded" \
        -d "username=admin&password=password&grant_type=password"
   ```

3. Verify authorization policies:
   - Check [Authorize] attributes on controllers/methods
   - Verify policy definitions in Startup.cs
   - Check role assignments for users

4. Examine TLS/SSL configuration:
   - Check certificate validity and chain
   - Verify protocol versions (should be TLS 1.2+)
   - Confirm cipher suites are compatible
   - Check for SNI issues if applicable

5. Review audit logs:
   - Check for failed authentication attempts
   - Look for authorization denials
   - Monitor for unusual access patterns

6. Test with simplified credentials:
   - Try default/admin credentials
   - Test with minimal privilege accounts
   - Verify password complexity requirements

#### Common Solutions
- Correct JWT secret key configuration
- Update expired tokens or refresh token logic
- Fix account lockout or disabled status
- Correct role mappings and policy requirements
- Renew expired TLS certificates
- Fix certificate trust chain issues
- Update protocol configurations to disable weak protocols
- Correct CORS policies if blocking legitimate requests
- Fix API key configuration for edge systems
- Ensure proper password hashing verification

### 6. Deployment Failures

#### Symptoms
- Containers crash immediately after starting
- Services fail to bind to ports
- Configuration errors during startup
- Missing dependencies or files
- Health checks consistently failing

#### Diagnostic Steps
1. Check container logs for startup errors:
   ```bash
   docker-compose logs <service-name>
   # or
   docker logs <container-id>
   ```

2. Verify image was built correctly:
   ```bash
   docker images | grep emscore
   docker run --rm <image> ls -la /app
   ```

3. Check port availability:
   ```bash
   # Check what's using the port
   netstat -tulpn | grep :8080
   ss -tulpn | grep :8080
   
   # Try to bind manually
   nc -lvp 8080
   ```

4. Validate configuration files:
   - Check appsettings.json for valid JSON
   - Verify environment variable substitution
   - Confirm file paths are correct
   - Check for missing configuration sections

5. Test entrypoint command directly:
   ```bash
   docker run --rm <image> dotnet EMSCore.Backend.dll --help
   ```

6. Check filesystem permissions:
   - Verify container user can read/write necessary directories
   - Check database volume permissions
   - Validate log directory accessibility

7. Review resource constraints:
   - Check memory limits aren't too low
   - Verify CPU limits allow reasonable execution
   - Ensure disk space is available

#### Common Solutions
- Fix configuration errors (invalid JSON, missing sections)
- Resolve port conflicts by changing container ports or stopping conflicting services
- Increase resource limits (memory, CPU) in deployment configuration
- Fix volume mount points and permissions
- Correct entrypoint or command in Dockerfile
- Add missing dependencies to Docker image
- Fix runtime errors in application startup code
- Ensure proper user permissions in container (non-root user)
- Check for architecture mismatches (trying to run ARM image on x86)
- Clean up failed container state: `docker system prune -f`

## Advanced Diagnostic Techniques

### Using Diagnostic Tools
- **dotnet-trace**: Collect CPU traces for performance analysis
  ```bash
  dotnet-trace collect --providers Microsoft-DotNETCore-SampleProfiler --process-id <PID>
  ```
- **dotnet-dump**: Capture and analyze memory dumps
  ```bash
  dotnet dump collect --process-id <PID>
  dotnet dump analyze <dump-file>
  ```
- **dotnet-counters**: Monitor real-time performance metrics
  ```bash
  dotnet-counters monitor --process-id <PID>
  ```

### Database Diagnostics
- **Extended Events**: Capture detailed database activity
- **pgBadger**: Analyze PostgreSQL log files for performance insights
- **TimescaleDB Inspector**: Examine hypertable and chunk status

### Network Diagnostics
- **Wireshark**: Deep packet inspection for MQTT/gRPC traffic
- **tcpdump**: Quick network capture for analysis
- **netstat/ss**: Monitor connection states and statistics

### Application Diagnostics
- **Custom Health Checks**: Implement detailed health checks for subsystems
- **Correlation IDs**: Track requests across service boundaries
- **Distributed Tracing**: Use OpenTelemetry for end-to-end tracing
- **Feature Flags**: Toggle problematic features at runtime

## When to Escalate

Consider escalating to senior developers or architects when:
- Issues persist after systematic troubleshooting
- Root cause appears to be in third-party components
- Security vulnerabilities are discovered
- Performance issues require architectural changes
- Data corruption or loss is suspected or confirmed
- Issues affect multiple unrelated systems
- Problems require changes to shared infrastructure
- Licensing or compliance issues are identified

## Documentation and Communication

When resolving issues:
1. Document the problem clearly
2. Record diagnostic steps taken
3. Note the root cause identified
4. Record the solution implemented
5. Document any configuration changes made
6. Share knowledge with team members
7. Consider if preventive measures can be implemented
8. Update troubleshooting guides if appropriate

## Maintenance and Prevention

To reduce future issues:
- Implement comprehensive monitoring and alerting
- Regularly review logs for early warning signs
- Keep dependencies updated
- Perform regular backup and restore drills
- Conduct periodic performance testing
- Review and update security configurations
- Document architecture decisions and rationale
- Implement chaos engineering practices for critical systems
- Regularly review and update runbooks