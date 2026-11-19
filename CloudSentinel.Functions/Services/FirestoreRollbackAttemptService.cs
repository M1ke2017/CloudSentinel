using System;
using System.Collections.Generic;
using System.Text;
using CloudSentinel.Functions.Models;
using Google.Cloud.Firestore;


namespace CloudSentinel.Functions.Services
{
    public class FirestoreRollbackAttemptService(FirestoreDb database)
    {
        public async System.Threading.Tasks.Task RecordAttemptAsync(string serviceName)
        {
            var attempt = new FirestoreRollbackAttempt
            {
                ServiceName = serviceName,
                CreatedAt = Timestamp.GetCurrentTimestamp()
            };
            await database.Collection("rollbackAttempts").AddAsync(attempt);
        }
        public async Task<int> GetAttemptsLastHourAsync(string serviceName)
        {
            var catoff = Timestamp.FromDateTime(DateTime.UtcNow.AddHours(-1));
            var snapshot = await database.Collection("rollbackAttempts")
                .WhereEqualTo("ServiceName", serviceName)
                .WhereGreaterThan("CreatedAt", catoff)
                .GetSnapshotAsync();
            return snapshot.Count;
        }

        public async System.Threading.Tasks.Task<bool> TryRecordAttemptAsync(
    string serviceName)
        {
            return await database.RunTransactionAsync(async transaction =>
            {
                var cutoff = Timestamp.FromDateTime(DateTime.UtcNow.AddHours(-1));

                var query = database.Collection("rollbackAttempts").WhereEqualTo("ServiceName", serviceName).WhereGreaterThan("CreatedAt", cutoff);

                var snapshot = await transaction.GetSnapshotAsync(query);

                if (snapshot.Count >= 3)return false;

                var attempt = new FirestoreRollbackAttempt
                {
                    ServiceName = serviceName,
                    CreatedAt = Timestamp.GetCurrentTimestamp()
                };

                var document = database.Collection("rollbackAttempts").Document();
                transaction.Create(document, attempt);

                return true;
            });
        }

    }
}
