using System;
using System.Collections.Generic;
using System.Text;
using CloudSentinel.Core.Models;

namespace CloudSentinel.Core.Services
{
    public class MonitoringService(HealthCheckService healthCheck)
    {
        public async Task<bool> CheckAsync(MonitoredService service)
        { return await healthCheck.CheckAsync(service.HealthUrl); }
    }
}
