using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using CloudSentinel.Core.Models;
using CloudSentinel.Core.Services;

namespace CloudSentinel.Tests
{
    public class MonitoringRunnerTests
    {
        private sealed class FakeHttpHandler(
       HttpStatusCode statusCode) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            { return Task.FromResult(new HttpResponseMessage(statusCode)); }
        }


        [Fact]
        public async Task RunAsync_UnhealthyService_CreatesIncident()
        {
            using var client = new HttpClient(new FakeHttpHandler(HttpStatusCode.ServiceUnavailable));
            var monitoring = new MonitoringService(new HealthCheckService(client));
            var reposiotry = new IncidentRepository();
            var runner = new MonitoringRunner(monitoring, new IncidentService(), reposiotry);
            await runner.RunAsync(new MonitoredService { Name = "demo-service", HealthUrl= "https://example.com/health" });
            var incident = Assert.Single(reposiotry.GetAll());
            Assert.Equal("demo-service", incident.ServiceName);
            Assert.Equal("Unhealthy", incident.Status);
            Assert.Equal("None", incident.Action);
        }

        [Fact]
        public async Task RunAsync_HealthyService_DoesNotCreateIncident()
        {
            using var client = new HttpClient(new FakeHttpHandler(HttpStatusCode.OK));
            var repository = new IncidentRepository();
            var runner = new MonitoringRunner(new MonitoringService(new HealthCheckService(client)), new IncidentService(), repository);
            await runner.RunAsync(new MonitoredService { Name = "demo-service", HealthUrl = "https://example.com/health" });
            Assert.Empty(repository.GetAll());
        }
    }
}
