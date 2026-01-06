# EMSCore - Energie-Management-System

Ein hochskalierbares Energie-Management-System (EMS) mit Batterieunterstützung und erweiterbaren Modulen, entwickelt in .NET 10.

## 🎯 Projektübersicht

EMSCore ist ein modernes Energie-Management-System, das aus einem zentralen Backend und verteilten Edge-Systemen besteht. Das System bietet:

- **Hybride Kommunikation** über MQTT und gRPC
- **TimescaleDB** für optimierte Zeitreihendaten
- **Entity Framework Core** als ORM
- **Plugin-Architektur** für erweiterbare Module
- **Hybride Sicherheit** mit lokalen Konten und Keycloak/OIDC

## 🏗️ Architektur

Das System besteht aus zwei Hauptkomponenten:

1. **Backend System:** Zentrale Datenverarbeitung, -speicherung und Systemverwaltung
2. **Edge Systems:** Dezentrale Datenerfassung und lokale Verarbeitung mit Offline-Fähigkeiten

### Technologie-Stack

- **Framework:** .NET 10 mit ASP.NET Core
- **ORM:** Entity Framework Core 10 mit PostgreSQL Provider
- **Datenbank:** PostgreSQL 16+ mit TimescaleDB 2.14+ Extension
- **Kommunikation:** MQTT 5.0 + gRPC mit HTTP/2
- **Service Bus:** MediatR + System.Threading.Channels + Reactive Extensions
- **Authentifizierung:** Hybride Architektur (lokale Konten + Keycloak/OIDC)

## 📁 Repository-Struktur

```
EMSCore/
├── docs/                           # Dokumentation
│   ├── architecture/               # Architektur-Dokumentation
│   ├── api/                       # API-Dokumentation
│   └── deployment/                # Deployment-Guides
├── src/                           # Quellcode
│   ├── EMSCore.Backend/           # Backend-Services
│   ├── EMSCore.Edge/              # Edge-Services
│   ├── EMSCore.Shared/            # Geteilte Bibliotheken
│   └── EMSCore.Plugins/           # Plugin-Framework
├── tests/                         # Tests
├── docker/                        # Docker-Konfigurationen
├── k8s/                          # Kubernetes-Manifeste
└── scripts/                      # Build- und Deployment-Skripte
```

## 🚀 Quick Start

### Voraussetzungen

- .NET 10 SDK
- PostgreSQL 16+ mit TimescaleDB
- Docker (optional)

### Installation

```bash
# Repository klonen
git clone ssh://git@codeberg.org/EMSCore/EMSCore.git
cd EMSCore

# Dependencies installieren
dotnet restore

# Datenbank migrieren
dotnet ef database update --project src/EMSCore.Backend

# Backend starten
dotnet run --project src/EMSCore.Backend
```

## 📖 Dokumentation

- **[Technische Spezifikation](docs/technical-specification.md)** - Detaillierte technische Anforderungen
- **[Architektur-Übersicht](docs/architecture/overview.md)** - System-Architektur und Komponenten
- **[API-Dokumentation](docs/api/)** - REST und gRPC APIs
- **[Deployment-Guide](docs/deployment/)** - Installation und Konfiguration

## 🔧 Entwicklung

### Plugin-Entwicklung

```csharp
[EMSModule("MyPlugin", "1.0.0", "Energy")]
public class MyEnergyPlugin : IEMSModule
{
    public async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        // Plugin-Initialisierung
    }
}
```

### Beitragen

1. Fork das Repository
2. Erstelle einen Feature-Branch (`git checkout -b feature/amazing-feature`)
3. Committe deine Änderungen (`git commit -m 'Add amazing feature'`)
4. Push zum Branch (`git push origin feature/amazing-feature`)
5. Öffne eine Pull Request

## 📄 Lizenz

Dieses Projekt steht unter der [MIT Lizenz](LICENSE).

## 🤝 Community

- **Issues:** [Codeberg Issues](https://codeberg.org/EMSCore/EMSCore/issues)
- **Diskussionen:** [Codeberg Discussions](https://codeberg.org/EMSCore/EMSCore/discussions)
- **Wiki:** [Projekt Wiki](https://codeberg.org/EMSCore/EMSCore/wiki)

## 🏷️ Version

Aktuelle Version: **0.1.0-alpha**

Siehe [CHANGELOG.md](CHANGELOG.md) für Details zu Änderungen.