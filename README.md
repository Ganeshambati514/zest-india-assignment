# Student Management System — ASP.NET Core Web API

A layered, JWT-secured Web API for managing students, built for the Zest India IT Pvt Ltd
Full Stack Developer technical assignment.

## Tech Stack

- **.NET 8** / ASP.NET Core Web API
- **Entity Framework Core** + **SQL Server**
- **JWT Bearer Authentication**
- **Serilog** (console + rolling file logs)
- **Swagger / Swashbuckle** (OpenAPI docs with JWT support)
- **xUnit + Moq + FluentAssertions** (unit tests)
- **Docker / docker-compose**

## Architecture

Clean, layered architecture split into four projects plus a test project:

```
StudentManagementSystem/
├── src/
│   ├── StudentManagementSystem.API/              # Controllers, middleware, DI, Program.cs
│   ├── StudentManagementSystem.Application/       # DTOs, service interfaces & implementations,
│   │                                               # custom exceptions, ApiResponse<T> wrapper
│   ├── StudentManagementSystem.Domain/            # Student entity (no external dependencies)
│   └── StudentManagementSystem.Infrastructure/    # EF Core DbContext, repository implementation
├── tests/
│   └── StudentManagementSystem.Tests/             # Unit tests for StudentService
├── Dockerfile
├── docker-compose.yml
└── StudentManagementSystem.sln
```

**Flow:** `Controller → Service (business logic) → Repository (data access) → EF Core → SQL Server`

- **Domain** has zero dependencies — just the `Student` entity.
- **Application** depends only on Domain. Defines `IStudentRepository` / `IStudentService`
  interfaces (dependency inversion) so the API and Infrastructure don't depend on each other directly.
- **Infrastructure** implements `IStudentRepository` using EF Core.
- **API** wires everything together in `Program.cs`, exposes REST endpoints, and hosts
  cross-cutting concerns (auth, logging, exception handling, Swagger).

## Features

| Requirement | Implementation |
|---|---|
| CRUD APIs | `GET /api/students`, `GET /api/students/{id}`, `POST /api/students`, `PUT /api/students/{id}`, `DELETE /api/students/{id}` |
| JWT Authentication | `POST /api/auth/login` issues a token; all `/api/students/*` endpoints require `Authorize` |
| Global Exception Handling | `ExceptionHandlingMiddleware` catches all unhandled exceptions and returns a consistent JSON error shape |
| Logging | Serilog logs every request (`UseSerilogRequestLogging`) plus structured logs in the service layer, written to console and `Logs/log-.txt` |
| Swagger | Available at `/swagger`, with a "Bearer" auth button to paste your JWT |
| Layered Architecture | Controller → Service → Repository, see above |

### Student table

| Column | Type |
|---|---|
| Id | int, PK, identity |
| Name | nvarchar(100) |
| Email | nvarchar(150), unique |
| Age | int |
| Course | nvarchar(100) |
| CreatedDate | datetime2, default UTC now |

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (local install, or via Docker — see below)
- (Optional) Docker Desktop, if you want to run everything containerized

## Setup — Run Locally

1. **Clone the repo**
   ```bash
   git clone <your-repo-url>
   cd StudentManagementSystem
   ```

2. **Configure the connection string**

   Edit `src/StudentManagementSystem.API/appsettings.json` (or better, use `dotnet user-secrets`)
   and point `ConnectionStrings:DefaultConnection` at your SQL Server instance:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost,1433;Database=StudentManagementDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True;"
   }
   ```

   No local SQL Server? Spin one up quickly with Docker:
   ```bash
   docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Your_password123" \
     -p 1433:1433 --name sms-sqlserver -d mcr.microsoft.com/mssql/server:2022-latest
   ```

3. **Install the EF Core CLI tool** (one-time, if you don't already have it)
   ```bash
   dotnet tool install --global dotnet-ef
   ```

4. **Restore packages**
   ```bash
   dotnet restore
   ```

5. **Create the initial migration** (the repo intentionally ships without a `Migrations/`
   folder so it isn't tied to one machine's EF tooling version — generate it locally):
   ```bash
   dotnet ef migrations add InitialCreate \
     --project src/StudentManagementSystem.Infrastructure \
     --startup-project src/StudentManagementSystem.API
   ```

6. **Run the API** — it applies migrations automatically on startup (see `Program.cs`),
   so this alone creates the database and table:
   ```bash
   dotnet run --project src/StudentManagementSystem.API
   ```

7. **Open Swagger**: `https://localhost:7163/swagger` (or the HTTP URL shown in the console).

