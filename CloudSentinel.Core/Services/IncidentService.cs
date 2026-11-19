using System;
using System.Collections.Generic;
using System.Text;
using CloudSentinel.Core.Models;


namespace CloudSentinel.Core.Services
{
    public class IncidentService
    {
        public Incident Create(string serviceName, string status, string action)
        { return new Incident { ServiceName = serviceName, Status = status, Action = action }; }
    }
}
