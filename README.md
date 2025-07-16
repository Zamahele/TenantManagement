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

## Core Functionality

### 1. Maintenance Request Management
- Tenants can submit maintenance requests for their rooms.
- Property managers can assign, track, and update the status of maintenance tasks.
- Maintenance history is logged per room.

### 2. Lease Agreement Management
- Store and manage digital copies of lease agreements.
- Track lease start and end dates, and notify tenants and managers of upcoming expirations.

### 3. Payment Tracking and Receipts
- Record all rent and deposit payments.
- Generate and send payment receipts to tenants.
- Track outstanding balances and payment history.

### 4. Automated Notifications
- Send reminders to tenants for upcoming rent due dates.
- Notify property managers of overdue rents or expiring leases.
- Alert maintenance staff of new or urgent requests.

### 5. Room Availability and Booking
- Track which rooms are occupied, vacant, or under maintenance.
- Allow prospective tenants to view available rooms and submit booking requests.

### 6. Tenant Profile Management
- Maintain detailed profiles for each tenant (contact info, emergency contacts, rental history).
- Allow tenants to update their own information.

### 7. Inspection Scheduling and Records
- Schedule regular room inspections.
- Record inspection results and follow-up actions.

### 8. Utility Billing and Tracking
- Track utility usage (water, electricity) per room or tenant.
- Generate and send utility bills.

### 9. Document Management
- Store important documents (insurance, compliance certificates) related to the property.

### 10. Reporting and Analytics
- Generate reports on occupancy rates, rent collection, maintenance costs, etc.
- Visual dashboards for property performance.

---

## Running the Stack Locally

1. **Clone the repository**

## Project Structure

PropertyManagement (Solution)
│
├── PropertyManagement.Domain
│   └── Entities
│       ├── Tenant.cs
│       ├── Room.cs
│       ├── LeaseAgreement.cs
│       ├── Payment.cs
│       ├── User.cs
│       └── ... (other domain entities)
│
├── PropertyManagement.Infrastructure
│   ├── Data
│   │   └── ApplicationDbContext.cs
│   └── Repositories
│       ├── IGenericRepository.cs
│       └── GenericRepository.cs
│
└── PropertyManagement.Web
    ├── Controllers
    │   └── TenantsController.cs
    ├── ViewModels
    │   ├── TenantViewModel.cs
    │   ├── RoomViewModel.cs
    │   └── ... (other view models)
    ├── Pages
    │   └── ... (Razor Pages and partials)
    └── Views
        └── ... (if using MVC views or shared partials)