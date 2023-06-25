using Azure;
using Azure.Data.Tables;
using System.Runtime.Serialization;

namespace RiotOAuthCallback.Classes
{
    public record RedditFlairUser : ITableEntity
    {
        // Partition Key, ignore serializing
        [IgnoreDataMember]
        public string SubredditName
        {
            get
            {
                return PartitionKey;
            }

            set
            {
                PartitionKey = value;
            }
        }

        // Row Key, ignore serializing
        [IgnoreDataMember]
        public string UserName
        {
            get
            {
                return RowKey;
            }

            set
            {
                RowKey = value;
            }
        }

        public DateTimeOffset ExpiresOn { get; set; } = DateTimeOffset.UtcNow.AddMinutes(-15);  // This should be set, but if not, let the record be expired.

        public string State { get; set; } = string.Empty;

        //public string PostId { get; set; } = string.Empty;

        // ITableEntity properties
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
