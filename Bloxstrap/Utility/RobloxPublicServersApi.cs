using Bloxstrap.Models.APIs.Roblox;

namespace Bloxstrap.Utility
{
    internal static class RobloxPublicServersApi
    {
        /// <summary>
        /// Maps an experience place ID to universe ID for the Games API.
        /// </summary>
        public static async Task<long?> GetUniverseIdForPlaceAsync(long placeId)
        {
            const string LOG_IDENT = "RobloxPublicServersApi::GetUniverseIdForPlaceAsync";

            try
            {
                string url = $"https://games.roblox.com/v1/games/multiget/place?placeIds={placeId}";
                var rows = await Http.GetJson<List<MultigetPlaceRow>>(url);

                if (rows is null || rows.Count == 0)
                    return null;

                return rows[0].UniverseId;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Could not resolve universe for place {placeId}");
                App.Logger.WriteException(LOG_IDENT, ex);
                return null;
            }
        }

        /// <summary>
        /// Fetches public server listings and sorts by ascending reported ping (Roblox-provided).
        /// </summary>
        public static async Task<List<GameServerEntry>> FetchPublicServersSortedByPingAsync(long universeId, int maxPages = 4)
        {
            const string LOG_IDENT = "RobloxPublicServersApi::FetchPublicServersSortedByPingAsync";

            var combined = new List<GameServerEntry>();
            string? cursor = null;

            try
            {
                for (int page = 0; page < maxPages; page++)
                {
                    string url =
                        $"https://games.roblox.com/v1/games/{universeId}/servers/Public?limit=100&excludeFullGames=true&sortOrder=Asc";

                    if (!string.IsNullOrEmpty(cursor))
                        url += "&cursor=" + Uri.EscapeDataString(cursor);

                    var resp = await Http.GetJson<GameServersPageResponse>(url);

                    if (resp?.Data is not null && resp.Data.Count > 0)
                        combined.AddRange(resp.Data);

                    cursor = resp?.NextPageCursor;
                    if (string.IsNullOrEmpty(cursor))
                        break;
                }

                combined.Sort((a, b) => a.Ping.CompareTo(b.Ping));
                return combined;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Failed to list public servers for universe {universeId}");
                App.Logger.WriteException(LOG_IDENT, ex);
                return combined;
            }
        }
    }
}
