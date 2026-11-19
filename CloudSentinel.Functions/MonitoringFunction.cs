
using System.Text.Json;
using CloudSentinel.Core.Models;
using CloudSentinel.Core.Services;
using Google.Cloud.Functions.Framework;
using Microsoft.AspNetCore.Http;

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

        var repository = new IncidentRepository();
        var webhookUrl = Environment.GetEnvironmentVariable("WEBHOOK_URL");
        var webhook = new WebhookService(client);

        var runner = new MonitoringRunner(new MonitoringService(new HealthCheckService(client)), new IncidentService(), repository, webhook);
        foreach (var service in services)
        { await runner.RunAsync(service, webhookUrl); }

        var remediation = document.RootElement.GetProperty("Remediation");

        var remediationEnabled = remediation.GetProperty("Enabled").GetBoolean();

        var verifiedRevision = remediation.GetProperty("VerifiedRevision").GetString();


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
                var rollbackCompleted = await rollback.TryRollbackAsync(cloudRun,attempts, healthCheck, service, verifiedRevision);
                if (rollbackCompleted)
                {
                    var incident = repository.GetAll().FirstOrDefault(x => x.ServiceName == service.Name &&x.Status == "Unhealthy");
                    if (incident is not null)incident.Action = "Traffic Rollback";
                }

            }
        }

        var projectId = Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT");

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
