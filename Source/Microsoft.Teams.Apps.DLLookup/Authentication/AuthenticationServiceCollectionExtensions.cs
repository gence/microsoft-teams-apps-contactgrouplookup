﻿// <copyright file="AuthenticationServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.DLLookup.Authentication
{
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Identity.Web;
    using Microsoft.IdentityModel.Tokens;

    /// <summary>
    /// Extension class for registering authentication services in DI container.
    /// </summary>
    public static class AuthenticationServiceCollectionExtensions
    {
        private const string ClientIdConfigurationSettingsKey = "AzureAd:ClientId";
        private const string TenantIdConfigurationSettingsKey = "AzureAd:TenantId";
        private const string ApplicationIdURIConfigurationSettingsKey = "AzureAd:ApplicationIdURI";
        private const string ValidIssuersConfigurationSettingsKey = "AzureAd:ValidIssuers";

        /// <summary>
        /// Extension method to register the authentication services.
        /// </summary>
        /// <param name="services">IServiceCollection instance.</param>
        /// <param name="configuration">IConfiguration instance.</param>
        public static void AddDLLookupAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            RegisterAuthenticationServices(services, configuration);
        }

        // This method works specifically for single tenant application.
        private static void RegisterAuthenticationServices(
            IServiceCollection services,
            IConfiguration configuration)
        {
            AuthenticationServiceCollectionExtensions.ValidateAuthenticationConfigurationSettings(configuration);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(configuration, "AzureAd", JwtBearerDefaults.AuthenticationScheme)
                .EnableTokenAcquisitionToCallDownstreamApi()
                .AddMicrosoftGraph(configuration.GetSection("DownstreamApi"))
                .AddInMemoryTokenCaches();

            services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidAudiences = AuthenticationServiceCollectionExtensions.GetValidAudiences(configuration),
                    ValidIssuers = AuthenticationServiceCollectionExtensions.GetValidIssuers(configuration),
                    AudienceValidator = AuthenticationServiceCollectionExtensions.AudienceValidator,
                };
            });
        }

        private static void ValidateAuthenticationConfigurationSettings(IConfiguration configuration)
        {
            var clientId = configuration[AuthenticationServiceCollectionExtensions.ClientIdConfigurationSettingsKey];
            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ApplicationException("AzureAD ClientId is missing in the configuration file.");
            }

            var tenantId = configuration[AuthenticationServiceCollectionExtensions.TenantIdConfigurationSettingsKey];
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                throw new ApplicationException("AzureAD TenantId is missing in the configuration file.");
            }

            var applicationIdURI = configuration[AuthenticationServiceCollectionExtensions.ApplicationIdURIConfigurationSettingsKey];
            if (string.IsNullOrWhiteSpace(applicationIdURI))
            {
                throw new ApplicationException("AzureAD ApplicationIdURI is missing in the configuration file.");
            }

            var validIssuers = configuration[AuthenticationServiceCollectionExtensions.ValidIssuersConfigurationSettingsKey];
            if (string.IsNullOrWhiteSpace(validIssuers))
            {
                throw new ApplicationException("AzureAD ValidIssuers is missing in the configuration file.");
            }
        }

        private static IEnumerable<string> GetSettings(IConfiguration configuration, string configurationSettingsKey)
        {
            var configurationSettingsValue = configuration[configurationSettingsKey];
            var settings = configurationSettingsValue
                ?.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                ?.Select(p => p.Trim());
            if (settings == null)
            {
                throw new ApplicationException($"{configurationSettingsKey} does not contain a valid value in the configuration file.");
            }

            return settings;
        }

        private static IEnumerable<string> GetValidAudiences(IConfiguration configuration)
        {
            var clientId = configuration[AuthenticationServiceCollectionExtensions.ClientIdConfigurationSettingsKey];

            var applicationIdURI = configuration[AuthenticationServiceCollectionExtensions.ApplicationIdURIConfigurationSettingsKey];

            var validAudiences = new List<string> { clientId, applicationIdURI.ToUpperInvariant() };

            return validAudiences;
        }

        private static IEnumerable<string> GetValidIssuers(IConfiguration configuration)
        {
            var tenantId = configuration[AuthenticationServiceCollectionExtensions.TenantIdConfigurationSettingsKey];

            var validIssuers =
                AuthenticationServiceCollectionExtensions.GetSettings(
                    configuration,
                    AuthenticationServiceCollectionExtensions.ValidIssuersConfigurationSettingsKey);

            validIssuers = validIssuers.Select(validIssuer => validIssuer.Replace("TENANT_ID", tenantId, StringComparison.OrdinalIgnoreCase));

            return validIssuers;
        }

        private static bool AudienceValidator(
            IEnumerable<string> tokenAudiences,
            SecurityToken securityToken,
            TokenValidationParameters validationParameters)
        {
            if (tokenAudiences == null || !tokenAudiences.Any())
            {
                throw new ApplicationException("No audience defined in token!");
            }

            var validAudiences = validationParameters.ValidAudiences;
            if (validAudiences == null || !validAudiences.Any())
            {
                throw new ApplicationException("No valid audiences defined in validationParameters!");
            }

            foreach (var tokenAudience in tokenAudiences)
            {
                if (validAudiences.Any(validAudience => validAudience.Equals(tokenAudience, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}