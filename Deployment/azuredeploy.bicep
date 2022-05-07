@description('The base name to use for the resources that will be provisioned.')
@minLength(1)
param baseResourceName string

@description('The client ID of the Azure AD app, e.g., 123e4567-e89b-12d3-a456-426655440000.')
@minLength(36)
@maxLength(36)
param appClientId string

@description('The client secret of the Azure AD app.')
@minLength(1)
@secure()
param appClientSecret string

@description('How the app will be hosted on a domain that is not *.azurewebsites.net. Azure Front Door is an easy option that the template can set up automatically, but it comes with ongoing monthly costs.')
@allowed([
  'Custom domain name (recommended)'
  'Azure Front Door'
])
param customDomainOption string = 'Azure Front Door'

@description('Custom domain name (if chosen).')
param customDomainName string = ''

@description('The ID of the tenant to which the app will be deployed.')
@minLength(1)
@maxLength(36)
param tenantId string = subscription().tenantId

@description('The pricing tier for the hosting plan.')
@allowed([
  'Basic'
  'Standard'
  'Premium'
])
param sku string = 'Standard'

@description('The size of the hosting plan (small = 1, medium = 2, or large = 3).')
@allowed([
  '1'
  '2'
  '3'
])
param planSize string = '2'

@description('Location for all resources.')
param location string = resourceGroup().location

@description('The URL to the GitHub repository to deploy.')
param gitRepoUrl string = 'https://github.com/gence/microsoft-teams-apps-contactgrouplookup.git'

@description('The branch of the GitHub repository to deploy.')
param gitBranch string = 'master'

@description('Number of seconds to cache user presence information in memory.')
param cacheInterval int = 60

var uniqueHash = uniqueString(subscription().subscriptionId, resourceGroup().id, baseResourceName)
var appName = baseResourceName
var appDomain = '${appName}.azurewebsites.net'
var hostingPlanName = baseResourceName
var storageAccountName = '${substring(baseResourceName, 0, 11)}${uniqueHash}'
var appInsightsName = baseResourceName
var logAnalyticsName = baseResourceName
var keyVaultName = '${substring(baseResourceName, 0, 11)}${uniqueHash}'
var useFrontDoor = (customDomainOption == 'Azure Front Door')
var frontDoorName = baseResourceName
var sharedSkus = [
  'Free'
  'Shared'
]
var isSharedPlan = contains(sharedSkus, sku)
var skuFamily = ((sku == 'Shared') ? 'D' : take(sku, 1))

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-08-01' = {
  kind: 'StorageV2'
  location: location
  name: storageAccountName
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    minimumTlsVersion: 'TLS1_2'
  }
}

resource hostingPlan 'Microsoft.Web/serverfarms@2021-03-01' = {
  location: location
  name: hostingPlanName
  sku: {
    name: (isSharedPlan ? '${skuFamily}1' : '${skuFamily}${planSize}')
    tier: sku
    size: '${skuFamily}${planSize}'
    family: skuFamily
    capacity: 0
  }
}

resource app 'Microsoft.Web/sites@2021-03-01' = {
  location: location
  name: appName
  identity: {
    type: 'SystemAssigned'
  }
  dependsOn: [
    logAnalyticsWorkspace
    frontDoorEndpoint
  ]
  properties: {
    serverFarmId: hostingPlan.id
    enabled: true
    reserved: false
    clientAffinityEnabled: true
    clientCertEnabled: false
    hostNamesDisabled: false
    containerSize: 0
    dailyMemoryTimeQuota: 0
    httpsOnly: true
    siteConfig: {
      alwaysOn: true
      appSettings: [
        {
          name: 'AzureAd:Instance'
          value: 'https://login.microsoftonline.com/'
        }
        {
          name: 'AzureAd:TenantId'
          value: tenantId
        }
        {
          name: 'AzureAd:ClientId'
          value: appClientId
        }
        {
          name: 'AzureAd:ClientSecret'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=ClientSecret)'
        }
        {
          name: 'AzureAd:ApplicationIdURI'
          value: 'api://${useFrontDoor ? frontDoorEndpoint.properties.hostName : customDomainName}/${appClientId}'
        }
        {
          name: 'AzureAd:ValidIssuers'
          value: 'https://login.microsoftonline.com/TENANT_ID/v2.0,https://sts.windows.net/TENANT_ID/'
        }
        {
          name: 'AzureAd:GraphScope'
          value: 'https://graph.microsoft.com/User.Read openid profile https://graph.microsoft.com/Group.Read.All https://graph.microsoft.com/User.Read.All https://graph.microsoft.com/Presence.Read.All'
        }
        {
          name: 'CacheInterval'
          value: '${cacheInterval}'
        }
        {
          name: 'ApplicationInsights:ConnectionString'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=AppInsightsConnString)'
        }
        {
          name: 'Storage:ConnectionString'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=StorageConnString)'
        }
        {
          name: 'ApplicationInsights:LogLevel:Default'
          value: 'Information'
        }
        {
          name: 'WEBSITE_NODE_DEFAULT_VERSION'
          value: '~16'
        }
        {
          name: 'SCM_COMMAND_IDLE_TIMEOUT'
          value: '600'
        }
      ]
      cors: {
        supportCredentials: true
        allowedOrigins: [
          'https://${useFrontDoor ? frontDoorEndpoint.properties.hostName : customDomainName}'
        ]
      }
      ftpsState: 'Disabled'
      metadata: [
        {
            name: 'CURRENT_STACK'
            value: 'dotnetcore'
        }
      ]
      minTlsVersion: '1.2'
      netFrameworkVersion: 'v6.0'
      scmMinTlsVersion: '1.2'
      use32BitWorkerProcess: false
      keyVaultReferenceIdentity: 'SystemAssigned'
    }
  }
}

