# Tools Skill for EMScore Development

This skill provides guidance on using development tools effectively when working with the EMScore codebase.

## IDE/Editor Configuration

### Visual Studio Code Recommendations
- Install C# extension by Microsoft
- Install .NET Install Tool
- Install Docker extension
- Install Kubernetes extension
- Install PostgreSQL extension
- Install MQTT Explorer (for testing)
- Install GitLens for enhanced Git capabilities

#### Recommended Settings
```json
{
  "editor.formatOnSave": true,
  "editor.codeActionsOnSave": {
    "source.organizeImports": true
  },
  "csharp.format.enable": true,
  "dotnet.testExtraArgs": ["--logger:trx"],
  "files.exclude": {
    "**/bin": true,
    "**/obj": true
  },
  "omnisharp.useGlobalMono": "never"
}
```

### Rider/Visual Studio Recommendations
- Install ReSharper (for Visual Studio)
- Install Docker support
- Install Kubernetes tools
- Install .NET profiling tools

## Debugging Techniques

### Backend Debugging
- Set breakpoints in Controllers, Services, and Handlers
- Use conditional breakpoints for specific device IDs or measurement types
- Utilize DataTips to inspect complex objects like EnergyMeasurement
- Use the Diagnostic Tools window for performance monitoring
- Attach to running Docker containers: `dotnet run --project src/EMSCore.Backend`

### Edge System Debugging
- Debug Web API controllers and middleware
- Test gRPC calls using BloomRPC or similar tools
- Debug MQTT message handling with breakpoints in handlers
- Use Emulator for hardware simulation when testing drivers

### Plugin Debugging
- Set breakpoints in InitializeAsync, StartAsync, and StopAsync methods
- Debug plugin loading by setting breakpoints in PluginManager
- Use module-specific logging to trace execution flow

## Performance Profiling Tools

### .NET Profiling
- Use dotnet-trace for CPU profiling:
  ```bash
  dotnet-trace collect --providers Microsoft-DotNETCore-SampleProfiler --process-id <PID>
  ```
- Use dotnet-counters for real-time metrics:
  ```bash
  dotnet-counters monitor --process-id <PID>
  ```
- Use dotnet-dump for memory analysis:
  ```bash
  dotnet dump collect --process-id <PID>
  ```

### Database Profiling
- Use pg_stat_statements extension for query analysis
- Use EXPLAIN ANALYZE for query execution plans
- Monitor TimescaleDB-specific metrics:
  ```sql
  SELECT * FROM timescaledb_information.hypertable_dimensions;
  SELECT * FROM timescaledb_information.chunks WHERE hypertable_name = 'energy_measurements';
  ```

### Network Profiling
- Use Wireshark to analyze MQTT and gRPC traffic
- Use netstat or ss to monitor connection states
- Use iptraf-ng for real-time network statistics

## Database Administration Tools

### PostgreSQL/TimescaleDB
- Use pgAdmin for graphical database administration
- Use psql command-line tool for direct database access
- Use TimescaleDB-specific functions:
  ```sql
  -- Check compression status
  SELECT * FROM timescaledb_information.compressed_chunks;
  
  -- Check hypertable details
  SELECT * FROM timescaledb_information.hypertables;
  
  -- Check continuous aggregates
  SELECT * FROM timescaledb_information.continuous_aggregates;
  ```

### Migration Management
- Use Entity Framework Core CLI tools:
  ```bash
  # Create migration
  dotnet ef migrations add <Name> --project src/EMSCore.Infrastructure --startup-project src/EMSCore.Backend
  
  # Apply migrations
  dotnet ef database update --project src/EMSCore.Backend --startup-project src/EMSCore.Backend
  
  # Remove last migration
  dotnet ef migrations remove --project src/EMSCore.Infrastructure --startup-project src/EMSCore.Backend
  
  # List migrations
  dotnet ef migrations list --project src/EMSCore.Infrastructure --startup-project src/EMSCore.Backend
  ```

## API Testing Tools

### Swagger/OpenAPI
- Access Swagger UI at http://localhost:8080/swagger
- Test endpoints directly from the browser
- Download OpenAPI specification for import into other tools
- Use try-it-out feature for manual testing

### Postman/Newman
- Use predefined collections for EMScore API testing
- Create environment variables for different deployments
- Use pre-request scripts for authentication
- Run automated tests with Newman in CI/CD pipelines
- Test both REST and gRPC endpoints (with appropriate plugins)

### gRPC Testing Tools
- Use BloomRPC for graphical gRPC testing
- Use grpcurl for command-line gRPC testing:
  ```bash
  # List services
  grpcurl -plaintext localhost:50051 list
  
  # Call a method
  grpcurl -plaintext -d '{"deviceId": "solar-panel-001"}' localhost:50051 ems.energy.EnergyService/GetLatestMeasurement
  ```

## Container Management Tools

