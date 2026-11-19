using CloudSentinel.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace CloudSentinel.Tests
{
    public class MonitoredServiceTests
    {
        [Fact]
        public void MonitoredService_ShouldStoreConfiguration()
        {
            var service = new MonitoredService
            {
                Name = "demo-service",
                HealthUrl = "https://example.com/health",
                ProjectId = "cloud-sentinel-demo",
                Region = "europe-west1"
            };

            Assert.Equal("demo-service", service.Name);
            Assert.Equal("https://example.com/health", service.HealthUrl);
            Assert.Equal("cloud-sentinel-demo", service.ProjectId);
            Assert.Equal("europe-west1", service.Region);
        }
    }
}
