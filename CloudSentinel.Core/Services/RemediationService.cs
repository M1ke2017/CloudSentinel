using System;
using System.Collections.Generic;
using System.Text;

namespace CloudSentinel.Core.Services
{
    public class RemediationService
    {
        public bool ShouldRemediate(bool isHealthy, int attemptsLastHour)
        { const int maxAttempsPerHour = 3; return !isHealthy && attemptsLastHour < maxAttempsPerHour; }
    }
}
