# Property Management Web Application

A comprehensive solution for managing rental properties, tenants, and operations. Built with modern, open-source technologies for reliability, scalability, and observability.

---

## Tech Stack

- ![dotnet](https://img.shields.io/badge/.NET-8.0-blueviolet?logo=dotnet&logoColor=white) **.NET 8** (C# 12) — Main backend framework, using Razor Pages for UI.
- ![sqlserver](https://img.shields.io/badge/SQL%20Server-2022-CC2927?logo=microsoftsqlserver&logoColor=white) **SQL Server 2022** — Relational database for persistent storage.
- ![serilog](https://img.shields.io/badge/Serilog-structured%20logging-blue?logo=serilog&logoColor=white) **Serilog** — Structured logging for diagnostics and monitoring.
- ![elasticsearch](https://img.shields.io/badge/Elasticsearch-ELK-005571?logo=elasticsearch&logoColor=white) **ELK Stack (Elasticsearch, Kibana)** — Open-source log aggregation and visualization.
- ![docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white) **Docker & Docker Compose** — Containerized deployment for all services.
- ![bootstrap](https://img.shields.io/badge/Bootstrap-5-7952B3?logo=bootstrap&logoColor=white) **Bootstrap 5** — Responsive UI components.
- ![jquery](https://img.shields.io/badge/jQuery-0769AD?logo=jquery&logoColor=white) **jQuery** & ![toastr](https://img.shields.io/badge/Toastr-notifications-ffcc00?logo=javascript&logoColor=black) **Toastr** — Client-side interactivity and notifications.

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