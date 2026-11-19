using System;
using System.Collections.Generic;
using System.Text;
using Google.Cloud.Monitoring.V3;
using Google.Protobuf.WellKnownTypes;

namespace CloudSentinel.Functions.Services
{
    public class CloudMonitoringService
    {
        public async Task<int> GetRequestCountAsync(string projectId, string serviceName)
        {
            var client = await MetricServiceClient.CreateAsync();
            var end = DateTime.UtcNow;
            var start = end.AddMinutes(-5);
            var interval = new TimeInterval
            { StartTime = Timestamp.FromDateTime(start), EndTime = Timestamp.FromDateTime(end) };
            var filter = "metric.type=\"run.googleapis.com/request_count\" " + "AND resource.type=\"cloud_run_revision\" " + $"AND resource.labels.service_name=\"{serviceName}\"";
            var count = 0L;
            var series = client.ListTimeSeriesAsync($"projects/{projectId}", filter, interval, ListTimeSeriesRequest.Types.TimeSeriesView.Full);
            await foreach (var item in series)
            {
                foreach (var point in item.Points)
                { count += point.Value.Int64Value; }
            }

            return checked((int)count);
        }
    }
}
