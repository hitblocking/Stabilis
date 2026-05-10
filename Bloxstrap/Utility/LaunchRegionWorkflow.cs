using Bloxstrap.Enums;

namespace Bloxstrap.Utility
{
    internal static class LaunchRegionWorkflow
    {
        /// <summary>
        /// URL parameter overrides saved settings for this launch only.
        /// </summary>
        public static RobloxRegionPreference GetEffectivePreference()
        {
            if (App.LaunchSettings.ParsedLaunchRegionPreference is RobloxRegionPreference fromUri)
                return fromUri;

            return App.Settings.Prop.PreferredRobloxRegion;
        }
    }
}
