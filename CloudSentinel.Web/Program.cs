using CloudSentinel.Core.Models;
using CloudSentinel.Core.Services;
using CloudSentinel.Web.Components;
using Google.Cloud.Firestore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSingleton<IncidentRepository>();
builder.Services.AddHttpClient<HealthCheckService>();
builder.Services.AddScoped<MonitoringService>();
builder.Services.AddScoped<IncidentService>();
builder.Services.AddScoped<MonitoringRunner>();
builder.Services.AddHealthChecks();
builder.Services.AddHttpClient<WebhookService>();
builder.Services.Configure<List<MonitoredService>>(builder.Configuration.GetSection("MonitoredServices"));

var projectId = builder.Configuration["GoogleCloud:ProjectId"];
if (!string.IsNullOrWhiteSpace(projectId)) { builder.Services.AddSingleton(FirestoreDb.Create(projectId)); }

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapHealthChecks("/health");
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
