using System;
using System.Collections.Generic;
using System.Text;
using CloudSentinel.Core.Models;
using CloudSentinel.Core.Services;

namespace CloudSentinel.Tests
{
    public class IncidentRepositoryTests
    {
        [Fact]
        public void Add_ShouldStoreIncident()
        {
            var repository = new IncidentRepository();
            var incident = new Incident
            { ServiceName = "demo-service", Status = "Unhealthy", Action = "Traffic Rollback" };
            repository.Add(incident);
            var result = repository.GetAll();
            Assert.Single(result);
            Assert.Equal(incident.Id, result[0].Id);
            Assert.Equal("demo-service", result[0].ServiceName);
        }
    }
}
