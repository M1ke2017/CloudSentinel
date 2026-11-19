using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using CloudSentinel.Core.Models;
using CloudSentinel.Core.Services;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Google.Cloud.Functions.Framework;
using Microsoft.AspNetCore.Http;

namespace CloudSentinel.Functions;

public class MonitoringFunction : IHttpFunction
{
    public async Task HandleAsync(HttpContext context)
    {
        var started = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        var runId = Guid.TryParse(context.Request.Headers["X-CloudSentinel-RunId"].ToString(), out var supplied)
            ? supplied.ToString("D") : Guid.NewGuid().ToString("D");
        var trigger = context.Request.Headers["X-CloudSentinel-Source"].ToString() == "Dashboard"
            ? "Dashboard" : "Cloud Scheduler";
        var stage = "Start";
        var outcome = "Failed";
        var error = "";
        var serviceStatuses = new List<string>();
        var incidentIds = new List<string>();
        var steps = new List<string>();
        var responseText = "";
        var projectId = Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT");
        FirestoreDb? database = null;

        try
        {
            stage = "Konfiguracja";
            var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            var json = await File.ReadAllTextAsync(path);
            using var document = JsonDocument.Parse(json);
            var services = document.RootElement.GetProperty("MonitoredServices")
                .Deserialize<List<MonitoredService>>() ?? [];
            var cloudRunHealthUrl = Environment.GetEnvironmentVariable("MONITORED_HEALTH_URL");
            if (!string.IsNullOrWhiteSpace(cloudRunHealthUrl))
                foreach (var service in services) service.HealthUrl = cloudRunHealthUrl;

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            if (!string.IsNullOrWhiteSpace(projectId) && !string.IsNullOrWhiteSpace(cloudRunHealthUrl))
            {
                var audience = new Uri(cloudRunHealthUrl).GetLeftPart(UriPartial.Authority);
                var credential = await GoogleCredential.GetApplicationDefaultAsync();
                var oidcToken = await credential.GetOidcTokenAsync(OidcTokenOptions.FromTargetAudience(audience));
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", await oidcToken.GetAccessTokenAsync());
            }

            var repository = new IncidentRepository();
            var webhookUrl = Environment.GetEnvironmentVariable("WEBHOOK_URL");
            var webhook = new WebhookService(client);
            var runner = new MonitoringRunner(new MonitoringService(new HealthCheckService(client)),
                new IncidentService(), repository, webhook);

            stage = "Health check";
            steps.Add($"Start: {started:O}");
            foreach (var service in services)
            {
                // MonitoringRunner records an incident for an unhealthy service.
                var before = repository.GetAll().Count;
                await runner.RunAsync(service, webhookUrl);
                var unhealthy = repository.GetAll().Count > before;
                serviceStatuses.Add($"{service.Name}: {(unhealthy ? "Unhealthy" : "Healthy")}");
                steps.Add($"{service.Name}: {(unhealthy ? "Unhealthy" : "Healthy")}");
            }

            var remediation = document.RootElement.GetProperty("Remediation");
            var enabledSetting = Environment.GetEnvironmentVariable("REMEDIATION_ENABLED");
            var remediationEnabled = bool.TryParse(enabledSetting, out var enabled)
                ? enabled : remediation.GetProperty("Enabled").GetBoolean();
            var verifiedRevision = Environment.GetEnvironmentVariable("VERIFIED_REVISION")
                ?? remediation.GetProperty("VerifiedRevision").GetString();

            if (remediationEnabled && !string.IsNullOrWhiteSpace(verifiedRevision))
            {
                stage = "Remediation";
                if (string.IsNullOrWhiteSpace(projectId))
                    throw new InvalidOperationException("Brak konfiguracji projektu GCP.");
                var rollbackServiceName = Environment.GetEnvironmentVariable("ROLLBACK_SERVICE_NAME");
                if (!string.IsNullOrWhiteSpace(rollbackServiceName))
                {
                    var cloudRun = new Services.CloudRunService();
                    var rollback = new Services.RollbackService();
                    database ??= FirestoreDb.Create(projectId);
                    var attempts = new Services.FirestoreRollbackAttemptService(database);
                    var healthCheck = new HealthCheckService(client);
                    foreach (var service in services)
                    {
                        if (!repository.GetAll().Any(x => x.ServiceName == service.Name && x.Status == "Unhealthy")) continue;
                        if (string.IsNullOrWhiteSpace(service.Region)) continue;
                        var cloudService = new MonitoredService
                        {
                            Name = rollbackServiceName,
                            ProjectId = projectId,
                            Region = service.Region,
                            HealthUrl = service.HealthUrl
                        };
                        var completed = await rollback.TryRollbackAsync(cloudRun, attempts, healthCheck,
                            cloudService, verifiedRevision);
                        if (completed)
                        {
                            var incident = repository.GetAll().FirstOrDefault(x => x.ServiceName == service.Name && x.Status == "Unhealthy");
                            if (incident is not null) incident.Action = "Traffic Rollback";
                            steps.Add($"Rollback: {rollbackServiceName} → {verifiedRevision}");
                        }
                        else steps.Add($"Rollback: nie wykonano dla {rollbackServiceName}");
                    }
                }
            }
            else steps.Add("Remediation: wyłączona");

            if (!string.IsNullOrWhiteSpace(projectId))
            {
                stage = "Cloud Monitoring";
                var monitoring = new Services.CloudMonitoringService();
                foreach (var service in services)
                {
                    try
                    {
                        var requests = await monitoring.GetRequestCountAsync(projectId, service.Name);
                        Console.WriteLine($"{service.Name}: {requests} żądań w ciągu 5 minut");
                    }
                    catch (Exception ex)
                    {
                        // Failure to read optional metrics does not invalidate a completed health check.
                        Console.WriteLine($"Cloud Monitoring ({service.Name}): {ex.Message}");
                    }
                }

                stage = "Firestore — incydenty";
                database ??= FirestoreDb.Create(projectId);
                var firestore = new Services.FirestoreIncidentService(database);
                foreach (var incident in repository.GetAll())
                {
                    await firestore.SaveAsync(incident);
                    incidentIds.Add(incident.Id.ToString());
                }
            }
            else if (repository.GetAll().Count > 0)
                steps.Add("Incydenty lokalne — projekt GCP nieustawiony");

            outcome = serviceStatuses.Any(s => s.EndsWith(": Unhealthy", StringComparison.Ordinal))
                ? "Unhealthy" : "Healthy";
            stage = "Zakończono";
            steps.Add($"Wynik: {outcome}; incydenty: {incidentIds.Count}");
            context.Response.StatusCode = 200;
            responseText = $"Kontrola zakończona. Incydenty: {repository.GetAll().Count}; RunId: {runId}";
        }
        catch (Exception ex)
        {
            error = ex.Message;
            steps.Add($"Błąd ({stage}): {error}");
            Console.Error.WriteLine($"CloudSentinel Run {runId}: {ex}");
            context.Response.StatusCode = 500;
            responseText = $"Kontrola nieudana. RunId: {runId}";
        }
        finally
        {
            stopwatch.Stop();
            if (!string.IsNullOrWhiteSpace(projectId))
            {
                try
                {
                    database ??= FirestoreDb.Create(projectId);
                    await database.Collection("monitoring_runs").Document(runId).SetAsync(new Dictionary<string, object>
                    {
                        ["RunId"] = runId,
                        ["Source"] = trigger,
                        ["StartedAt"] = Timestamp.FromDateTime(started),
                        ["FinishedAt"] = Timestamp.FromDateTime(DateTime.UtcNow),
                        ["DurationMs"] = stopwatch.ElapsedMilliseconds,
                        ["Result"] = outcome,
                        ["Stage"] = stage,
                        ["Error"] = error,
                        ["Services"] = serviceStatuses,
                        ["IncidentIds"] = incidentIds,
                        ["Steps"] = steps
                    });
                }
                catch (Exception saveError)
                {
                    Console.Error.WriteLine($"CloudSentinel: nie zapisano historii RunId={runId}: {saveError}");
                    context.Response.StatusCode = 500;
                    responseText = $"Nie udało się zapisać wyniku monitoringu. RunId: {runId}";
                }
            }
        }
        await context.Response.WriteAsync(responseText);
    }
}
