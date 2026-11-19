using CloudSentinel.Functions.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Google.Cloud.Firestore;

namespace CloudSentinel.Tests
{
    public class FirestoreIncidentTests
    {
        [Fact]
        public void FirestoreIncident_ShouldStoreData()
        {
            var createdAt = Timestamp.FromDateTime(DateTime.UtcNow);

            var incident = new FirestoreIncident
            {
                Id = Guid.NewGuid().ToString(),
                ServiceName = "demo-service",
                Status = "Unhealthy",
                Action = "Traffic Rollback",
                CreatedAt = createdAt
            };

            Assert.False(string.IsNullOrWhiteSpace(incident.Id));
            Assert.Equal("demo-service", incident.ServiceName);
            Assert.Equal("Unhealthy", incident.Status);
            Assert.Equal("Traffic Rollback", incident.Action);
            Assert.Equal(createdAt, incident.CreatedAt);
        }
    }
}