resource appSourceControl 'Microsoft.Web/sites/sourcecontrols@2021-03-01' = if (!empty(gitRepoUrl)) {
  parent: app
  name: 'web'
  properties: {
    repoUrl: gitRepoUrl
    branch: gitBranch
    isManualIntegration: true
  }
}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2021-06-01' = {
  name: logAnalyticsName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 90
    workspaceCapping: {
      dailyQuotaGb: 1
    }
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  kind: 'web'
  name: appInsightsName
  location: location
  tags: {
    'hidden-link:${resourceGroup().id}/providers/Microsoft.Web/sites/${appName}': 'Resource'
  }
  properties: {
    Application_Type: 'web'
    Request_Source: 'rest'
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2021-11-01-preview' = {
  name: keyVaultName
  location: location
  properties: {
    accessPolicies: [
      {
        objectId: app.identity.principalId
        permissions: {
          certificates: []
          keys: []
          secrets: [
            'get'
          ]
        }
        tenantId: tenantId
      }
    ]
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: false
    enableSoftDelete: true
    publicNetworkAccess: 'Enabled'
    sku: {
      family: 'A'
      name: 'standard'
    }
    softDeleteRetentionInDays: 90
    tenantId: tenantId
  }
}

resource kvSecretAppInsightsConnString 'Microsoft.KeyVault/vaults/secrets@2021-11-01-preview' = {
  name: 'AppInsightsConnString'
  parent: keyVault
  properties: {
    attributes: {
      enabled: true
    }
    value: '${reference(appInsights.id, '2020-02-02').ConnectionString}'
  }
}

resource kvSecretClientSecret 'Microsoft.KeyVault/vaults/secrets@2021-11-01-preview' = {
  name: 'ClientSecret'
  parent: keyVault
  properties: {
    attributes: {
      enabled: true
    }
    value: appClientSecret
  }
}

resource kvSecretStorageConnString 'Microsoft.KeyVault/vaults/secrets@2021-11-01-preview' = {
  name: 'StorageConnString'
  parent: keyVault
  properties: {
    attributes: {
      enabled: true
    }
    value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=${listKeys(storageAccount.id, '2021-08-01').keys[0].value}'
  }
}

resource frontDoorProfile 'Microsoft.Cdn/profiles@2021-06-01' = if (useFrontDoor) {
  name: frontDoorName
  location: 'global'
  sku: {
    name: 'Standard_AzureFrontDoor'
  }
}

resource frontDoorEndpoint 'Microsoft.Cdn/profiles/afdEndpoints@2021-06-01' = if (useFrontDoor) {
  name: frontDoorName
  parent: frontDoorProfile
  location: 'global'
  properties: {
    enabledState: 'Enabled'
  }
}

resource frontDoorOriginGroup 'Microsoft.Cdn/profiles/originGroups@2021-06-01' = if (useFrontDoor) {
  name: '${frontDoorName}OriginGroup'
  parent: frontDoorProfile
  properties: {
    loadBalancingSettings: {
      sampleSize: 4
      successfulSamplesRequired: 3
    }
    healthProbeSettings: {
      probePath: '/'
      probeRequestType: 'HEAD'
      probeProtocol: 'Https'
      probeIntervalInSeconds: 100
    }
  }
}

resource frontDoorOrigin 'Microsoft.Cdn/profiles/originGroups/origins@2021-06-01' = if (useFrontDoor) {
  name: '${frontDoorName}Origin'
  parent: frontDoorOriginGroup
  properties: {
    hostName: appDomain
    httpPort: 80
    httpsPort: 443
    originHostHeader: appDomain
    priority: 1
    weight: 50
  }
}

resource frontDoorRoute 'Microsoft.Cdn/profiles/afdEndpoints/routes@2021-06-01' = if (useFrontDoor) {
  name: '${frontDoorName}Route'
  parent: frontDoorEndpoint
  dependsOn:[
    frontDoorOrigin // This explicit dependency is required to ensure that the origin group is not empty when the route is created.
  ]
  properties: {
    originGroup: {
      id: frontDoorOriginGroup.id
    }
    supportedProtocols: [
      'Https'
    ]
    patternsToMatch: [
      '/*'
    ]
    forwardingProtocol: 'HttpsOnly'
    linkToDefaultDomain: 'Enabled'
    httpsRedirect: 'Enabled'
  }
}

output appClientId string = appClientId
output appDomain string = (useFrontDoor ? frontDoorEndpoint.properties.hostName : 'Please create custom domain name ${customDomainName} for ${appDomain} and use that in the manifest')
