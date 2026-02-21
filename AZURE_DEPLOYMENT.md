# Azure Deployment (API + PostgreSQL + GitHub Actions)

This repository is configured to deploy the API in `EcommerceApi/` to Azure App Service and run EF Core migrations against Azure Database for PostgreSQL from GitHub Actions.

## What was added

- Reusable workflow: `.github/workflows/reusable-azure-appservice-api.yml`
- Project deploy workflow: `.github/workflows/deploy-api.yml`
- Startup behavior changes:
  - DB migrations are no longer executed during API startup.
  - Seed data runs only in development (or if `SeedDataOnStartup=true`).

## 1) One-time Azure resource setup

Create these resources once:

- Resource Group
- App Service Plan (Linux)
- Web App (ASP.NET Core)
- Azure Database for PostgreSQL Flexible Server
- PostgreSQL database for the API

You can use Azure CLI (example names):

```bash
az group create --name rg-ecommerceapi-prod --location eastus

az appservice plan create \
  --name asp-ecommerceapi-prod \
  --resource-group rg-ecommerceapi-prod \
  --sku B1 \
  --is-linux

az webapp create \
  --name app-ecommerceapi-prod \
  --resource-group rg-ecommerceapi-prod \
  --plan asp-ecommerceapi-prod \
  --runtime "DOTNETCORE|10.0"

az postgres flexible-server create \
  --name pg-ecommerceapi-prod \
  --resource-group rg-ecommerceapi-prod \
  --location eastus \
  --admin-user pgadmin \
  --admin-password "<STRONG_PASSWORD>" \
  --sku-name Standard_B1ms \
  --version 16 \
  --public-access 0.0.0.0

az postgres flexible-server db create \
  --resource-group rg-ecommerceapi-prod \
  --server-name pg-ecommerceapi-prod \
  --database-name ecommerce_prod
```

> If your region does not support `DOTNETCORE|10.0`, choose a supported runtime and retarget the API accordingly.

## 2) Configure GitHub OIDC (recommended)

Create a service principal with federated credentials for this repo/environment, and grant it access to the resource group.

At minimum, the principal needs permissions to:

- Deploy to Web App
- Update Web App app settings

Typical roles at resource-group scope:

- `Website Contributor`
- `Reader`

## 3) Configure GitHub variables and secrets

In GitHub repo settings:

### Variables

- `AZURE_RESOURCE_GROUP` = your resource group name
- `AZURE_WEBAPP_NAME` = your web app name

### Secrets

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`
- `POSTGRES_CONNECTION_STRING`
- `JWT_ISSUER`
- `JWT_AUDIENCE`
- `JWT_KEY`

`POSTGRES_CONNECTION_STRING` example:

```text
Host=pg-ecommerceapi-prod.postgres.database.azure.com;Port=5432;Database=ecommerce_prod;Username=pgadmin;Password=<PASSWORD>;Ssl Mode=Require;Trust Server Certificate=true
```

## 4) Trigger deployment

- Push to `main` (changes under `EcommerceApi/**`), or
- Run workflow manually: `deploy-api`

Pipeline behavior:

1. Restore/build/publish API
2. Run `dotnet ef database update` using `POSTGRES_CONNECTION_STRING`
3. Apply App Service settings (`ConnectionStrings__DefaultConnection`, `Jwt__*`, `SeedDataOnStartup=false`)
4. Deploy published API to Azure App Service

## 5) Reuse this for future APIs

Reuse `.github/workflows/reusable-azure-appservice-api.yml` from other repos by:

- Calling it with a different `project_path` and `startup_project_path`
- Setting per-project `AZURE_RESOURCE_GROUP`, `AZURE_WEBAPP_NAME`
- Providing that project's DB/JWT secrets

This avoids rebuilding deployment logic for each new API.
