using System.Text.Json;
using CloudSentinel.Core.Models;
using CloudSentinel.Core.Services;
using Google.Cloud.Functions.Framework;
using Microsoft.AspNetCore.Http;
using Google.Apis.Auth.OAuth2;
using System.Net.Http.Headers;

namespace CloudSentinel.Functions;

public class MonitoringFunction : IHttpFunction
{
    public async Task HandleAsync(HttpContext context)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        var json = await File.ReadAllTextAsync(path);
        using var document = JsonDocument.Parse(json);
        var services = document.RootElement.GetProperty("MonitoredServices").Deserialize<List<MonitoredService>>() ?? [];
        var healthUrl = Environment.GetEnvironmentVariable("MONITORED_HEALTH_URL");
        if (!string.IsNullOrWhiteSpace(healthUrl))
        { foreach (var service in services) { service.HealthUrl = healthUrl; } }
        using var client = new HttpClient
        { Timeout = TimeSpan.FromSeconds(10) };
        var projectId = Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT");
        var cloudRunHealthUrl = Environment.GetEnvironmentVariable("MONITORED_HEALTH_URL");
        if (!string.IsNullOrWhiteSpace(projectId) && !string.IsNullOrWhiteSpace(cloudRunHealthUrl))
        {
            var audience = new Uri(cloudRunHealthUrl).GetLeftPart(UriPartial.Authority);
            var credential = await GoogleCredential.GetApplicationDefaultAsync();
            var oidcToken = await credential.GetOidcTokenAsync(OidcTokenOptions.FromTargetAudience(audience));
            var token = await oidcToken.GetAccessTokenAsync();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var repository = new IncidentRepository();
        var webhookUrl = Environment.GetEnvironmentVariable("WEBHOOK_URL");
        var webhook = new WebhookService(client);

        var runner = new MonitoringRunner(new MonitoringService(new HealthCheckService(client)), new IncidentService(), repository, webhook);
        foreach (var service in services)
        { await runner.RunAsync(service, webhookUrl); }

        var remediation = document.RootElement.GetProperty("Remediation");

        var enabledSetting = Environment.GetEnvironmentVariable("REMEDIATION_ENABLED");

        var remediationEnabled = bool.TryParse(enabledSetting, out var enabled) ? enabled : remediation.GetProperty("Enabled").GetBoolean();

        var verifiedRevision = Environment.GetEnvironmentVariable("VERIFIED_REVISION") ?? remediation.GetProperty("VerifiedRevision").GetString();



        if (remediationEnabled && !string.IsNullOrWhiteSpace(verifiedRevision))
        {
            var projectIdForRollback = Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT");
            if (string.IsNullOrWhiteSpace(projectIdForRollback)) throw new InvalidOperationException("Brak konfiguracji projektu GCP.");
            var cloudRun = new Services.CloudRunService();
            var rollback = new Services.RollbackService();
            var database = Google.Cloud.Firestore.FirestoreDb.Create(projectIdForRollback);
            var attempts = new Services.FirestoreRollbackAttemptService(database);

            var healthCheck = new HealthCheckService(client);

            foreach (var service in services)
            {
                if (!repository.GetAll().Any(x => x.ServiceName == service.Name && x.Status == "Unhealthy")) continue;
                if (string.IsNullOrWhiteSpace(service.ProjectId) || string.IsNullOrWhiteSpace(service.Region)) continue;

                var rollbackServiceName = Environment.GetEnvironmentVariable("ROLLBACK_SERVICE_NAME");

                if (string.IsNullOrWhiteSpace(rollbackServiceName)) continue;

                var cloudRunService = new MonitoredService
                {
                    Name = rollbackServiceName,
                    ProjectId = projectIdForRollback,
                    Region = service.Region,
                    HealthUrl = service.HealthUrl
                };

                var rollbackCompleted = await rollback.TryRollbackAsync(cloudRun, attempts, healthCheck, cloudRunService, verifiedRevision);

                if (rollbackCompleted)
                {
                    var incident = repository.GetAll().FirstOrDefault(x => x.ServiceName == service.Name &&x.Status == "Unhealthy");
                    if (incident is not null)incident.Action = "Traffic Rollback";
                }

            }
        }


        if (!string.IsNullOrWhiteSpace(projectId))
        {
            var monitoring = new Services.CloudMonitoringService();

            foreach (var service in services)
            {
                try
                {
                    var requests = await monitoring.GetRequestCountAsync(projectId, service.Name);
                    Console.WriteLine($"{service.Name}: {requests} żądań w ciągu 5 minut");
                }
                catch (Exception ex)
                { Console.WriteLine($"Cloud Monitoring ({service.Name}): {ex.Message}"); }
            }
        }


        if (!string.IsNullOrWhiteSpace(projectId))
        {
            var database = Google.Cloud.Firestore.FirestoreDb.Create(projectId);
            var firestore = new Services.FirestoreIncidentService(database);
            foreach (var incident in repository.GetAll())
            { await firestore.SaveAsync(incident); }
        }
        context.Response.StatusCode = 200;
        await context.Response.WriteAsync($"Kontrola zakończona. Incydenty: {repository.GetAll().Count}");
    }
}
