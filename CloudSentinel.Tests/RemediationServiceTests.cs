using CloudSentinel.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace CloudSentinel.Tests
{
    public class RemediationServiceTests
    {
        [Theory]
        [InlineData(true, 0, false)]
        [InlineData(false, 0, true)]
        [InlineData(false, 2, true)]
        [InlineData(false, 3, false)]
        [InlineData(false, 4, false)]

        public void ShouldRemediate_ReturnsExpectedResult(bool isHealthy, int attemptsLastHour, bool expected)
        {
            var service = new RemediationService();
            var result = service.ShouldRemediate(isHealthy, attemptsLastHour);
            Assert.Equal(expected, result);
        }
    }
}
