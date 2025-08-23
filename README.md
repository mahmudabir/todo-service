<div align="center">
	<h1>Todo Service (ASP.NET Core 9)</h1>
	<p>A layered, modular Todo REST API showcasing clean architecture style (Domain / Application / Infrastructure / WebApi / Shared) with ASP.NET Core Identity, JWT authentication (access + refresh tokens), EF Core, Serilog logging, validation, pagination, and OpenAPI/Swagger.</p>
</div>

## Table of Contents
- Overview
- Architecture
- Features
 - OAuth2 Implementation
- Tech Stack
- Getting Started
	- Prerequisites
	- Clone & Restore
	- Database & Migrations
	- Run the API
	- Environment / Configuration
- Seeded Users (Test Credentials)
- Authentication & Authorization
	- Signup `/api/auth/signup`
	- Login Alias (JSON) `/api/auth/login`
	- Password Grant (Form) `/api/auth/token`
	- Refresh Token `/api/auth/token`
- Users API
- Todos API
- Pagination & Sorting
- Validation
- Logging & Observability

---

## Overview
This service exposes endpoints to manage users and their todo items. It demonstrates:
* Clean separation of concerns via layered projects.
* JWT bearer auth with access + refresh tokens, optional single-login enforcement.
* ASP.NET Core Identity for user management.
* EF Core (code-first) with migrations & seeding.
* FluentValidation for request models.
* Consistent `Result<T>` response envelope and error shape.
* Pagination, sorting, and filtering helpers.
* Serilog structured logging to console & rolling files.
* OpenAPI (Swagger UI) in Development.

## Architecture
Project solution folders:
* `Domain` – Entities, abstractions, domain models.
* `Application` – DTOs, services (business logic), mapping profiles, validators.
* `Infrastructure` – EF Core `ApplicationDbContext`, migrations, repositories, identity, authentication helpers.
* `Shared` – Cross-cutting models, enums, constants, pagination, result wrappers.
* `WebApi` – Presentation layer (controllers, DI wiring, middleware, configuration).

## Features
* JWT (HS256) Access + Refresh, rotation/renewal logic, absolute lifetime & revocation.
* OAuth2 Password (Resource Owner Password Credentials) flow exposed in Swagger & Postman for one-click token retrieval.
* Lockout policy (max failed attempts, lockout duration) configured in `appsettings.json` (see `JwtSettings.MaximumFailedAccessCount`, `UserLockoutMinutes`).
* Device info & IP capture for refresh tokens.
* Optional single-login or single-refresh-token strategies (toggle via settings).
* CRUD for Todos with validation, pagination & sorting.
* User management (query, create, update, (de)activate, signup, auth token issuing).

## Tech Stack
* .NET 9 (`net9.0` TargetFramework)
* ASP.NET Core Web API
* Entity Framework Core + SQL Server (LocalDB default)
* ASP.NET Core Identity
* FluentValidation
* Serilog (Console + File sinks)
* OpenTelemetry packages (instrumentation ready)
* Swashbuckle (Swagger / OpenAPI)

## Getting Started
### 1. Prerequisites
Install locally:
* .NET SDK 9.0
* SQL Server or LocalDB (default connection uses `(localdb)\\MSSQLLocalDB`)
* (Optional) Docker if you adapt to containerized SQL.

### 2. Clone & Restore
```powershell
git clone https://github.com/mahmudabir/todo-service.git
cd todo-service
dotnet restore
```

### 3. Database & Migrations
By default the API automatically applies migrations at startup in Development (`app.ApplyMigrations();`). To apply manually:
```powershell
dotnet ef database update --project src/Infrastructure --startup-project src/WebApi
```
Adjust connection string in `src/WebApi/appsettings.Development.json` or `appsettings.json` (key: `ConnectionStrings:Database`).

### 4. Run the API
```powershell
dotnet run --project src/WebApi
```
The API will typically listen on https://localhost:5001 and http://localhost:5000 (exact ports depend on your profile). Swagger UI (Development) at `/swagger`.

