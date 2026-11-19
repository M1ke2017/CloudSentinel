using System;
using System.Collections.Generic;
using System.Text;

namespace CloudSentinel.Functions.Services
{
    public class RollbackAttemptService
    {
        private readonly List<DateTime> _attempts = [];
        public int GetAttemptsLastHour()
        {
            var cutoff = DateTime.UtcNow.AddHours(-1);
            return _attempts.Count(x => x > cutoff);
        }
        public void RecordAttempt()
        { _attempts.Add(DateTime.UtcNow); }

        public bool TryRecordAttempt()
        {
            if(GetAttemptsLastHour() >= 3) return false;
            RecordAttempt(); return true;
        }
    }
}
