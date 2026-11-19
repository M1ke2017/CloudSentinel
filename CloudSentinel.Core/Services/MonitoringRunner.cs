using System;
using System.Collections.Generic;
using System.Text;
using CloudSentinel.Core.Models;

namespace CloudSentinel.Core.Services
{
    public class MonitoringRunner(MonitoringService monitoring, IncidentService incidentService, IncidentRepository repository, WebhookService? webhook = null)
    {
        public async Task RunAsync(MonitoredService service, string? webhookUrl = null)
        {
            bool isHealthy = await monitoring.CheckAsync(service);
            if (isHealthy) return;
            var incident = incidentService.Create(service.Name, "Unhealthy", "None");
            repository.Add(incident);

            if (webhook is not null && !string.IsNullOrWhiteSpace(webhookUrl))
            { await webhook.SendAsync(webhookUrl, $"CloudSentinel: awaria usługi {service.Name}"); }
        }
    }
}