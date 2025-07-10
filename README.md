# Property Management - Podman Compose Setup

This project includes a .NET 8 Razor Pages web application and a SQL Server database, both containerized and orchestrated using Podman Compose.

## Prerequisites

- [Podman](https://podman.io/) with Compose support (`podman-compose`)
- `.NET 8` SDK (for local development/builds)
- `aspnetapp.pfx` (HTTPS certificate) and `ca.crt` (CA certificate) files in the project root

## Running the Application

1. **Clone the repository** (if you haven't already):