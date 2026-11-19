using System;
using System.Net;
using System.Collections.Generic;
using System.Text;
using CloudSentinel.Core.Services;

namespace CloudSentinel.Tests
{
    public class HealthCheckServiceTests
    {
        [Theory]
        [InlineData(HttpStatusCode.OK, true)]
        [InlineData(HttpStatusCode.InternalServerError, false)]
        [InlineData(HttpStatusCode.NotFound, false)]
        public async Task CheckAsync_RetrunsExpectedResult(HttpStatusCode statusCode, bool expected)
        {
            var handler = new FakeHttpHandler(statusCode);
            using var client = new HttpClient(handler);

            var service = new HealthCheckService(client);
            var result = await service.CheckAsync("https://example.com/health");
            Assert.Equal(expected, result);
        }

        private sealed class FakeHttpHandler(HttpStatusCode statusCode) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            { return Task.FromResult(new HttpResponseMessage(statusCode)); }
        }
    }
}
