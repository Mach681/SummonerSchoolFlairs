using System.Text.Json.Serialization;

namespace RiotOAuthCallback.Classes
{
    public record TokenResponse
    {
        [JsonPropertyName("scope")]
        public string Scope { get; set; } = string.Empty;
        [JsonPropertyName("expires_in")]
        public int Expires_In { get; set; }
        [JsonPropertyName("token_type")]
        public string Token_Type { get; set; } = string.Empty;
        [JsonPropertyName("refresh_token")]
        public string Refresh_Token { get; set; } = string.Empty;
        [JsonPropertyName("id_token")]
        public string Id_Token { get; set; } = string.Empty;
        [JsonPropertyName("sub_sid")]
        public string Sub_Sid { get; set; } = string.Empty;
        [JsonPropertyName("access_token")]
        public string Access_Token { get; set; } = string.Empty;
    }
}