### 5. Environment / Configuration
Key section: `JwtSettings` in `appsettings.json`:
```jsonc
"JwtSettings": {
	"Key": "secret_key.secret_key.secret_key",
	"Issuer": "SoftoverseCqrsKitApi",
	"Audience": "SoftoverseCqrsKitUser",
	"AccessTokenExpirationMinutes": 20,
	"RefreshTokenExpirationMinutes": 120,
	"ExtendRefreshTokenEverytime": true,
	"RevokeRefreshTokenAfterAbsoluteExpiration": true,
	"RefreshTokenAbsoluteExpirationMinutes": 43200,
	"RemoveExpiredRefreshTokenBeforeDays": 7,
	"SingleRefreshTokenPerUser": false,
	"SingleLoginEnabled": false,
	"MaximumFailedAccessCount": 3,
	"UserLockoutMinutes": 60,
	"ClientId": "Softoverse",
	"ClientSecret": "CqrsKit",
	"TokenUrl": "http://localhost:5000/api/auth/token",
	"TokenRefreshUrl": "http://localhost:5000/api/auth/token"
}
```

## Seeded Users (Test Credentials)
Migration `20250823141729_User_Seed` inserts two users:
| Username | Email             | Password (raw) |
|----------|-------------------|----------------|
| user1    | user1@example.com | user1@1A       |
| user2    | user2@example.com | user2@1A      |

> NOTE: Password hash in migration corresponds to the demonstrated raw password `user1@1A` & `user2@1A`. If login fails, reset passwords via Identity management or recreate migration.

## Authentication & Authorization
Bearer Authentication: obtain Access + Refresh tokens, then include:
```
Authorization: Bearer {access_token}
```

### Grant Types
Two flows exposed via the same `/api/auth/token` endpoint:
1. Password grant (form fields)
2. Refresh token grant

Alias `/api/auth/login` accepts JSON body for convenience.

### OAuth2 (Password Flow) Implementation
The API registers an OAuth2 security scheme named `oauth2` (Password flow) in Swagger (`SwaggerExtensions`). Configuration points to `JwtSettings:TokenUrl` & `JwtSettings:TokenRefreshUrl`. Scopes (example): `apiScope`, `uiScope`. While scopes are optional in current authorization checks (no scope-based enforcement yet), they illustrate how to extend fine-grained access later.

In Swagger UI (Development):
1. Click Authorize.
2. Choose oauth2 scheme.
3. Enter seeded username & password (`user1` / `Pass@123`).
4. (Client credentials auto-filled) Obtain token; Swagger persists authorization for Try-It-Out calls.

This mirrors a Resource Owner Password Credentials flow (deprecated in formal OAuth 2.1 drafts, but intentionally implemented here for demonstration / rapid testing). A future enhancement could add Authorization Code + PKCE.

### 1. Password Grant (Form) – POST `/api/auth/token`
Content-Type: `application/x-www-form-urlencoded` (or multipart form)
Form fields:
```
grant_type=password&username=user1&password=Pass@123&client_id=Softoverse&client_secret=CqrsKit
```
Successful Response (200):
```jsonc
{
	"message": "Login Successful.",
	"accessToken": "...",
	"refreshToken": "...",
	"tokenType": "Bearer",
	"expiresIn": 1200,
	"refreshExpiresIn": 7200,
	"login": { "accessToken": "...", "tokenType": "Bearer" },
	"roles": []
}
```

Common Errors:
* 400 Invalid Username or Password.
* 400 Already logged in (if `SingleLoginEnabled` true).
* 400 Account locked (after failed attempts reaches threshold).

### 1a. JSON Login Alias – POST `/api/auth/login`
Body:
```json
{ "username": "user1", "password": "user1@1A" }
{ "username": "user2", "password": "user2@1A" }
```
Response: same as password grant above.

### 2. Refresh Token – POST `/api/auth/token`
Form fields:
```
grant_type=refresh_token&refresh_token={refreshToken}
```
Returns new access token (and possibly extended refresh token depending on settings).
Errors: 401 if token revoked/expired; 400 if missing or invalid.

### 3. Signup – POST `/api/auth/signup`
JSON Body:
```json
{
	"username": "newuser",
	"email": "newuser@example.com",
	"phoneNumber": "+1000000000",
	"password": "Pass@123",
	"dateOfBirth": "1990-01-01"
}
```
Responses:
* 200 Success wrapper with message "Registered successfully." or validation errors.

