# EMSCore - Energie-Management-System

Ein hochskalierbares Energie-Management-System (EMS) mit Batterieunterstützung und Plugin-basierter Erweiterbarkeit, implementiert in .NET 10 mit TimescaleDB und MQTT. Das Einsatzszenario ist die Freiflächensolaranlage mit Batteriecontainer.

## Achtung WIP 

## 🎯 Projektübersicht

EMSCore ist ein modernes Energie-Management-System, das aus einem zentralen Backend und verteilten Edge-Systemen besteht. Das System bietet:

- **Hybride Kommunikation** über MQTT und gRPC
- **TimescaleDB** für optimierte Zeitreihendaten
- **Entity Framework Core** als ORM
- **Plugin-Architektur** für erweiterbare Module
- **Hybride Sicherheit** mit lokalen Konten und Keycloak/OIDC

## 🏗️ Architektur

Das System basiert auf Clean Architecture mit folgenden Layern:

- **Domain Layer** (`EMSCore.Domain`) - Core Entities, Enums und Interfaces
- **Application Layer** (`EMSCore.Application`) - MediatR Commands/Queries und Handler
- **Infrastructure Layer** (`EMSCore.Infrastructure`) - EF Core, Repositories, MQTT Services
- **Backend System** (`EMSCore.Backend`) - Zentrale Datenverarbeitung, -speicherung und Systemverwaltung
- **Edge System** (`EMSCore.Edge`) - ASP.NET Core Web API für Edge-Deployment

### Technologie-Stack

- **.NET 10** - Application Framework
- **ASP.NET Core** - Web API Framework
- **Entity Framework Core 10** - ORM mit PostgreSQL Provider
- **PostgreSQL 16** - Relationale Datenbank
- **TimescaleDB** - Zeitreihen-Extension
- **MQTTnet** - MQTT Client/Server
- **MediatR** - CQRS Pattern
- **Docker** - Containerisierung
- **Swagger/OpenAPI** - API Dokumentation

## 📁 Repository-Struktur

```
EMSCore/
├── docs/                           # Dokumentation
│   ├── architecture/               # Architektur-Dokumentation
│   ├── api/                       # API-Dokumentation
│   └── deployment/                # Deployment-Guides
├── src/                           # Quellcode
│   ├── EMSCore.Domain/            # Domain Layer
│   ├── EMSCore.Application/       # Application Layer
│   ├── EMSCore.Infrastructure/    # Infrastructure Layer
│   ├── EMSCore.Backend/            # Backend System (Zentrale Datenverarbeitung)
│   ├── EMSCore.Edge/              # Edge System (ASP.NET Core API)
│   └── EMSCore.Plugins/           # Plugin-Framework
├── tests/                         # Tests
├── config/                        # Konfigurationsdateien
├── scripts/                       # Initialisierungsskripte
├── docker-compose.yml             # Docker Compose Setup
└── README.md                      # Diese Datei
```

## 🚀 Quick Start mit Docker

### Voraussetzungen

- Docker und Docker Compose
- .NET 10 SDK (für lokale Entwicklung)
- Git

### 1. Repository klonen

```bash
# Repository klonen
git clone ssh://git@codeberg.org/EMSCore/EMSCore.git
cd EMSCore
```

### 2. System starten

```bash
# Alle Services starten
docker-compose up -d

# Logs verfolgen
docker-compose logs -f emscore-backend
```

### 3. Services überprüfen

- **EMS Core Backend**: http://localhost:8080
- **EMS Core Edge**: http://localhost:8081
- **Swagger UI**: http://localhost:8080 (automatisch geöffnet)
- **Health Check**: http://localhost:8080/health
- **Grafana Dashboard**: http://localhost:3000 (admin/admin)
- **MQTT Broker**: localhost:1883
- **PostgreSQL/TimescaleDB**: localhost:5432

## 📊 API Endpoints

### Energiedaten

```bash
# Aktuelle Messungen für ein Gerät
GET /api/energy/devices/{deviceId}/measurements?startTime=2024-01-01T00:00:00Z&endTime=2024-01-02T00:00:00Z

# Aggregierte Daten (stündlich)
GET /api/energy/devices/{deviceId}/measurements/aggregated?startTime=2024-01-01T00:00:00Z&endTime=2024-01-02T00:00:00Z&intervalMinutes=60

# Neueste Messung
GET /api/energy/devices/{deviceId}/measurements/latest

# Statistiken
GET /api/energy/devices/{deviceId}/measurements/statistics?measurementType=Power&startTime=2024-01-01T00:00:00Z&endTime=2024-01-02T00:00:00Z

# Site-Daten
GET /api/energy/sites/{siteId}/measurements?startTime=2024-01-01T00:00:00Z&endTime=2024-01-02T00:00:00Z
```

### Geräte-Management

```bash
# Alle Geräte
GET /api/devices

# Spezifisches Gerät
GET /api/devices/{deviceId}

# Geräte nach Site
GET /api/devices/site/{siteId}

# Aktive Geräte
GET /api/devices/active

# Online Geräte
GET /api/devices/online

# Neues Gerät erstellen
POST /api/devices
{
  "id": "device-001",
  "name": "Solar Panel 1",
  "type": "Solar Panel",
  "siteId": "site-001",
  "manufacturer": "SolarTech",
  "model": "ST-500W"
}
```

