# Deployment Guide for EMScore

This document provides detailed instructions for deploying the EMScore (Energy Management System) in various environments.

## Docker Deployment

### Development Setup with Docker Compose

The easiest way to get started with EMScore is using Docker Compose for local development.

#### Prerequisites
- Docker Desktop or Docker Engine
- Docker Compose v2+
- Git

#### Steps
1. Clone the repository:
   ```bash
   git clone <repository-url>
   cd EMSCore
   ```

2. Start all services:
   ```bash
   docker-compose up -d
   ```

3. View logs:
   ```bash
   docker-compose logs -f emscore-backend
   ```

4. Access the services:
   - EMS Core Backend: http://localhost:8080
   - EMS Core Edge: http://localhost:8081
   - Swagger UI: http://localhost:8080 (automatically opened)
   - Health Check: http://localhost:8080/health
   - Grafana Dashboard: http://localhost:3000 (admin/admin)
   - MQTT Broker: localhost:1883
   - PostgreSQL/TimescaleDB: localhost:5432

### Production Deployment with Docker

For production environments, use multi-stage Docker builds and production-specific compose files.

#### Building Production Images
```bash
# Production Build Backend
docker build -f src/EMSCore.Backend/Dockerfile -t emscore-backend:latest .

# Production Build Edge
docker build -f src/EMSCore.Edge/Dockerfile -t emscore-edge:latest .
```

#### Production Compose File
Use a separate compose file for production:
```bash
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

#### Dockerfile Example (Multi-stage Build)
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80 443 5000 5001

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["EMSCore.csproj", "."]
RUN dotnet restore "EMSCore.csproj"
COPY . .
RUN dotnet build "EMSCore.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EMSCore.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EMSCore.dll"]
```

## Kubernetes Deployment

For production-scale deployments, Kubernetes provides orchestration and scaling capabilities.

### Prerequisites
- Kubernetes cluster (v1.24+)
- kubectl configured
- Helm v3+ (optional but recommended)

### Basic Kubernetes Deployment

#### Namespace Creation
```bash
kubectl create namespace emscore
```

#### Deployment YAML Example
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: ems-backend
  namespace: emscore
spec:
  replicas: 3
  selector:
    matchLabels:
      app: ems-backend
  template:
    metadata:
      labels:
        app: ems-backend
    spec:
      containers:
      - name: ems-backend
        image: emscore-backend:latest
        ports:
        - containerPort: 80
        - containerPort: 5000
        env:
        - name: Database__ConnectionString
          valueFrom:
            secretKeyRef:
              name: ems-secrets
              key: database-connection
        resources:
          requests:
            memory: "512Mi"
            cpu: "250m"
          limits:
            memory: "1Gi"
            cpu: "500m"
---
apiVersion: v1
kind: Service
metadata:
  name: ems-backend-service
  namespace: emscore
spec:
  selector:
    app: ems-backend
  ports:
    - protocol: TCP
      port: 80
      targetPort: 80
  type: LoadBalancer
