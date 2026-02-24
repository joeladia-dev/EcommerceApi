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
az group create --name rg-ecommerceapi-prod --location centralus

az appservice plan create \
  --name asp-ecommerceapi-prod \
  --resource-group rg-ecommerceapi-prod \
  --sku F1 \
  --is-linux

az webapp create \
  --name app-ecommerceapi-prod \
  --resource-group rg-ecommerceapi-prod \
  --plan asp-ecommerceapi-prod \
  --runtime "DOTNETCORE|10.0"

az postgres flexible-server create \
  --name pg-ecommerceapi-prod \
  --resource-group rg-ecommerceapi-prod \
  --location centralus \
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
>
> Free-tier scan results for this subscription:
>
> - Works: `centralus`, `westus3`, `canadacentral`, `westeurope`, `francecentral`
> - Blocked by quota: `eastus`, `eastus2`, `southcentralus`, `westus`, `westus2`, `northcentralus`, `northeurope`, `uksouth`

## 1.1) Provision with Bicep (recommended)

This repo now includes IaC under `infra/`:

- `infra/main.bicep`
- `infra/main.parameters.json`

Update `infra/main.parameters.json` with your target names/location, then run:

```bash
az deployment group what-if \
  --resource-group rg-ecommerceapi-prod \
  --template-file infra/main.bicep \
  --parameters @infra/main.parameters.json \
  --parameters postgresAdminPassword='<STRONG_PASSWORD>'

az deployment group create \
  --resource-group rg-ecommerceapi-prod \
  --template-file infra/main.bicep \
  --parameters @infra/main.parameters.json \
  --parameters postgresAdminPassword='<STRONG_PASSWORD>'
```

After provisioning, use the deployed names for GitHub variables:

- `AZURE_RESOURCE_GROUP` = your resource group (for example `rg-ecommerceapi-prod`)
- `AZURE_WEBAPP_NAME` = deployed `webAppName` from Bicep

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
- `POSTGRES_ADMIN_PASSWORD` (required only when `provision_infra=true` in workflow_dispatch)
- `POSTGRES_CONNECTION_STRING`
- `JWT_ISSUER`
- `JWT_AUDIENCE`
- `JWT_KEY`

`POSTGRES_CONNECTION_STRING` example:

```text
Host=pg-ecommerceapi-prod.postgres.database.azure.com;Port=5432;Database=ecommerce_prod;Username=pgadmin;Password=<PASSWORD>;Ssl Mode=Require;Trust Server Certificate=true
```

After any PostgreSQL password rotation, immediately update:

- App Service setting: `ConnectionStrings__DefaultConnection`
- GitHub secret: `POSTGRES_CONNECTION_STRING`

## 4) Trigger deployment

- Push to `main` (changes under `EcommerceApi/**`), or
- Run workflow manually: `deploy-api`

Pipeline behavior:

1. Restore/build/publish API
2. Optionally run integration tests (`tests/EcommerceApi.ApiTests`) when `run_tests=true`
3. Run `dotnet ef database update` using `POSTGRES_CONNECTION_STRING` (enabled by default)
4. Apply App Service settings (`ConnectionStrings__DefaultConnection`, `Jwt__*`, `SeedDataOnStartup=false`)
5. Deploy published API to Azure App Service

## 5) Manual workflow options

When running `deploy-api` manually (`workflow_dispatch`), you can choose:

- `provision_infra` (default `false`) to run Bicep provisioning before API deploy
- `run_tests` (default `false`)
- `build_configuration` (`Release` by default)

The reusable workflow also supports `run_migrations` (default `true`) if you call it from another workflow and want to temporarily disable migration execution.

When `provision_infra=true`, the workflow executes:

1. Infra job in `.github/workflows/deploy-api.yml` (`what-if` then `create` using `infra/main.bicep`)
2. `.github/workflows/reusable-azure-appservice-api.yml` (build, optional tests, migrations, app deploy)

## 6) Reuse this for future APIs

Reuse `.github/workflows/reusable-azure-appservice-api.yml` from other repos by:

- Calling it with a different `project_path` and `startup_project_path`
- Setting per-project `AZURE_RESOURCE_GROUP`, `AZURE_WEBAPP_NAME`
- Providing that project's DB/JWT secrets

This avoids rebuilding deployment logic for each new API.

## 7) Rotate PostgreSQL password (manual workflow)

Use workflow `.github/workflows/rotate-db-password.yml` when you need to rotate the PostgreSQL admin password.

What it does:

1. Rotates PostgreSQL admin password in Azure
2. Updates App Service setting `ConnectionStrings__DefaultConnection`
3. Verifies the connection string still points to Azure PostgreSQL and runs an API smoke test

How to run:

- GitHub Actions → `rotate-db-password` → `Run workflow`
- Leave `resource_group` and `webapp_name` empty to use repo variables (`AZURE_RESOURCE_GROUP`, `AZURE_WEBAPP_NAME`)

After rotation, update GitHub secret `POSTGRES_CONNECTION_STRING` before the next `deploy-api` run.

## 8) Verify deployed endpoints (manual workflow)

Use workflow `.github/workflows/verify-deployed-api.yml` to run a live endpoint verification suite against the deployed API.

What it validates:

1. Root and auth endpoints
2. Categories and products read endpoints
3. Expected authorization behavior (`401`/`403`)
4. Authenticated create/update/delete flows for categories and products

How to run:

- GitHub Actions → `verify-deployed-api` → `Run workflow`
- Optionally override `base_url` (defaults to `https://app-ecommerceapi-prod.azurewebsites.net`)

The workflow fails when any endpoint check does not match expected behavior.