## 🔌 MQTT Integration

### Topic-Struktur

```
ems/{site_id}/devices/{device_id}/measurements/{type}
ems/{site_id}/devices/{device_id}/measurements/batch
ems/{site_id}/devices/{device_id}/status
```

### Beispiel-Nachrichten

#### Einzelne Messung
```bash
# Topic: ems/site-001/devices/solar-panel-001/measurements/power
mosquitto_pub -h localhost -t "ems/docker-site-001/devices/solar-panel-001/measurements/power" -m '{
  "value": 1500.5,
  "unit": "W",
  "timestamp": "2024-01-01T12:00:00Z",
  "quality": 0,
  "metadata": {"phase": "L1"}
}'
```

#### Batch-Messungen
```bash
# Topic: ems/site-001/devices/device-001/measurements/batch
mosquitto_pub -h localhost -t "ems/docker-site-001/devices/battery-001/measurements/batch" -m '{
  "measurements": [
    {"type": "Power", "value": -800, "unit": "W", "timestamp": "2024-01-01T12:00:00Z"},
    {"type": "Voltage", "value": 48.2, "unit": "V", "timestamp": "2024-01-01T12:00:00Z"},
    {"type": "BatterySOC", "value": 75.5, "unit": "%", "timestamp": "2024-01-01T12:00:00Z"}
  ]
}'
```

## 🛠️ Lokale Entwicklung

### Voraussetzungen

- .NET 10 SDK
- PostgreSQL mit TimescaleDB Extension
- MQTT Broker (z.B. Mosquitto)

### Setup

```bash
# Dependencies installieren
dotnet restore

# Datenbank-Connection String anpassen
# src/EMSCore.Backend/appsettings.json

# Backend-Anwendung starten
dotnet run --project src/EMSCore.Backend

# Oder Edge-Anwendung starten
dotnet run --project src/EMSCore.Edge
```

### Tests ausführen

```bash
# Unit Tests
dotnet test

# Integration Tests mit Docker
docker-compose -f docker-compose.test.yml up --abort-on-container-exit
```

## 📖 Dokumentation

- **[Technische Spezifikation](docs/technical-specification.md)** - Detaillierte technische Anforderungen
- **[Architektur-Übersicht](docs/architecture/overview.md)** - System-Architektur und Komponenten
- **[API-Dokumentation](docs/api/)** - REST und gRPC APIs
- **[Deployment-Guide](docs/deployment/)** - Installation und Konfiguration

## 🧪 Testdaten

Das System wird mit Beispieldaten initialisiert:

- **Site**: `docker-site-001` (Docker Development Site)
- **Geräte**: 
  - `solar-panel-001` (Solar Panel Array)
  - `battery-001` (Lithium Battery Bank)
  - `inverter-001` (Grid Tie Inverter)
  - `meter-001` (Smart Energy Meter)
- **Messungen**: 24 Stunden historische Daten mit realistischen Werten

## 🔧 Konfiguration

### Umgebungsvariablen

```bash
# Datenbank
ConnectionStrings__DefaultConnection="Host=localhost;Database=emscore;Username=postgres;Password=postgres"

# MQTT
Mqtt__BrokerHost="localhost"
Mqtt__BrokerPort=1883
Mqtt__Username=""
Mqtt__Password=""

# EMS Spezifisch
EMS__SiteId="site-001"
EMS__SiteName="My Site"
EMS__EdgeMode=true
```

## 🔌 Plugin-System

Das System unterstützt Plugin-basierte Erweiterungen:

```csharp
// Beispiel Plugin Interface
public interface IBatteryModule : IEMSModule
{
    Task<BatteryStatus> GetStatusAsync();
    Task<Result> SetChargingModeAsync(ChargingMode mode);
    IObservable<BatteryEvent> BatteryEvents { get; }
}
```

## 🚀 Deployment

### Docker Production

```bash
# Production Build Backend
docker build -f src/EMSCore.Backend/Dockerfile -t emscore-backend:latest .

# Production Build Edge
docker build -f src/EMSCore.Edge/Dockerfile -t emscore-edge:latest .

# Mit Production Compose
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

## 🤝 Beitragen

1. Fork das Repository
2. Feature Branch erstellen (`git checkout -b feature/amazing-feature`)
3. Änderungen committen (`git commit -m 'Add amazing feature'`)
4. Branch pushen (`git push origin feature/amazing-feature`)
5. Pull Request erstellen

## 📄 Lizenz

Dieses Projekt steht unter der [EUPL 1.2 Lizenz](LICENSE).

## 🤝 Community

- **Issues:** [Codeberg Issues](https://codeberg.org/EMSCore/EMSCore/issues)
- **Diskussionen:** [Codeberg Discussions](https://codeberg.org/EMSCore/EMSCore/discussions)
- **Wiki:** [Projekt Wiki](https://codeberg.org/EMSCore/EMSCore/wiki)

## 🏷️ Version

Aktuelle Version: **0.1.0-alpha** (Prototyp)

Siehe [CHANGELOG.md](CHANGELOG.md) für Details zu Änderungen.

---

**Entwickelt mit ❤️ für nachhaltige Energiesysteme**