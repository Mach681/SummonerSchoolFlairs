using Azure;
using Azure.Data.Tables;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace LeagueFlairUpdateJob.Classes
{
    internal record SummonerInfo : ITableEntity
    {
        [IgnoreDataMember]
        [JsonPropertyName("reddit_username")]
        public string Reddit_Username
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

        [IgnoreDataMember]
        [JsonPropertyName("flair_subreddit")]
        public string Flair_Subreddit
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

        [JsonPropertyName("signup_timestamp")]
        public DateTime Signup_Timestamp { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("last_updated")]
        public DateTime Last_Updated { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public string PartitionKey { get; set; } = string.Empty;

        [JsonIgnore]
        public string RowKey { get; set; } = string.Empty;

        [JsonIgnore]
        public DateTimeOffset? Timestamp { get; set; }

        [JsonIgnore]
        public ETag ETag { get; set; }
    }
}
