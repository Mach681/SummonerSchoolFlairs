using System.Text.Json.Serialization;

namespace LeagueFlairRiotUpdateService.Classes
{
    public record UserInfoResponse
    {
        [JsonPropertyName("sub")]
        public string Sub { get; set; } = string.Empty;

        [JsonPropertyName("cpid")]
        public string CPID { get; set; } = string.Empty;

        [JsonPropertyName("jti")]
        public string JTI { get; set; } = string.Empty;
    }
}
