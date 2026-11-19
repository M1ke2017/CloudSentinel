using System;
using System.Collections.Generic;
using System.Text;

namespace CloudSentinel.Core.Models
{
    public class MonitoredService
    {
        public string Name { get; set; } = string.Empty;
        public string HealthUrl { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
    }
}
