using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using CloudSentinel.Core.Models;
using CloudSentinel.Core.Services;

namespace CloudSentinel.Tests
{
    public class MonitoringServiceTests
    {
        [Theory]
        [InlineData(HttpStatusCode.OK, true)]
        [InlineData(HttpStatusCode.ServiceUnavailable, false)]
        public async Task CheckAsync_ReturnsExpectedHealth(HttpStatusCode statusCode, bool expected)
        {
            using var client = new HttpClient(new FakeHttpHandler(statusCode));
            var healthCheck = new HealthCheckService(client);
            var monitoring = new MonitoringService(healthCheck);
            var service = new MonitoredService
            { Name = "demo-service", HealthUrl = "https://example.com/health" };
            var result = await monitoring.CheckAsync(service);
            Assert.Equal(expected, result);
        }
        private sealed class FakeHttpHandler(HttpStatusCode statusCode) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            { return Task.FromResult(new HttpResponseMessage(statusCode)); }
        }
    }
}
