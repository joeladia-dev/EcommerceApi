# Ecommerce API

ASP.NET Core Web API for a simple e-commerce backend with products, categories, JWT auth, validation, and PostgreSQL persistence.

## Tech Stack

- .NET 10 (`net10.0`)
- ASP.NET Core Web API
- Entity Framework Core + Npgsql (PostgreSQL)
- AutoMapper
- FluentValidation
- JWT Bearer Authentication
- Swagger / OpenAPI

## Features

- Product and category CRUD endpoints
- Filtering, search, sorting, and pagination for products
- Category-to-products endpoint
- JWT login endpoint with role-based authorization
- Global exception handling middleware
- Seed data support for development

## Project Structure

- `EcommerceApi/` main API project
- `EcommerceApi/Controllers/` API endpoints
- `EcommerceApi/Data/` EF Core `DbContext` and seed logic
- `EcommerceApi/Models/` entities
- `EcommerceApi/Dtos/` request/response DTOs
- `EcommerceApi/Validators/` FluentValidation validators
- `.github/workflows/` GitHub Actions deployment workflows
- `AZURE_DEPLOYMENT.md` Azure deployment setup notes

## Getting Started

### Prerequisites

- .NET SDK 10
- PostgreSQL (local or remote)
- Optional: EF Core CLI

Install EF CLI (if needed):

```bash
dotnet tool install --global dotnet-ef
```

### 1) Clone and restore

```bash
git clone <your-repo-url>
cd EcommerceApi
dotnet restore EcommerceApi/EcommerceApi.csproj
```

### 2) Configure settings

Create your local development config at:

- `EcommerceApi/appsettings.Development.json`

Example:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ecommerce_dev;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Issuer": "EcommerceApi",
    "Audience": "EcommerceApi",
    "Key": "replace_with_a_long_random_secret_key"
  },
  "SeedDataOnStartup": true
}
```

> `appsettings.Development.json` is gitignored by default.

### 3) Apply database migrations

```bash
dotnet ef database update --project EcommerceApi/EcommerceApi.csproj --startup-project EcommerceApi/EcommerceApi.csproj
```

### 4) Run the API

```bash
dotnet run --project EcommerceApi/EcommerceApi.csproj
```

In development, Swagger UI is available at:

- `http://localhost:<port>/swagger`

## Authentication

Login endpoint:

- `POST /api/auth/login`

Demo credentials (for local testing only):

- Admin: `admin` / `password_123`
- User: `user` / `password_321`

Use returned token as:

- `Authorization: Bearer <token>`

## Useful API Endpoints

- `GET /api/categories`
- `GET /api/categories/{id}`
- `GET /api/categories/{id}/products`
- `POST /api/categories` (auth required)
- `PUT /api/categories/{id}` (auth required)
- `DELETE /api/categories/{id}` (admin only)
- `GET /api/products`
- `GET /api/products/{id}`
- `POST /api/products` (auth required)
- `PUT /api/products/{id}` (auth required)
- `DELETE /api/products/{id}` (admin only)

## API Request Collection

Use the provided HTTP file for quick testing:

- `EcommerceApi/EcommerceApi.http`

## Automated Tests

Integration tests are located in:

- `tests/EcommerceApi.ApiTests`

Run only the API integration tests:

```bash
dotnet test tests/EcommerceApi.ApiTests/EcommerceApi.ApiTests.csproj
```

Run all tests in the solution:

```bash
dotnet test EcommerceApi.slnx
```

## Deployment

Azure App Service deployment docs are in:

- `AZURE_DEPLOYMENT.md`

Infrastructure as Code files are in:

- `infra/main.bicep`
- `infra/main.parameters.json`

GitHub Actions workflows are in:

- `.github/workflows/deploy-api.yml`
- `.github/workflows/reusable-azure-appservice-api.yml`

## Notes

- The current auth flow uses demo credentials for development/testing.
- Replace JWT key, credentials, and connection strings before production use.
