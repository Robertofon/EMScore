# EMScore - Agent Interaction Guidelines

This document provides guidelines for AI agents interacting with the EMScore (Energy Management System) codebase.

## Project Overview

EMScore is a high-scalable Energy Management System (EMS) with battery support and plugin-based extensibility, implemented in .NET 10 with TimescaleDB and MQTT. The system consists of a central backend and distributed edge systems.

## Repository Structure

```
EMSCore/
├── docs/                           # Documentation
│   ├── architecture/               # Architecture documentation
│   ├── api/                       # API documentation
│   ├── references/                # Modular reference guides
│   └── deployment/                # Deployment guides
├── src/                           # Source code
│   ├── EMSCore.Domain/            # Domain Layer (Entities, Enums, Interfaces)
│   ├── EMSCore.Application/       # Application Layer (MediatR Commands/Queries and Handlers)
│   ├── EMSCore.Infrastructure/    # Infrastructure Layer (EF Core, Repositories, MQTT Services)
│   ├── EMSCore.Backend/           # Backend System (Central data processing)
│   ├── EMSCore.Edge/              # Edge System (ASP.NET Core API)
│   └── EMSCore.Plugins/           # Plugin Framework
├── tests/                         # Tests
├── config/                        # Configuration files
├── scripts/                       # Initialization scripts
├── docker-compose.yml             # Docker Compose setup
├── .opencode/                     # Agent skills and configurations
└── README.md                      # Project overview
```

## Technology Stack

- **Framework**: .NET 10 with ASP.NET Core
- **ORM**: Entity Framework Core 10 with PostgreSQL provider
- **Database**: PostgreSQL 16+ with TimescaleDB 2.14+ extension
- **Communication**: 
  - MQTT 5.0 for telemetry data
  - gRPC with HTTP/2 for commands and configuration
- **Service Bus**: MediatR + System.Threading.Channels + Reactive Extensions (Rx.NET)
- **Authentication**: Hybrid architecture (local accounts + Keycloak/OIDC)
- **Containerization**: Docker with multi-stage builds
- **Monitoring**: OpenTelemetry + Prometheus + Grafana
- **Logging**: Serilog with structured logging

## Architecture Patterns

### Clean Architecture Layers

1. **Domain Layer** (`EMSCore.Domain`)
   - Core entities, enums, and interfaces
   - Business logic independent of external concerns

2. **Application Layer** (`EMSCore.Application`)
   - MediatR commands/queries and handlers
   - Application-specific business rules

3. **Infrastructure Layer** (`EMSCore.Infrastructure`)
   - EF Core implementation
   - Repository pattern implementations
   - External service integrations (MQTT, etc.)

4. **Presentation Layers**
   - `EMSCore.Backend`: Central data processing and system management
   - `EMSCore.Edge`: ASP.NET Core Web API for edge deployment

5. **Plugin System** (`EMSCore.Plugins`)
   - Extensible module framework with hot-swapping capabilities

### Key Interfaces and Patterns

- **Repository Pattern**: Abstract data access (`IEnergyMeasurementRepository`, `IDeviceRepository`, etc.)
- **Service Pattern**: Business logic services (`IMqttService`, etc.)
- **Plugin Interface**: `IEMSModule` with specialized interfaces (`IBatteryModule`, `ISensorModule`, `IAnalyticsModule`)
- **Event-Driven**: Using Reactive Extensions and Channels for async processing
- **CQRS**: MediatR for separating read and write operations

## Development Setup

### Prerequisites
- .NET 10 SDK
- PostgreSQL with TimescaleDB extension
- MQTT broker (e.g., Mosquitto)
- Docker and Docker Compose (for full system setup)

### Local Development
```bash
# Clone repository
git clone <repository-url>
cd EMSCore

# Restore dependencies
dotnet restore

# Configure database connection in src/EMSCore.Backend/appsettings.json

# Run backend
dotnet run --project src/EMSCore.Backend

# Or run edge system
dotnet run --project src/EMSCore.Edge
```

### Docker Development
```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f emscore-backend
```

## Testing

### Unit Tests
```bash
dotnet test
```

### Integration Tests
```bash
docker-compose -f docker-compose.test.yml up --abort-on-container-exit
```

## Coding Conventions

For detailed coding conventions and style guidelines, refer to the [Coding Style Guide](docs/references/CODING_STYLE.md).

## Common Tasks

