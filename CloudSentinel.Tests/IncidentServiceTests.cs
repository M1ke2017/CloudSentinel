using System;
using System.Collections.Generic;
using System.Text;
using CloudSentinel.Core.Services;

namespace CloudSentinel.Tests
{
    public class IncidentServiceTests
    {
        [Fact]
        public void Create_ShouldReturnIncidentWithCorrectData()
        {
            var service = new IncidentService();
            var incident = service.Create("demo-service", "Unhealthy", "Traffic Rollback");
            Assert.NotEqual(Guid.Empty, incident.Id);
            Assert.Equal("demo-service", incident.ServiceName);
            Assert.Equal("Unhealthy", incident.Status);
            Assert.Equal("Traffic Rollback", incident.Action);
            Assert.True(incident.CreatedAt <= DateTime.UtcNow);
        }
    }
}
