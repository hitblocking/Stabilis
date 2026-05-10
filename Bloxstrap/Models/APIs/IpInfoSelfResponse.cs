using System.Text.Json.Serialization;

namespace Bloxstrap.Models.APIs
{
    /// <summary>
    /// Response from <c>https://ipinfo.io/json</c> (caller&apos;s public IP / geo).
    /// </summary>
    public class IpInfoSelfResponse
    {
        [JsonPropertyName("ip")]
        public string? Ip { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("region")]
        public string? Region { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        /// <summary>
        /// IANA zone, e.g. <c>Europe/Berlin</c> — used to infer coarse region.
        /// </summary>
        [JsonPropertyName("timezone")]
        public string? Timezone { get; set; }
    }
}
