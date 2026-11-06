# CessnaCrew

Webapplikation zur gemeinsamen Verwaltung einer Segelyacht (Modul 223). Die Projektdokumentation liegt in [docs/](docs/).

## Aufbau

| Ordner | Inhalt |
|---|---|
| `CessnaCrew.WebApi/` | Backend (ASP.NET Core, .NET 10) |
| `CessnaCrew.Web.App/ClientApp/` | Frontend (Vite, React + TypeScript) |
| `.devcontainer/` | Entwicklungsumgebung mit Docker Compose |

## Entwicklungsumgebung

Das Projekt im Dev Container öffnen (VS Code: «Reopen in Container»). Docker Compose startet folgende Container:

| Container | Aufgabe | Adresse |
|---|---|---|
| `app` | .NET-SDK, hier wird das Backend gestartet | http://localhost:5136 |
| `frontend` | Node, startet automatisch den Vite-Dev-Server | http://localhost:5173 |
| `db` | PostgreSQL 18 | Host `db`, Port 5432 |
| `pgadmin` | Datenbankadministration | http://localhost:5050 |

Backend im `app`-Container starten:

```bash
dotnet run --project CessnaCrew.WebApi
```

Der Vite-Dev-Server leitet Anfragen an `/api` an das Backend weiter (siehe `vite.config.ts`).

# Datenbank
Die Daten werden in einer PostgreSQL-Datenbank gespeichert. In der Entwicklungsumgebung wird diese in der docker-compose.yml konfiguriert.

## Datenbankadministration
Über http://localhost:5050 ist PgAdmin4 erreichbar. Damit lässt sich die Datenbank komfortabel verwalten. Der Benutzername lautet zli@example.com und das Passwort zli*123. Die Verbindung zur PostgreSQL-Datenbank muss zuerst mit folgenden Daten konfiguriert werden:

* Host name/address: db
* Port: 5432
* Maintenance database: postgres
* Username: postgres
* Password: postgres
