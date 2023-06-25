using System.Text.Json.Serialization;

namespace LeagueFlairRiotUpdateService.Classes
{
    public record RiotAccountInfoResult
    {
        [JsonPropertyName("puuid")]
        public string Puuid { get; set; } = string.Empty;

        [JsonPropertyName("gameName")]
        public string Name { get; set; } = string.Empty;
    }
}
