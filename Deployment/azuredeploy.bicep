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

@description('How the app will be hosted on a domain that is not *.azurewebsites.net. Azure Front Door is an easy option that the template can set up automatically, but it comes with ongoing monthly costs. ')
@allowed([
  'Custom domain name (recommended)'
  'Azure Front Door'
])
param customDomainOption string = 'Azure Front Door'

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
var appUrl = 'https://${appDomain}'
var frontDoorDomain = '${appName}.azurefd.net'
var hostingPlanName = baseResourceName
var storageAccountName = '${substring(baseResourceName, 0, 11)}${uniqueHash}'
var appInsightsName = baseResourceName
var logAnalyticsName = baseResourceName
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
  dependsOn: [
    logAnalyticsWorkspace
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
          value: appClientSecret
        }
        {
          name: 'AzureAd:ApplicationIdURI'
          value: 'api://${frontDoorDomain}/${appClientId}'
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
          name: 'ApplicationInsights:InstrumentationKey'
          value: reference(appInsights.id, '2020-02-02').InstrumentationKey
        }
        {
          name: 'ApplicationInsights:ConnectionString'
          value: reference(appInsights.id, '2020-02-02').ConnectionString
        }
        {
          name: 'Storage:ConnectionString'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=${listKeys(storageAccount.id, '2021-08-01').keys[0].value}'
        }
        {
          name: 'ApplicationInsights:LogLevel:Default'
          value: 'Information'
        }
      ]
      cors: {
        supportCredentials: true
        allowedOrigins: [
          'https://${frontDoorDomain}'
        ]
      }
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      netFrameworkVersion: 'v6.0'
      scmMinTlsVersion: '1.2'
      use32BitWorkerProcess: false
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

resource frontDoor 'Microsoft.Network/frontDoors@2020-05-01' = if (useFrontDoor) {
  name: frontDoorName
  location: 'Global'
  properties: {
    backendPools: [
      {
        name: 'backendPool1'
        properties: {
          backends: [
            {
              address: appDomain
              backendHostHeader: appDomain
              httpPort: 80
              httpsPort: 443
              priority: 1
              weight: 50
              enabledState: 'Enabled'
            }
          ]
          healthProbeSettings: {
            id: resourceId('Microsoft.Network/frontDoors/healthProbeSettings', frontDoorName, 'healthProbeSettings1')
          }
          loadBalancingSettings: {
            id: resourceId('Microsoft.Network/frontDoors/loadBalancingSettings', frontDoorName, 'loadBalancingSettings1')
          }
        }
      }
    ]
    healthProbeSettings: [
      {
        name: 'healthProbeSettings1'
        properties: {
          intervalInSeconds: 255
          path: '/health'
          protocol: 'Https'
        }
      }
    ]
    frontendEndpoints: [
      {
        name: 'frontendEndpoint1'
        properties: {
          hostName: frontDoorDomain
          sessionAffinityEnabledState: 'Disabled'
        }
      }
    ]
    loadBalancingSettings: [
      {
        name: 'loadBalancingSettings1'
        properties: {
          additionalLatencyMilliseconds: 0
          sampleSize: 4
          successfulSamplesRequired: 2
        }
      }
    ]
    routingRules: [
      {
        name: 'routingRule1'
        properties: {
          frontendEndpoints: [
            {
              id: resourceId('Microsoft.Network/frontDoors/frontendEndpoints', frontDoorName, 'frontendEndpoint1')
            }
          ]
          acceptedProtocols: [
            'Https'
          ]
          patternsToMatch: [
            '/*'
          ]
          routeConfiguration: {
            '@odata.type': '#Microsoft.Azure.FrontDoor.Models.FrontdoorForwardingConfiguration'
            forwardingProtocol: 'HttpsOnly'
            backendPool: {
              id: resourceId('Microsoft.Network/frontDoors/backendPools', frontDoorName, 'backendPool1')
            }
          }
          enabledState: 'Enabled'
        }
      }
    ]
    enabledState: 'Enabled'
    friendlyName: frontDoorName
  }
  dependsOn: [
    app
  ]
}

output appClientId string = appClientId
output appDomain string = (useFrontDoor ? frontDoorDomain : 'Please create a custom domain name for ${appDomain} and use that in the manifest')
