using System;
using System.Collections.Generic;
using System.Text;
using CloudSentinel.Functions;
using Microsoft.AspNetCore.Http;

namespace CloudSentinel.Tests
{
    public class MonitoringFunctionTests
    {
        [Fact]
        public async Task HandleAsync_ReturnsMonitoringResult()
        {
            var function = new MonitoringFunction();
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            await function.HandleAsync(context);
            context.Response.Body.Position = 0;
            using var reader = new StreamReader(context.Response.Body);
            var response = await reader.ReadToEndAsync();
            Assert.Equal(200, context.Response.StatusCode);
            Assert.StartsWith("Kontrola zakończona. Incydenty:", response);
        }
    }
}
