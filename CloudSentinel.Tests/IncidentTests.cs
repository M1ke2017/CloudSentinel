using CloudSentinel.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace CloudSentinel.Tests
{
    public class IncidentTests
    {
        [Fact]
        public void NewIncident_ShouldGaveDefaultValue()
        {
            var incident = new Incident();

            Assert.NotEqual(Guid.Empty, incident.Id);
            Assert.True(incident.CreatedAt <= DateTime.UtcNow);
            Assert.Equal(string.Empty, incident.ServiceName);
            Assert.Equal(string.Empty, incident.Status);
            Assert.Equal(string.Empty, incident.Action);

        }
    }
}