### Docker
- Use Docker Desktop for container visualization
- Use docker-compose commands for multi-container management:
  ```bash
  # View running containers
  docker ps
  
  # View logs
  docker-compose logs -f <service>
  
  # Enter container shell
  docker exec -it <container> /bin/bash
  
  # Check resource usage
  docker stats
  
  # Rebuild and restart
  docker-compose up -d --build
  ```

### Kubernetes
- Use kubectl for cluster management:
  ```bash
  # Get pods
  kubectl get pods -n emscore
  
  # Describe pod
  kubectl describe pod <pod-name> -n emscore
  
  # Get logs
  kubectl logs <pod-name> -n emscore
  
  # Exec into pod
  kubectl exec -it <pod-name> -n emscore -- /bin/bash
  
  # Port forwarding for testing
  kubectl port-forward service/ems-backend 8080:80 -n emscore
  
  # Deploy from manifest
  kubectl apply -f <manifest.yaml> -n emscore
  
  # Check resource usage
  kubectl top pods -n emscore
  ```

## Version Control Best Practices

### Git Workflow
- Use feature branching: `git checkout -b feature/<ticket-number>-<description>`
- Commit frequently with meaningful messages
- Use conventional commit format:
  ```
  <type>(<scope>): <subject>
  
  <body>
  
  <footer>
  ```
- Types: feat, fix, docs, style, refactor, perf, test, chore
- Scope: Domain, Application, Infrastructure, Backend, Edge, Plugins
- Example: `feat(backend): add battery charging optimization`

### Pull Request Process
- Keep PRs focused on single feature/bug fix
- Include tests for new functionality
- Update documentation when needed
- Request reviews from appropriate team members
- Address all review comments before merging
- Use squash merge for clean history

### Repository Maintenance
- Regularly fetch and prune remote branches: `git fetch --prune`
- Clean up local branches: `git branch --merged main | grep -v "\\* main" | xargs -n 1 git branch -d`
- Use git rebase to keep feature branches up to date
- Tag releases appropriately: `git tag -a v1.0.0 -m "Release version 1.0.0"`

## Testing Frameworks and Execution

### Unit Testing
- Use xUnit as the primary testing framework
- Use Moq for mocking dependencies
- Use FluentAssertions for readable assertions
- Follow AAA pattern: Arrange, Act, Assert
- Test public interfaces, not private implementation details
- Aim for high coverage on business logic

#### Running Tests
```bash
# Run all tests
dotnet test

# Run tests in specific project
dotnet test tests/EMSCore.Tests

# Run tests with specific filter
dotnet test --filter Category=Unit

# Run tests with detailed output
dotnet test --logger:"console;verbosity=detailed"

# Run tests and generate coverage report
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov
```

### Integration Testing
- Use Docker Compose for test environment orchestration
- Use Testcontainers for disposable dependencies
- Test against real services when possible
- Clean up test data after each test run
- Use environment-specific configuration for tests

#### Running Integration Tests
```bash
# Run integration tests
docker-compose -f docker-compose.test.yml up --abort-on-container-exit

# Run specific integration tests
dotnet test --filter Category=Integration
```

### Performance Testing
- Use BenchmarkDotNet for microbenchmarking
- Use k6 or JMeter for load testing
- Test under realistic load conditions
- Measure and track performance over time
- Identify and address bottlenecks

## Code Quality and Analysis Tools

### Static Analysis
- Use Roslyn analyzers for code quality
- Configure .editorconfig for consistent formatting
- Use StyleCop analyzers for style enforcement
- Treat warnings as errors in builds
- Use SonarQube or similar for code quality metrics

### Formatting Tools
- Use dotnet format for code formatting:
  ```bash
  # Format entire solution
  dotnet format
  
  # Format specific project
  dotnet format src/EMSCore.Backend
  
  # Check formatting without applying
  dotnet format --verify-no-changes
  ```

### Dependency Management
- Use dotnet list package to see package references
- Use dotnet outdated to check for updates
- Use NuGet.config for package source configuration
- Consider using InternalDotSet for visible internals testing
- Regularly update packages for security patches

## Documentation Tools

### API Documentation
- Use Swagger/OpenAPI for REST API documentation
- Use Protobuf definitions for gRPC service documentation
- Keep API documentation up to date with code changes
- Include examples for common operations
- Document error responses and status codes

### Technical Documentation
- Use Markdown for all documentation
- Keep documentation in docs/ directory
- Use diagrams (Mermaid, PlantUML) for complex concepts
- Include code samples where helpful
- Link to related documentation
- Maintain version-specific documentation

## Troubleshooting Tools

### Logging Analysis
- Use structured logging with Serilog
- Correlate logs using correlation IDs
- Use Elastic Stack (ELK) for log aggregation in production
- Use Seq for local log aggregation and analysis
- Write logs to file and console in development

### Diagnostic Commands
- Use dotnet tool commands for runtime diagnostics:
  ```bash
  # List running .NET processes
  dotnet-trace ps
  
  # Collect a trace
  dotnet-trace collect --process-id <PID> --duration 30s
  
  # Analyze a dump
  dotnet-dump analyze <dump-file>
  ```