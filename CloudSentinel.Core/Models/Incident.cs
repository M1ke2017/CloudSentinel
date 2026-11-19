using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace CloudSentinel.Core.Models
{
    public class Incident
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string ServiceName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }
}
