using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using CloudSentinel.Core.Services;

namespace CloudSentinel.Tests
{
    public class WebhookServiceTests
    {
        [Theory]
        [InlineData(HttpStatusCode.OK, true)]
        [InlineData(HttpStatusCode.BadRequest, false)]
        [InlineData(HttpStatusCode.InternalServerError, false)]
        public async Task SendAsync_ReturnsExpectedResult(HttpStatusCode statusCode, bool expected)
        {
            using var clinet = new HttpClient(new FakeHttpHandler(statusCode));
            var services = new WebhookService(clinet);
            var result = await services.SendAsync("https://example.com/webhook", "CloudSentinel: wykryto awarię");
            Assert.Equal(expected, result);
        }

        private sealed class FakeHttpHandler(HttpStatusCode statusCode) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            { return Task.FromResult(new HttpResponseMessage(statusCode)); }
        }
    }
}