// <copyright file="Program.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.SnapshotCollector;
using Microsoft.Identity.Client;
using Microsoft.Teams.Apps.DLLookup.Authentication;
using Microsoft.Teams.Apps.DLLookup.Helpers;
using Microsoft.Teams.Apps.DLLookup.Helpers.Extentions;
using Microsoft.Teams.Apps.DLLookup.Models;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var scopes = builder.Configuration["AzureAd:GraphScope"].Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
IConfidentialClientApplication confidentialClientApp = ConfidentialClientApplicationBuilder.Create(builder.Configuration["AzureAd:ClientId"])
    .WithClientSecret(builder.Configuration["AzureAd:ClientSecret"])
    .Build();

builder.Services.AddSingleton<IConfidentialClientApplication>(confidentialClientApp);
builder.Services.AddDLLookupAuthentication(builder.Configuration);
builder.Services.AddSingleton<TokenAcquisitionHelper>();
builder.Services.AddMvc();
builder.Services.AddSession();
builder.Services.AddApplicationInsightsTelemetry(options: new ApplicationInsightsServiceOptions { ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"] });
builder.Services.AddSnapshotCollector((configuration) => builder.Configuration.Bind(nameof(SnapshotCollectorConfiguration), configuration));

builder.Services.Configure<StorageOptions>(options =>
{
    options.ConnectionString = builder.Configuration["Storage:ConnectionString"];
});

builder.Services.Configure<Microsoft.Teams.Apps.DLLookup.Models.CacheOptions>(options =>
{
    options.CacheInterval = builder.Configuration.GetValue<int>("CacheInterval");
});

builder.Services.Configure<AzureAdOptions>(options =>
{
    options.ClientId = builder.Configuration["AzureAd:ClientId"];
    options.ClientSecret = builder.Configuration["AzureAd:ClientSecret"];
    options.GraphScope = builder.Configuration["AzureAd:GraphScope"];
    options.TenantId = builder.Configuration["AzureAd:TenantId"];
});

builder.Services.AddRepositories();
builder.Services.AddHttpClient();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.MapFallbackToFile("index.html");

app.Run();
