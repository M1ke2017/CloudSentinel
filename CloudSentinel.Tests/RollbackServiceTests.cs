using CloudSentinel.Core.Services;
using CloudSentinel.Functions.Services;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;


namespace CloudSentinel.Tests
{
    public class RollbackServiceTests
    {
        [Theory]
        [InlineData("revision-2", "revision-1", "revision-1")]
        [InlineData("revision-2", "revision-2", null)]
        [InlineData("revision-2", "revision-3", null)]
        [InlineData("revision-2", "", null)]
        public void SelectRevision_ReturnsExpectedResult(string currentRevision, string verifiedRevision, string? expected) 
        {
            var service = new RollbackService();
            var revisions = new List<string> { "revision-1", "revision-2" };
            var result = service.SelectRevision(revisions, currentRevision, verifiedRevision);
            Assert.Equal(expected, result);
        }
        [Fact]
        public void PrepareTrafficTarget_ValidRevision_Returns100Percent()
        {
            var service = new RollbackService();
            var revisions = new List<string> { "revision-1", "revision-2" };
            var result = service.PrepareTrafficTarget(revisions, "revision-2", "revision-1");
            Assert.NotNull(result);
            Assert.Equal("revision-1", result.Revision);
            Assert.Equal(100, result.Percent);
        }

        [Theory]
        [InlineData("revision-2", "revision-2")]
        [InlineData("revision-2", "revision-3")]
        [InlineData("revision-2", "")]
        public void PrepareTrafficTarget_InvalidRevision_ReturnsNull(string currentRevision, string verifiedRevision)
        {
            var service = new RollbackService();
            var revisions = new List<string> { "revision-1", "revision-2" };
            var results = service.PrepareTrafficTarget(revisions, currentRevision, verifiedRevision);
            Assert.Null(results);
        }

        [Theory]
        [InlineData(true, true, 0, true)]
        [InlineData(true, true, 2, true)]
        [InlineData(true, true, 3, false)]
        [InlineData(false, true, 0, false)]
        [InlineData(true, false, 0, false)]
        [InlineData(false, false, 0, false)]
        [InlineData(true, true, -1, false)]
        public void CanRollback_ReturnsExpectedResult(bool revisionReady, bool healthCheckPassed, int attemptsLastHour, bool expected)
        {
            var service = new RollbackService();
            var result = service.CanRollback(revisionReady, healthCheckPassed, attemptsLastHour);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(true, true, 0, true)]
        [InlineData(false, true, 0, false)]
        [InlineData(true, false, 0, false)]
        [InlineData(true, true, 3, false)]
        public void PrepareRollback_ReturnsExpectedTarget(bool revisionReady, bool healthCheckPassed, int attemptsLastHour, bool expected)
        {
            var service = new RollbackService();
            var result = service.PrepareRollback(
            ["revision-1", "revision-2"], "revision-2", "revision-1", revisionReady, healthCheckPassed, attemptsLastHour);
            Assert.Equal(expected, result is not null);
            if(expected) 
            { 
              Assert.Equal("revision-1", result!.Revision); 
              Assert.Equal(100, result.Percent); 
            }
        }

        [Fact]
        public async Task CheckRevisionHealthAsync_NoUrl_ReturnsFasle()
        {
            var rollback = new RollbackService();
            using var client = new HttpClient();
            var healthCheck = new HealthCheckService(client);
            var result = await rollback.CheckRevisionHealthAsync(healthCheck, null);
            Assert.False(result);
        }

        [Theory]
        [InlineData(HttpStatusCode.OK, true)]
        [InlineData(HttpStatusCode.ServiceUnavailable, false)]
        public async Task CheckRevisionHealthAsync_ReturnsExpectedResult(HttpStatusCode statusCode, bool expected)
        {
            using var client = new HttpClient(new TestHandler(statusCode));
            var healthCheck = new HealthCheckService(client);
            var rollback = new RollbackService();
            var result = await rollback.CheckRevisionHealthAsync(healthCheck, "https://revision.example.com");
            Assert.Equal(expected, result);
        }
        private sealed class TestHandler(HttpStatusCode statusCode) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Assert.Equal("https://revision.example.com/health", request.RequestUri?.ToString());
                return Task.FromResult(new HttpResponseMessage(statusCode));
            }
        }
    }
}