## Authentication

Default dev credentials (configured in `appsettings.json` under `AdminCredentials`):

```
Username: admin
Password: Admin@123
```

**Login:**
```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "Admin@123"
}
```

Response:
```json
{
  "success": true,
  "message": "Login successful",
  "data": { "token": "eyJhbGciOi...", "expiresInMinutes": 60 }
}
```

Then, in Swagger, click **Authorize** and enter `Bearer <token>`, or add the header manually
to any request:
```
Authorization: Bearer <token>
```

## API Endpoints

All `/api/students` endpoints require a valid JWT.

| Method | Route | Description |
|---|---|---|
| POST | `/api/auth/login` | Get a JWT token |
| GET | `/api/students` | Get all students |
| GET | `/api/students/{id}` | Get a student by Id |
| POST | `/api/students` | Add a new student |
| PUT | `/api/students/{id}` | Update a student |
| DELETE | `/api/students/{id}` | Delete a student |

**Example — Create a student:**
```http
POST /api/students
Authorization: Bearer <token>
Content-Type: application/json

{
  "name": "Jane Doe",
  "email": "jane.doe@example.com",
  "age": 21,
  "course": "Computer Science"
}
```

Every response is wrapped consistently:
```json
{
  "success": true,
  "message": "Student created successfully",
  "data": { "id": 1, "name": "Jane Doe", "email": "jane.doe@example.com", "age": 21, "course": "Computer Science", "createdDate": "2026-07-22T10:00:00Z" }
}
```

Errors follow the same shape with `success: false`, e.g. a 404:
```json
{ "success": false, "message": "Student with Id 99 was not found", "data": null }
```

## Running with Docker

```bash
docker-compose up --build
```

This starts SQL Server and the API together. The API waits for SQL Server's healthcheck
before starting, then applies migrations automatically. API will be available at
`http://localhost:8080/swagger`.

> Note: generate the `InitialCreate` migration locally (step 5 above) **before** building
> the Docker image, since it's copied in along with the rest of the source.

## Running Unit Tests

```bash
dotnet test
```

Covers the service layer (`StudentService`) with mocked repository dependencies:
success paths, not-found cases, and duplicate-email validation.

## Design Notes / Assumptions

- Since no separate Users table was specified, JWT auth validates against a single
  admin account from configuration (`AdminCredentials` in `appsettings.json`). This is
  intentionally simple for the assignment scope — swap in ASP.NET Core Identity or a
  Users table for a real multi-user system.
- Email is enforced unique at both the database level (unique index) and the service
  layer (friendly 400 error instead of a raw DB exception).
- Global exception middleware maps `NotFoundException` → 404, `ValidationException` → 400,
  `UnauthorizedAccessException` → 401, and anything else → 500 (with the real exception
  message only ever included in `Development`).
- Secrets in `appsettings.json` (JWT key, DB password) are placeholders for local/dev use —
  in a real deployment these should come from `dotnet user-secrets`, environment variables,
  or a secret manager (Azure Key Vault, AWS Secrets Manager, etc.), never committed to source control.

## Bonus Items Included

- ✅ Unit tests (xUnit + Moq + FluentAssertions)
- ✅ Docker + docker-compose (API + SQL Server)
- ⬜ Angular/React UI — not included; the API is CORS-enabled (`AllowAll` policy) so a
  frontend can be added and pointed at it directly.
