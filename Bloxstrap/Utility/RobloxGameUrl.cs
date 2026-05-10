using System.Text.RegularExpressions;

using Bloxstrap.Enums;

namespace Bloxstrap.Utility
{
    /// <summary>
    /// Resolves Roblox experience identifiers from web URLs and launch URIs (e.g. roblox-player / roblox://).
    /// </summary>
    public static class RobloxGameUrl
    {
        /// <summary>
        /// Matches https://www.roblox.com/games/{placeId}/...
        /// </summary>
        private static readonly Regex WebGamesPath = new(@"roblox\.com/games/(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// placeId in query strings and roblox-player segments (<c>+placeId=123</c>, <c>?placeId=123</c>).
        /// </summary>
        private static readonly Regex PlaceIdParameter = new(@"(?:^|[?&+])placeId=(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Optional region hint appended to a roblox.com game URL, e.g.
        /// <c>https://www.roblox.com/games/123/game?bloxstrapRegion=Europe</c>.
        /// </summary>
        public static string AppendRegionQuery(string robloxWebOrLaunchUri, RobloxRegionPreference preference)
        {
            if (preference == RobloxRegionPreference.Auto || string.IsNullOrWhiteSpace(robloxWebOrLaunchUri))
                return robloxWebOrLaunchUri;

            char sep = robloxWebOrLaunchUri.Contains('?', StringComparison.Ordinal) ? '&' : '?';
            return $"{robloxWebOrLaunchUri}{sep}bloxstrapRegion={preference}";
        }

        private static readonly Regex RegionPreferenceParameter =
            new(@"(?:[?&+])(?:bloxstrapRegion|region)=([^&+]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static bool TryParseRegionPreference(string? uriOrUrl, out RobloxRegionPreference preference)
        {
            preference = RobloxRegionPreference.Auto;

            if (string.IsNullOrWhiteSpace(uriOrUrl))
                return false;

            Match m = RegionPreferenceParameter.Match(uriOrUrl.Trim());
            if (!m.Success)
                return false;

            string raw = Uri.UnescapeDataString(m.Groups[1].Value.Trim());
            if (TryMatchRegionToken(raw, out preference))
                return true;

            return false;
        }

        private static bool TryMatchRegionToken(string raw, out RobloxRegionPreference preference)
        {
            preference = RobloxRegionPreference.Auto;

            raw = raw.Trim();

            if (Enum.TryParse(raw, true, out preference))
                return true;

            string compact = raw.Replace(" ", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal);
            if (Enum.TryParse(compact, true, out preference))
                return true;

            string[] parts = raw.Split(new[] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length > 1)
            {
                string pascal = string.Concat(parts.Select(static p =>
                    p.Length == 0 ? "" : char.ToUpperInvariant(p[0]) + p.AsSpan(1).ToString().ToLowerInvariant()));
                if (Enum.TryParse(pascal, true, out preference))
                    return true;
            }

            return raw.ToUpperInvariant() switch
            {
                "US" or "NA" or "SA" or "LATAM" => SetPref(out preference, RobloxRegionPreference.Americas),
                "EU" or "UK" or "GB" => SetPref(out preference, RobloxRegionPreference.Europe),
                "AP" or "JP" or "KR" or "SG" => SetPref(out preference, RobloxRegionPreference.AsiaPacific),
                "AU" or "NZ" => SetPref(out preference, RobloxRegionPreference.Oceania),
                "ME" or "AE" => SetPref(out preference, RobloxRegionPreference.MiddleEastAfrica),
                _ => false,
            };
        }

        private static bool SetPref(out RobloxRegionPreference target, RobloxRegionPreference value)
        {
            target = value;
            return true;
        }

        public static bool TryParsePlaceId(string? uriOrUrl, out long placeId)
        {
            placeId = 0;

            if (string.IsNullOrWhiteSpace(uriOrUrl))
                return false;

            string s = uriOrUrl.Trim();

            Match m = WebGamesPath.Match(s);
            if (m.Success && long.TryParse(m.Groups[1].Value, out placeId))
                return true;

            m = PlaceIdParameter.Match(s);
            return m.Success && long.TryParse(m.Groups[1].Value, out placeId);
        }
    }
}
