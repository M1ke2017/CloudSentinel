using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Text;

namespace CloudSentinel.Functions.Models
{
    [FirestoreData]
    public class FirestoreIncident
    {
        [FirestoreProperty]
        public string Id { get; set; } = string.Empty;

        [FirestoreProperty]
        public string ServiceName { get; set; } = string.Empty;

        [FirestoreProperty]
        public string Status { get; set; } = string.Empty;

        [FirestoreProperty]
        public string Action { get; set; } = string.Empty;

        [FirestoreProperty]
        public Timestamp CreatedAt { get; set; }
    }
}