### Postman Collection (Pre-configured OAuth2)
A Postman collection (`Todo Web API.postman_collection.json`) and environment (`Todo Service Environment.postman_environment.json`) are included at the repository root.

Reviewer Convenience:
* The collection description mirrors this README for context.
* Requests already include the Bearer auth placeholder or are wired for the OAuth2 password flow.
* To authenticate: open the collection's Authorization tab (set to OAuth2), click Get New Access Token (it uses the same token URL `/api/auth/token`), supply `user1` / `Pass@123`, then Postman auto-injects the token into subsequent requests.
* Environment variables: `base_url`, maybe `access_token` (auto-updated when saving token) — adjust `base_url` if your dev port differs.

This setup minimizes reviewer effort: no manual cURL crafting, single click to authorize and exercise endpoints.

## Users API (Requires Bearer Token)
Base Route: `/api/users` (This api were just to manupulate user data via API)

| Method | Route | Description | Query/Body |
|--------|-------|-------------|------------|
| GET | `/api/users` | List users (filter by `q`) | `?q=term` (optional) |
| GET | `/api/users/{username}` | Get by username |  |
| POST | `/api/users` | Create user | `{ username, email, phoneNumber, password }` |
| PUT | `/api/users/{username}` | Update (email/phone/password) | `{ username, email, phoneNumber, password? }` |
| POST | `/api/users/deactivate/{username}` | Lock user (soft delete) |  |
| POST | `/api/users/activate/{username}` | Unlock user |  |

Responses use `Result<T>` envelope with `payload`, `message`, optional `errors`.

## Todos API (Requires Bearer Token)
Base Route: `/api/todos`

| Method | Route | Description | Body / Params |
|--------|-------|-------------|---------------|
| GET | `/api/todos` | Paged list | Query: `query`, pagination + sorting |
| GET | `/api/todos/{id}` | Single todo | Path id (long) |
| POST | `/api/todos` | Create | `TodoCreateViewModel` (e.g. `{ "title": "Test", "description": "..." }`) |
| PUT | `/api/todos/{id}` | Update | `TodoUpdateViewModel` |
| DELETE | `/api/todos/{id}` | Delete | Path id |

### Pagination & Sorting
The `GET /api/todos` endpoint accepts:
* `pageNumber` (default 1)
* `pageSize` (default e.g. 10/20 depending on implementation)
* Sort parameters via `sortable` model (e.g. `sortBy=createdAt&sortOrder=desc`).
Response returns `Result<PagedData<TodoViewModel>>` containing page metadata + items.

## Validation
* Uses FluentValidation for todo create/update models.
* Validation failures return a `Result.Error` with `errors` dictionary.

## Logging & Observability
* Serilog writes structured logs to console and `logs/log-<date>.log` (hourly rolling, retention 168 files ~ 7 days).
* OpenTelemetry packages are referenced — you can configure an OTLP endpoint to export traces/metrics/logs.

## Error Handling
Global exception handling middleware (invoked via `app.UseExceptionHandler();`) standardizes error responses.

## Security Notes
* Replace `JwtSettings:Key` with a secure secret in production (minimum 32 random bytes).
* Enable HTTPS enforcement and consider HSTS.
* Consider enabling `SingleLoginEnabled` and/or `SingleRefreshTokenPerUser` for stricter session management.
* Rotate secrets and implement refresh token revocation list persistence if scaling out.

---
### Quick Test Flow
1. Start API.
2. Obtain token:
	 ```powershell
	 # Form URL encoded example using PowerShell Invoke-RestMethod
	 Invoke-RestMethod -Method Post -Uri http://localhost:5000/api/auth/token -Body @{grant_type='password';username='user1';password='Pass@123';client_id='Softoverse';client_secret='CqrsKit'}
	 ```
3. Call Todos with returned access token:
	 ```powershell
	 $token = '<ACCESS_TOKEN>'
	 Invoke-RestMethod -Headers @{ Authorization = "Bearer $token" } -Uri http://localhost:5000/api/todos
	 ```