### Adding a New Entity
1. Create entity class in `EMSCore.Domain/Entities/`
2. Add DbSet to `EMSDbContext` in `EMSCore.Infrastructure/`
3. Configure relationships in `OnModelCreating` if needed
4. Add repository interface in `EMSCore.Domain/Interfaces/`
5. Implement repository in `EMSCore.Infrastructure/`
6. Add related commands/queries in `EMSCore.Application/`
7. Implement handlers in `EMSCore.Application/Handlers/`

### Adding a New Plugin Module
1. Create interface in `EMSCore.Domain/Interfaces/` (if specialized)
2. Implement module class in `EMSCore.Plugins/` inheriting from `PluginBase`
3. Apply `EMSModuleAttribute` with metadata
4. Register module in plugin configuration
5. Implement required methods: `InitializeAsync`, `StartAsync`, `StopAsync`, `GetHealthAsync`

### Working with MQTT
1. Use `IMqttService` for publishing/subscribing
2. Follow topic structure: `ems/{site_id}/devices/{device_id}/measurements/{type}`
3. Handle connection lifecycle properly
4. Use appropriate QoS levels for message reliability

### Working with gRPC
1. Define services in `.proto` files
2. Implement service classes inheriting from generated base
3. Register services in DI container
4. Use for command/response patterns requiring reliability

## Plugin System Guidelines

### Module Lifecycle
1. `InitializeAsync`: One-time setup with service provider access
2. `StartAsync`: Begin module operations
3. `StopAsync`: Graceful shutdown
4. `GetHealthAsync`: Report module health status

### Best Practices
- Keep modules loosely coupled
- Use dependency injection for services
- Handle cancellation tokens properly
- Implement proper error handling and logging
- Respect module dependencies declared in attributes
- Use semantic versioning for modules

## Database Guidelines

### TimescaleDB Usage
- Use hypertables for time-series data (`EnergyMeasurement` table)
- Leverage time_bucket functions for aggregations
- Configure appropriate retention policies
- Use compression for older data

### EF Core Patterns
- Use repository pattern for data access
- Leverage navigation properties for related data
- Configure indexes for query performance
- Use async methods for database operations
- Handle transactions properly when needed

## Security Considerations

### Authentication
- Use hybrid auth (local + Keycloak/OIDC)
- Implement proper JWT handling
- Secure API endpoints with [Authorize] attributes
- Use role-based authorization (SystemAdmin, SiteManager, Operator, etc.)

### Data Protection
- Encrypt sensitive configuration data
- Use TLS 1.3 for all communications
- Implement proper certificate validation for device authentication
- Hash passwords using industry-standard algorithms

## Performance Guidelines

### Latency Targets
- MQTT messages: < 100ms end-to-end
- gRPC commands: < 500ms response time
- Database queries: < 1s for complex aggregations
- Plugin initialization: < 30s per module

### Throughput Targets
- Sensor data: 10,000 measurement points/second per edge system
- Concurrent connections: 1,000 MQTT clients per broker
- API requests: 1,000 requests/second
- Database writes: 50,000 inserts/second

### Optimization Tips
- Use async/await throughout I/O-bound operations
- Implement proper caching strategies
- Use database indexing effectively
- Minimize object allocations in hot paths
- Use object pooling where appropriate
- Batch database operations when possible

## Troubleshooting

For detailed troubleshooting procedures and diagnostic guidance, refer to the [Troubleshooting Skill](.opencode/skill/troubleshooting/skill.md).

## Deployment

For detailed deployment instructions and environment-specific configurations, refer to the [Deployment Guide](docs/references/DEPLOYMENT_GUIDE.md).

## Resources

- [Technical Specification](docs/technical-specification.md) - Detailed technical requirements
- [Architecture Overview](docs/architecture/overview.md) - System architecture and components
- [API Documentation](docs/api/) - REST and gRPC API references
- [Coding Style Guide](docs/references/CODING_STYLE.md) - Coding conventions and style guidelines
- [Deployment Guide](docs/references/DEPLOYMENT_GUIDE.md) - Deployment instructions and environment-specific configurations
- [README.md] - Project overview and quick start guide

## Skills

- [Tools Skill](.opencode/skill/tools/skill.md) - Guidance on using development tools effectively
- [Troubleshooting Skill](.opencode/skill/troubleshooting/skill.md) - Systematic approach to diagnosing and resolving common issues

## Getting Help

When working with the EMScore codebase:
1. Consult the documentation in the `docs/` directory first
2. Examine existing code for patterns and conventions
3. Look at unit tests for usage examples
4. Check git history for recent changes and rationale
5. When in doubt, ask for clarification before making assumptions