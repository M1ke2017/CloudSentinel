using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using CloudSentinel.Core.Models;
using CloudSentinel.Core.Services;
using Microsoft.AspNetCore.Http;

namespace CloudSentinel.Tests
{
    public class MonitoringWebhookTests
    {
        [Fact]
        public async Task RunAsync_UnhealthyService_SendsWebhook()
        {
            var webhookHandler = new FakeHandler(HttpStatusCode.OK);
            using var webhookClient = new HttpClient(webhookHandler);
            using var healthClient = new HttpClient(new FakeHandler(HttpStatusCode.ServiceUnavailable));

            var runner = new MonitoringRunner(new MonitoringService(new HealthCheckService(healthClient)),
            new IncidentService(),
            new IncidentRepository(),
            new WebhookService(webhookClient));
            
            await runner.RunAsync(new MonitoredService
            { Name = "demo-service", HealthUrl = "https://example.com/health"}, "https://example.com/webhook"); Assert.Equal(1, webhookHandler.RequestCount);
  
        }
        private sealed class FakeHandler(HttpStatusCode statusCode) : HttpMessageHandler
        {
            public int RequestCount { get; private set; }
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            { RequestCount++; return Task.FromResult(new HttpResponseMessage(statusCode)); }
        }
    }

}
