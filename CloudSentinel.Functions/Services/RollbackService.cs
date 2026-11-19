using Google.Cloud.Run.V2;
using System;
using System.Collections.Generic;
using System.Text;
using CloudSentinel.Core.Services;
using CloudSentinel.Core.Models;
using Google.Api;

namespace CloudSentinel.Functions.Services
{
    public class RollbackService 
    {
        public string? SelectRevision(List<string> revisions, string currentRevision,string verifiedRevision)
        {
            if (string.IsNullOrWhiteSpace(verifiedRevision)) return null;
            if (verifiedRevision == currentRevision) return null;
            return revisions.Contains(verifiedRevision) ? verifiedRevision : null;
        }
        public TrafficTarget? PrepareTrafficTarget(List<string> revisions, string currentRevision, string verifiedRevision)
        {
            var target = SelectRevision(revisions, currentRevision, verifiedRevision);
            if (target is null) return null;
            return new TrafficTarget { Revision = target, Percent = 100 };
        }
        public bool CanRollback(bool revisionReady, bool healthCheckPassed, int attemptsLastHour)
        { return revisionReady && healthCheckPassed && attemptsLastHour >= 0 && attemptsLastHour < 3; }


        public TrafficTarget? PrepareRollback(
            List<string> revisions,
            string currentRevision,
            string verifiedRevision,
            bool revisionReady,
            bool healthCheckPassed,
            int attemptsLastHour)
        {
            if (!CanRollback(revisionReady, healthCheckPassed, attemptsLastHour)) { return null; }
            return PrepareTrafficTarget(revisions, currentRevision, verifiedRevision);
        }
        public async Task<bool> CheckRevisionHealthAsync(HealthCheckService healthCheck, string? revisionUrl)
        {
            if (string.IsNullOrWhiteSpace(revisionUrl)) return false;
            var healthUrl = $"{revisionUrl.TrimEnd('/')}/health";
            return await healthCheck.CheckAsync(healthUrl);
        }
        public async Task<bool> ExecuteRollbackAsync(CloudRunService cloudRun, FirestoreRollbackAttemptService attempts, MonitoredService service, TrafficTarget target)
        {
            if (target is null || target.Percent != 100) return false;
            if (!await attempts.TryRecordAttemptAsync(service.Name)) return false;
            await cloudRun.UpdateTrafficAsync(service.ProjectId, service.Region, service.Name, target); return true;
        }
        public async System.Threading.Tasks.Task<bool> TryRollbackAsync(CloudRunService cloudRun, FirestoreRollbackAttemptService attempts, HealthCheckService healthCheck,
        MonitoredService service, string verifiedRevision)
        {
            var cloudService = await cloudRun.GetServiceAsync(service.ProjectId, service.Region, service.Name);
            if (cloudService.Reconciling) return false;

            var active = cloudService.TrafficStatuses.Where(x => x.Percent > 0).ToList();
            if (active.Count != 1 || active[0].Percent != 100)return false;
            var currentRevision = active[0].Revision;

            var revisions = await cloudRun.GetAllRevisionsAsync(service.ProjectId, service.Region, service.Name);

            var ready = await cloudRun.IsRevisionReadyAsync(verifiedRevision);

            var url = await cloudRun.GetTaggedRevisionUrlAsync(service.ProjectId, service.Region,service.Name, verifiedRevision);

            var healthy = await CheckRevisionHealthAsync(healthCheck, url);

            var target = PrepareRollback(revisions, currentRevision, verifiedRevision,ready, healthy, 0);

            if (target is null)return false;
            return await ExecuteRollbackAsync(cloudRun, attempts, service, target);
        }

    }
}
