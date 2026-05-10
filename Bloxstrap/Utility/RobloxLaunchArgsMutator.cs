using System.Text.RegularExpressions;

namespace Bloxstrap.Utility
{
    internal static class RobloxLaunchArgsMutator
    {
        private static readonly Regex GameInstanceIdRx = new(@"([?&+])gameInstanceId=([^&+]*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Sets or replaces <c>gameInstanceId</c> on <c>roblox://</c> / <c>roblox-player:</c> launch strings.
        /// </summary>
        public static string SetGameInstanceId(string launchArgs, string jobId)
        {
            if (string.IsNullOrWhiteSpace(launchArgs))
                return launchArgs;

            if (GameInstanceIdRx.IsMatch(launchArgs))
                return GameInstanceIdRx.Replace(launchArgs, $"$1gameInstanceId={jobId}");

            bool robloxPlayer = launchArgs.StartsWith("roblox-player:", StringComparison.OrdinalIgnoreCase);
            char sep = robloxPlayer ? '+' :
                launchArgs.Contains('?', StringComparison.Ordinal) ? '&' : '?';

            return $"{launchArgs}{sep}gameInstanceId={jobId}";
        }
    }
}
