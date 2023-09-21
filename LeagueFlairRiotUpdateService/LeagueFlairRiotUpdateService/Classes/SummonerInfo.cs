using Azure;
using Azure.Data.Tables;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace LeagueFlairRiotUpdateService.Classes
{
    internal record SummonerInfo : ITableEntity
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

        [JsonPropertyName("league_rank")]
        public string League_Rank { get; set; } = string.Empty;

        [JsonPropertyName("summoner_id")]
        public string Summoner_Id { get; set; } = string.Empty;

        [JsonPropertyName("summoner_puuid")]
        public string Summoner_Puuid { get; set; } = string.Empty;

        [JsonPropertyName("access_token")]
        public string Access_Token { get; set; } = string.Empty;

        [JsonPropertyName("signup_timestamp")]
        public DateTime Signup_Timestamp { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("region")]
        public string Region { get; set; } = string.Empty;

        [JsonPropertyName("last_riot_updated")]
        public DateTime? Last_Riot_Updated { get; set; } // No default.  I want to set this by hand.

        [JsonPropertyName("summoner_Name")]
        public string Summoner_Name { get; set; } = string.Empty;

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
