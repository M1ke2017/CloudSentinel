using CloudSentinel.Functions.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace CloudSentinel.Tests
{
    public class RollbackAttemptServiceTests
    {
        [Fact]
        public void RecordAttempt_IncreasesCounter()
        {
            var service = new RollbackAttemptService();

            Assert.Equal(0, service.GetAttemptsLastHour());

            service.RecordAttempt();
            service.RecordAttempt();

            Assert.Equal(2, service.GetAttemptsLastHour());
        }

        [Fact]
        public void NewInstance_HasNoAttempts()
        {
            var service = new RollbackAttemptService();

            Assert.Equal(0, service.GetAttemptsLastHour());
        }
        [Fact]
        public void CanRollback_AfterThreeAttempts_ReturnsFalse()
        {
            var attempts = new RollbackAttemptService();
            var rollback = new RollbackService();

            attempts.RecordAttempt();
            attempts.RecordAttempt();
            attempts.RecordAttempt();

            var result = rollback.CanRollback(
                revisionReady: true,
                healthCheckPassed: true,
                attemptsLastHour: attempts.GetAttemptsLastHour());

            Assert.False(result);
        }
        [Fact]
        public void TryRecordAttempt_BlocksFourthAttempt()
        {
            var service = new RollbackAttemptService();
            Assert.True(service.TryRecordAttempt());
            Assert.True(service.TryRecordAttempt());
            Assert.True(service.TryRecordAttempt());
            Assert.False(service.TryRecordAttempt());
            Assert.Equal(3, service.GetAttemptsLastHour());
        }
    }
}