```

#### Helm Chart Approach
For more complex deployments, consider using Helm:
```bash
helm repo add emscore https://your-helm-repo.com
helm install emscore emscore/emscore --namespace emscore
```

### Kubernetes Resources
- **Deployments**: For backend and edge services
- **Services**: For internal communication and external exposure
- **ConfigMaps**: For configuration files
- **Secrets**: For sensitive data (database passwords, API keys)
- **PersistentVolumes**: For data persistence
- **Ingress**: For external access (optional)
- **HorizontalPodAutoscaler**: For automatic scaling
- **PodDisruptionBudget**: For high availability

## Environment-Specific Configurations

### Development Environment
- Use docker-compose.yml directly
- Enable developer exceptions and detailed logging
- Use local MQTT broker and database
- Hot reload enabled where possible

### Staging Environment
- Similar to production but with reduced replica counts
- Use staging-specific configuration values
- Enable additional logging and monitoring
- Point to staging external services (if any)

### Production Environment
- Use docker-compose.prod.yml or Kubernetes manifests
- Optimized resource limits and requests
- Minimal logging (error level and above)
- Secure configurations (TLS, secrets management)
- High availability settings
- Monitoring and alerting configured

## Configuration Management

### Environment Variables
EMScore uses environment variables for configuration following the ASP.NET Core convention:

#### Database
```
ConnectionStrings__DefaultConnection="Host=localhost;Database=emscore;Username=postgres;Password=postgres"
```

#### MQTT
```
Mqtt__BrokerHost="localhost"
Mqtt__BrokerPort=1883
Mqtt__Username=""
Mqtt__Password=""
```

#### EMS Specific
```
EMS__SiteId="site-001"
EMS__SiteName="My Site"
EMS__EdgeMode=true
```

### Configuration Files
- appsettings.json: Base configuration
- appsettings.Development.json: Development overrides
- appsettings.Production.json: Production overrides
- appsettings.Staging.json: Staging overrides

### Secrets Management
For production deployments, use proper secrets management:
- Docker/Kubernetes secrets
- HashiCorp Vault
- AWS Secrets Manager
- Azure Key Vault
- Google Secret Manager

## Database Setup

### TimescaleDB Installation
EMScore requires PostgreSQL with the TimescaleDB extension.

#### Manual Installation
1. Install PostgreSQL 16+
2. Install TimescaleDB 2.14+ extension
3. Create database and user:
   ```sql
   CREATE DATABASE emscore;
   CREATE USER ems_user WITH PASSWORD 'secure_password';
   GRANT ALL PRIVILEGES ON DATABASE emscore TO ems_user;
   ```
4. Connect to database and enable extension:
   ```sql
   \c emscore
   CREATE EXCEPTION IF NOT EXISTS timescaledb CASCADE;
   ```

#### Docker-Based TimescaleDB
The docker-compose.yml includes a TimescaleDB service:
```yaml
timescaledb:
  image: timescale/timescaledb:2.14.0-pg16
  restart: unless-stopped
  volumes:
    - timescaledb-data:/var/lib/postgresql/data
  environment:
    POSTGRES_USER: postgres
    POSTGRES_PASSWORD: postgres
    POSTGRES_DB: emscore
  ports:
    - "5432:5432"
```

### Database Migrations
EMScore uses Entity Framework Core migrations for database schema management.

#### Applying Migrations
```bash
# For backend
dotnet ef database update --project src/EMSCore.Backend --startup-project src/EMSCore.Backend

