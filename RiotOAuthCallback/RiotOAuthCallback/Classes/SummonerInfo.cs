using Azure;
using Azure.Data.Tables;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace RiotOAuthCallback.Classes
{
    public record SummonerInfo : ITableEntity
    {
        [JsonPropertyName("refresh_token")]
        public string Refresh_Token { get; set; } = string.Empty;

        [JsonPropertyName("id_token")]
        public string Id_Token { get; set; } = string.Empty;

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

        [JsonPropertyName("access_token")]
        public string Access_Token { get; set; } = string.Empty;

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
