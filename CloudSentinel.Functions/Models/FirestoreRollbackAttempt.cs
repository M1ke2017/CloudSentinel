using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Text;

namespace CloudSentinel.Functions.Models
{
    [FirestoreData]
    public class FirestoreRollbackAttempt
    {
        [FirestoreProperty]
        public string ServiceName { get; set; } = string.Empty;

        [FirestoreProperty]
        public Timestamp CreatedAt { get; set; }
    }
}
