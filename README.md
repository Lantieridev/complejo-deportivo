# 🏟️ Complejo Deportivo | Sports Facility Booking API

![C#](https://img.shields.io/badge/C%23-.NET_8-purple?style=for-the-badge&logo=csharp)
![EF Core](https://img.shields.io/badge/EF_Core-8.0-blue?style=for-the-badge)
![JWT](https://img.shields.io/badge/Auth-JWT-black?style=for-the-badge&logo=jsonwebtokens)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)
![CI](https://img.shields.io/github/actions/workflow/status/Lantieridev/complejo-deportivo/ci.yml?branch=main&style=for-the-badge&label=CI)

A booking system for sports facility complexes: courts, availability, multi-court reservations with time-based pricing, and a role-based admin dashboard. Built as a Web API + Repository pattern layered backend (ASP.NET Core 8 + EF Core), with a static HTML/JS frontend consuming it.

---

## Features

- **Multi-court booking** — reserve several courts in a single transaction, hour-by-hour availability checks per court, differential pricing (e.g. floodlit hours after 19:00).
- **Role-based access** — `Cliente` / `Empleado` / `Admin`, enforced per endpoint and per record (a client can only see/cancel their own reservations, validated against their JWT).
- **JWT authentication** with BCrypt password hashing.
- **Admin dashboard** — revenue KPIs, reservation status breakdown, top-10 courts by bookings, top-10 clients by spend, filterable by date range and complex.
- **Full ABMC (CRUD)** for complexes, courts, court types, clients, employees, and user accounts.

## Tech Stack

- **ASP.NET Core 8** Web API
- **Entity Framework Core 8** (SQL Server)
- **JWT Bearer** authentication, **BCrypt.Net** for password hashing
- **Swashbuckle / Swagger** for API exploration
- Layered architecture: `Controllers` → `Services` → `Repositories` → `Models`, with `DTOs` at the API boundary

## API Overview

See [endpoints.md](endpoints.md) for the full endpoint reference (permissions, request bodies, and behavior notes per endpoint). Highlights:

| Module | Endpoints |
|---|---|
| Auth | `POST /api/auth/login`, `POST /api/account/register` |
| Reservations | `GET /api/reserva/complejos`, `GET /api/reserva/disponibilidad`, `POST /api/reserva`, `PUT /api/reserva/cancelar` |
| Dashboard | `GET /api/dashboard` (KPIs, charts, rankings) |
| Admin (ABMC) | Clients, employees, courts, court types, complexes, user accounts |

## Setup

```powershell
# 1. Restore and create the database
dotnet restore
dotnet ef database update   # or run database/create-database.sql directly in SQL Server

# 2. Set your own JWT signing key (don't use the placeholder in appsettings.json)
dotnet user-secrets set "Jwt:Key" "<your-own-random-secret>"

# 3. Run
dotnet run
```

The frontend (`Front/`) is static HTML/JS — open `Front/index.html` directly or serve it from any static file server, pointed at the running API.

## Academic Context

Developed as the final integrative assignment (TPI) for Programación II at UTN FRC — object-oriented design, layered architecture, a Web API consumed by a web client, and JWT-based auth were the core requirements. Cleaned up for portfolio presentation: removed a stray debug file, moved the raw DB creation script into `database/`, and replaced a hardcoded JWT signing key with a placeholder.

---

*Developed by [Martin Lantieri](https://github.com/Lantieridev) - 2026*
