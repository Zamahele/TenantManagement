# Property Management Web Application

A comprehensive solution for managing rental properties, tenants, and operations. Built with modern, open-source technologies for reliability, scalability, and observability.

---

## Tech Stack

- **.NET 8** (C# 12) — Main backend framework, using Razor Pages for UI.
- **SQL Server 2022** — Relational database for persistent storage.
- **Serilog** — Structured logging for diagnostics and monitoring.
- **ELK Stack (Elasticsearch, Kibana)** — Open-source log aggregation and visualization.
- **Docker & Docker Compose** — Containerized deployment for all services.
- **Bootstrap 5** — Responsive UI components.
- **jQuery & Toastr** — Client-side interactivity and notifications.

---

## Architecture Overview

- **Web App**: ASP.NET Core Razor Pages, runs in a container.
- **Database**: SQL Server, runs in a container.
- **Logging**: Serilog writes logs to both console and Elasticsearch.
- **Log Visualization**: Kibana (with Elasticsearch) for searching and visualizing logs/errors.
- **All services are orchestrated via Docker Compose.**

---

## Running the Stack Locally

1. **Clone the repository** (if you haven't already):