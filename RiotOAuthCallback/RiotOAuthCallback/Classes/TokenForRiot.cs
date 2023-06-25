using System.Text.Json.Serialization;

namespace RiotOAuthCallback.Classes
{
    public record TokenForRiot
    {
        [JsonPropertyName("aud")]
        public List<string> Audience { get; set; } = new List<string>();

        [JsonPropertyName("exp")]
        public double Expiration { get; set; } = DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds();

        [JsonPropertyName("iat")]
        public double IssuedAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        [JsonPropertyName("iss")]
        public string Issuer { get; set; } = string.Empty;

        [JsonPropertyName("jti")]
        public string JwtId { get; set; } = string.Empty;

        [JsonPropertyName("sub")]
        public string Subject { get; set; } = string.Empty;
    }
}
