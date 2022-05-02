// <copyright file="Program.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

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

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IConfidentialClientApplication>(confidentialClientApp);
builder.Services.AddDLLookupAuthentication(builder.Configuration);
builder.Services.AddSingleton<TokenAcquisitionHelper>();
builder.Services.AddSession();
builder.Services.AddMvc().AddSessionStateTempDataProvider();
builder.Services.AddApplicationInsightsTelemetry(builder.Configuration["ApplicationInsights:InstrumentationKey"]);

builder.Services.Configure<StorageOptions>(options =>
{
    options.ConnectionString = builder.Configuration.GetValue<string>("Storage:ConnectionString");
});

builder.Services.Configure<Microsoft.Teams.Apps.DLLookup.Models.CacheOptions>(options =>
{
    options.CacheInterval = builder.Configuration.GetValue<int>("CacheInterval");
});

builder.Services.Configure<AzureAdOptions>(options =>
{
    options.ClientId = builder.Configuration.GetValue<string>("AzureAd:ClientId");
    options.ClientSecret = builder.Configuration.GetValue<string>("AzureAd:ClientSecret");
    options.GraphScope = builder.Configuration.GetValue<string>("AzureAd:GraphScope");
    options.TenantId = builder.Configuration.GetValue<string>("AzureAd:TenantId");
});

builder.Services.AddRepositories();
builder.Services.AddHttpClient();

var app = builder.Build();

app.UseSession();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.MapFallbackToFile("index.html");

app.Run();