# For edge (if applicable)
dotnet ef database update --project src/EMSCore.Edge --startup-project src/EMSCore.Edge
```

#### Creating Migrations
```bash
dotnet ef migrations add InitialCreate --project src/EMSCore.Infrastructure --startup-project src/EMSCore.Backend
```

## Monitoring and Logging

### Logging Configuration
EMScore uses Serilog for structured logging.

#### Configuration Example (appsettings.json)
```json
{
  "Serilog": {
    "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.File" ],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "Logs/log-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30
        }
      }
    ],
    "Enrich": [ "FromLogContext", "WithMachineName", "WithThreadId" ],
    "Properties": {
      "Application": "EMSCore"
    }
  }
}
```

### Monitoring with Prometheus and Grafana
The docker-compose.yml includes pre-configured Prometheus and Grafana services.

#### Accessing Grafana
- URL: http://localhost:3000
- Default credentials: admin/admin
- Pre-configured dashboards for EMScore metrics

#### Key Metrics Exposed
- EMScore application metrics (custom)
- .NET runtime metrics
- HTTP request rates and durations
- Database connection pool usage
- Message queue depths
- Cache hit/miss ratios

### Health Checks
EMScore includes health check endpoints:
- Backend: http://localhost:8080/health
- Edge: http://localhost:8081/health

Health checks include:
- Database connectivity
- MQTT broker connectivity
- gRPC service availability
- Plugin loading status
- Memory and CPU usage

## Backup and Disaster Recovery

### Database Backup
Regular backups of the TimescaleDB database are essential.

#### Manual Backup
```bash
pg_dump -U postgres -h localhost -Fc emscore > emscore_backup.dump
```

#### Automated Backup (Cron Job)
```bash
# Daily backup at 2 AM
0 2 * * * pg_dump -U postgres -h localhost -Fc emscore > /backups/emscore_$(date +\%Y\%m\%d).dump
```

#### Point-in-Time Recovery
TimescaleDB supports point-in-time recovery when combined with WAL archiving.

### Configuration Backup
Backup configuration files and environment-specific settings:
- docker-compose.yml and override files
- Kubernetes manifests
- Environment variable files
- SSL certificates and keys

## Scaling Strategies

### Horizontal Scaling
- Scale backend services based on CPU/memory usage or request rate
- Scale edge systems based on number of connected devices
- Use load balancers to distribute traffic
- Database read replicas for scaling read operations

### Vertical Scaling
- Increase container/resource limits for compute-intensive operations
- Optimize database queries and indexing
- Consider partitioning strategies for large datasets

### Database Scaling
- TimescaleDB automatic partitioning
- Read replicas for query distribution
- Multi-node TimescaleDB deployments for high availability
- Compression policies for older data

## Troubleshooting Deployment Issues

### Common Problems and Solutions

#### Container Fails to Start
1. Check container logs: `docker-compose logs <service-name>`
2. Verify environment variables are set correctly
3. Check port conflicts with other services
4. Validate image was built correctly

#### Database Connection Failures
1. Verify PostgreSQL/TimescaleDB is running
2. Check connection string in appsettings.json or environment variables
3. Ensure network connectivity between services
4. Verify database user has correct permissions

#### MQTT Connection Issues
1. Verify MQTT broker is running and accessible
2. Check broker hostname and port configuration
3. Validate authentication credentials (if used)
4. Check firewall rules and network policies
5. Verify client ID uniqueness

#### High Resource Usage
1. Identify which service is consuming resources
2. Check for memory leaks in application code
3. Review database query performance
4. Consider scaling horizontally or vertically
5. Review caching strategies

#### Plugin Loading Failures
1. Verify plugin DLLs are in the correct location
2. Check .NET version compatibility
3. Review plugin initialization logs
4. Ensure plugin dependencies are available
5. Check for exceptions during plugin initialization

## Security Considerations for Deployment

### Network Security
- Use firewalls to restrict access to services
- Implement network segmentation (dev/stage/prod)
- Use private networks for inter-service communication
- Consider service mesh for advanced traffic management

### Transport Security
- Enable TLS 1.3 for all service-to-service communication
- Use HTTPS for external endpoints
- Secure MQTT with TLS/TLS-PSK
- Use mutual TLS (mTLS) for service-to-service authentication

### Secrets Management
- Never hardcode secrets in configuration files or Dockerfiles
- Use environment variables or secret management systems
- Rotate secrets regularly
- Audit access to secrets

### Image Security
- Use minimal base images (distroless when possible)
- Scan images for vulnerabilities regularly
- Use trusted base images from official repositories
- Implement image signing and verification

### Runtime Security
- Run containers as non-root users
- Implement read-only root filesystems where possible
- Drop unnecessary Linux capabilities
- Use security contexts and pod security policies (Kubernetes)
- Implement runtime security monitoring

## Performance Optimization Tips

### Container Optimization
- Use multi-stage builds to minimize image size
- Leverage Docker layer caching
- Use .NET trimming and linking for smaller deployments
- Enable ready-to-run (R2R) compilation for faster startup

### Database Optimization
- Create appropriate indexes for query patterns
- Use TimescaleDB hypertables and compression
- Configure connection pooling correctly
- Monitor and optimize slow queries
- Consider partitioning strategies for large datasets

### Application Optimization
- Use asynchronous programming throughout
- Implement caching for frequently accessed data
- Optimize serialization/deserialization
- Use object pooling for high-frequency object creation
- Minimize allocations in hot paths

### Network Optimization
- Use HTTP/2 for gRPC communication
- Enable keep-alive connections
- Optimize message batching for MQTT
- Consider message compression where appropriate
- Use content delivery networks (CDNs) for static assets

## Validation and Testing Deployments

### Smoke Testing
After deployment, perform basic validation:
1. Verify all services are running
2. Check health check endpoints return healthy status
3. Validate API endpoints respond correctly
4. Test basic MQTT publish/subscribe
5. Verify database connectivity

### Integration Testing
Run automated tests against the deployed environment:
```bash
# Run tests pointing to deployed services
dotnet test --filter Category=Integration
```

### Performance Testing
Validate performance under load:
- Use tools like k6, JMeter, or Locust for load testing
- Test MQTT throughput and latency
- Test gRPC request/response performance
- Test API endpoint response times
- Monitor resource usage during tests

### Chaos Engineering
For production deployments, consider implementing chaos engineering:
- Network latency injection
- Pod/container termination
- Database connection failures
- Resource exhaustion simulation