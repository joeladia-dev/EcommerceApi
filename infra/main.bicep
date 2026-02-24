targetScope = 'resourceGroup'

@description('Location for all resources. Defaults to resource group location.')
param location string = resourceGroup().location

@description('App Service plan name.')
param appServicePlanName string

@description('Web App name. Must be globally unique in Azure App Service DNS.')
param webAppName string

@description('App Service SKU name (for example: B1, P1v3).')
param appServiceSkuName string = 'B1'

@description('App Service worker count.')
@minValue(1)
param appServiceSkuCapacity int = 1

@description('Runtime stack for Linux Web App.')
param linuxFxVersion string = 'DOTNETCORE|10.0'

@description('PostgreSQL flexible server name. Must be globally unique in postgres.database.azure.com DNS.')
param postgresServerName string

@description('PostgreSQL admin username (without @server suffix).')
param postgresAdminLogin string

@secure()
@description('PostgreSQL admin password.')
param postgresAdminPassword string

@description('PostgreSQL database name for the API.')
param postgresDatabaseName string = 'ecommerce_prod'

@description('PostgreSQL server compute SKU (for example: Standard_B1ms, Standard_D2s_v3).')
param postgresSkuName string = 'Standard_B1ms'

@allowed([
  'Burstable'
  'GeneralPurpose'
  'MemoryOptimized'
])
@description('PostgreSQL pricing tier. Must align with postgresSkuName.')
param postgresTier string = 'Burstable'

@allowed([
  -1
  1
  2
  3
])
@description('Availability zone for PostgreSQL. Use -1 for no preference.')
param postgresAvailabilityZone int = -1

@allowed([
  'Disabled'
  'Enabled'
])
@description('Enable public network access for PostgreSQL server.')
param postgresPublicNetworkAccess string = 'Enabled'

@description('When true, creates firewall rule 0.0.0.0 to allow Azure services.')
param allowAzureServicesToPostgres bool = true

@description('Tags applied to all resources.')
param tags object = {}

resource appServicePlan 'Microsoft.Web/serverfarms@2024-11-01' = {
  name: appServicePlanName
  location: location
  kind: 'linux'
  sku: {
    name: appServiceSkuName
    capacity: appServiceSkuCapacity
  }
  properties: {
    reserved: true
  }
  tags: tags
}

resource webApp 'Microsoft.Web/sites@2024-11-01' = {
  name: webAppName
  location: location
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: linuxFxVersion
      alwaysOn: toUpper(appServiceSkuName) == 'F1' ? false : true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'SeedDataOnStartup'
          value: 'false'
        }
        {
          name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED'
          value: 'true'
        }
      ]
    }
  }
  tags: tags
}

resource postgresServer 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: postgresServerName
  location: location
  sku: {
    name: postgresSkuName
    tier: postgresTier
  }
  properties: {
    administratorLogin: postgresAdminLogin
    administratorLoginPassword: postgresAdminPassword
    version: '16'
    availabilityZone: postgresAvailabilityZone == -1 ? null : string(postgresAvailabilityZone)
    createMode: 'Create'
    storage: {
      storageSizeGB: 32
      autoGrow: 'Enabled'
      tier: 'P4'
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    network: {
      publicNetworkAccess: postgresPublicNetworkAccess
    }
    highAvailability: {
      mode: 'Disabled'
    }
    maintenanceWindow: {
      customWindow: 'Enabled'
      dayOfWeek: 0
      startHour: 1
      startMinute: 0
    }
  }
  tags: tags
}

resource postgresDatabase 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  parent: postgresServer
  name: postgresDatabaseName
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

resource allowAzureServicesRule 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = if (allowAzureServicesToPostgres) {
  parent: postgresServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

output webAppName string = webApp.name
output webAppDefaultHostName string = webApp.properties.defaultHostName
output postgresServerName string = postgresServer.name
output postgresServerFqdn string = postgresServer.properties.fullyQualifiedDomainName
output postgresDatabaseName string = postgresDatabase.name
output postgresAdminLogin string = postgresAdminLogin
output postgresConnectionStringTemplate string = 'Host=${postgresServer.properties.fullyQualifiedDomainName};Port=5432;Database=${postgresDatabase.name};Username=${postgresAdminLogin};Password=<SET_AT_RUNTIME>;Ssl Mode=Require;Trust Server Certificate=true'
