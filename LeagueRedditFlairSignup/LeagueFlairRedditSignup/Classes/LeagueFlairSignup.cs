using Azure;
using Azure.Data.Tables;
using System.Runtime.Serialization;

namespace LeagueFlairRedditSignup.Classes
{
    // Representation of the record saved to Azure Table Storage.
    internal record LeagueFlairSignup : ITableEntity
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

        public DateTimeOffset ExpiresOn { get; set; } = DateTimeOffset.UtcNow.AddDays(2);

        public string State { get; set; } = string.Empty;

        public string PostId { get; set; } = string.Empty;


        // ITableEntity properties.  These are part of Table Storage stuff and not directly related to the flair bot.
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
