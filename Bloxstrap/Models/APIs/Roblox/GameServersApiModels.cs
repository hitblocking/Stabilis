using System.Text.Json.Serialization;

namespace Bloxstrap.Models.APIs.Roblox
{
    public class MultigetPlaceRow
    {
        [JsonPropertyName("universeId")]
        public long UniverseId { get; set; }
    }

    public class GameServersPageResponse
    {
        [JsonPropertyName("data")]
        public List<GameServerEntry>? Data { get; set; }

        [JsonPropertyName("nextPageCursor")]
        public string? NextPageCursor { get; set; }
    }

    public class GameServerEntry
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("playing")]
        public int Playing { get; set; }

        [JsonPropertyName("maxPlayers")]
        public int MaxPlayers { get; set; }

        /// <summary>
        /// Latency hint reported by Roblox for this server listing (not measured from this PC).
        /// </summary>
        [JsonPropertyName("ping")]
        public double Ping { get; set; }

        public string PlayersDisplay => $"{Playing}/{MaxPlayers}";
    }
}
