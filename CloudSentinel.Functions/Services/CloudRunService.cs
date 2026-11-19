using System;
using System.Collections.Generic;
using System.Text;
using Google.Cloud.Run.V2;
using Google.Protobuf.WellKnownTypes;

namespace CloudSentinel.Functions.Services
{
    public class CloudRunService
    {
        public async Task<List<string>> GetServicesAsync(string projectId, string region)
        {
           var client = await ServicesClient.CreateAsync();
           var parent = $"projects/{projectId}/locations/{region}";
           var services = new List<string>();
           await foreach(var service in client.ListServicesAsync(parent))
           { services.Add(service.Name); } return services;
        }
        public async Task<Service> GetServiceAsync(string projectId, string region, string serviceName)
        {
            var client = await ServicesClient.CreateAsync();
            var name = $"projects/{projectId}/locations/{region}/services/{serviceName}";
            return await client.GetServiceAsync(name);
        }
        public async Task<string> GetServiceStatusAsync(string projectId, string region, string serviceName)
        {
            var service = await GetServiceAsync(projectId, region, serviceName);
            if (service.Reconciling) return "Updating";
            if (service.TerminalCondition?.State == Condition.Types.State.ConditionSucceeded) return "Ready";
            return "NotReady";
        }
        public async Task<(string LatestCreated, string LatestReady)> GetRevisionsAsync(string projectId, string region, string serviceName)
        {
            var service = await GetServiceAsync(projectId, region, serviceName);
            return(service.LatestCreatedRevision, service.LatestReadyRevision);
        }
        public async Task<List<string>> GetAllRevisionsAsync(string projectId, string region, string serviceName)
        {
            var client = await RevisionsClient.CreateAsync();
            var parent = $"projects/{projectId}/locations/{region}/services/{serviceName}";
            var revisions = new List<string>();
            await foreach(var revision in client.ListRevisionsAsync(parent))
            { revisions.Add(revision.Name); } return revisions;
        }
        public async System.Threading.Tasks.Task UpdateTrafficAsync(string projectId, string region, string serviceName, TrafficTarget target)
        {
            var client = await ServicesClient.CreateAsync();
            var name = $"projects/{projectId}/locations/{region}/services/{serviceName}";
            var service = await client.GetServiceAsync(name);
            service.Traffic.Clear();
            service.Traffic.Add(target);
            var mask = new FieldMask();
            mask.Paths.Add("traffic");
            var operation = await client.UpdateServiceAsync(service, mask);
            var completed = await operation.PollUntilCompletedAsync();

            var updatedService = completed.Result;

            if (updatedService.TrafficStatuses.Count != 1 ||
                updatedService.TrafficStatuses[0].Revision != target.Revision ||
                updatedService.TrafficStatuses[0].Percent != 100)
            { throw new InvalidOperationException("Traffic Rollback nie został potwierdzony."); }
        }
        public async Task<Revision> GetRevisionAsync(string revisionName)
        {
            var client = await RevisionsClient.CreateAsync();
            return await client.GetRevisionAsync(revisionName);
        }
        public async Task<bool> IsRevisionReadyAsync(string revisionName)
        {
            var revision = await GetRevisionAsync(revisionName);
            return !revision.Reconciling && revision.Conditions.Any(c => c.Type == "Ready" && c.State == Condition.Types.State.ConditionSucceeded);
        }
        public async Task<string?> GetTaggedRevisionUrlAsync(string projectId, string region, string serviceName, string revisionName)
        {
            var service = await GetServiceAsync(projectId, region, serviceName);
            return service.TrafficStatuses.FirstOrDefault(x => x.Revision == revisionName && !string.IsNullOrWhiteSpace(x.Tag))?.Uri;
        }

    }
}
