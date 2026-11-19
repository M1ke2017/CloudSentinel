
using CloudSentinel.Core.Models;
using CloudSentinel.Functions.Models;
using Google.Cloud.Firestore;

namespace CloudSentinel.Functions.Services;

public class FirestoreIncidentService(FirestoreDb database)
{
    public async System.Threading.Tasks.Task SaveAsync(FirestoreIncident incident)
    {
        var document = database.Collection("incidents").Document(incident.Id);
        await document.SetAsync(incident);
    }

    public async Task<List<FirestoreIncident>> GetAllAsync()
    {
        var snapshot = await database.Collection("incidents").OrderByDescending("CreatedAt").GetSnapshotAsync();
        return snapshot.Documents.Select(document => document.ConvertTo<FirestoreIncident>()).ToList();
    }

    public async System.Threading.Tasks.Task SaveAsync(Incident incident)
    {
        var firestoreIncident = new FirestoreIncident
        {
            Id = incident.Id.ToString(),
            ServiceName = incident.ServiceName,
            Status = incident.Status,
            Action = incident.Action,
            CreatedAt = Timestamp.FromDateTime(incident.CreatedAt.ToUniversalTime())
        };

        await SaveAsync(firestoreIncident);
    }
}
