using Bloxstrap.Enums;
using Bloxstrap.Models.APIs;

namespace Bloxstrap.Utility
{
    internal static class UserDeviceRegionDetector
    {
        /// <summary>
        /// Geo region for this device using ipinfo (public IP).
        /// </summary>
        public static async Task<string?> GetRegionSummaryLineAsync()
        {
            var parts = await GetRegionAnnouncementPartsAsync();
            if (parts.BucketLine is null)
                return null;

            return string.IsNullOrEmpty(parts.DetailLine)
                ? parts.BucketLine
                : $"{parts.BucketLine} — {parts.DetailLine}";
        }

        /// <summary>
        /// Explicit bucket name plus optional network location line for the launch toast.
        /// </summary>
        public static async Task<(string? BucketLine, string? DetailLine)> GetRegionAnnouncementPartsAsync()
        {
            const string LOG_IDENT = "UserDeviceRegionDetector::GetRegionAnnouncementPartsAsync";

            try
            {
                var info = await Http.GetJson<IpInfoSelfResponse>("https://ipinfo.io/json");
                RobloxRegionPreference bucket = DeviceRegionClassifier.Classify(info);

                string? bucketLine = bucket switch
                {
                    RobloxRegionPreference.Auto => Strings.Dialog_LaunchRegion_Toast_RegionUndetermined,
                    RobloxRegionPreference.Americas => string.Format(Strings.Dialog_LaunchRegion_Toast_YouAreIn, Strings.Menu_Integrations_Region_Value_Americas),
                    RobloxRegionPreference.Europe => string.Format(Strings.Dialog_LaunchRegion_Toast_YouAreIn, Strings.Menu_Integrations_Region_Value_Europe),
                    RobloxRegionPreference.AsiaPacific => string.Format(Strings.Dialog_LaunchRegion_Toast_YouAreIn, Strings.Menu_Integrations_Region_Value_AsiaPacific),
                    RobloxRegionPreference.Oceania => string.Format(Strings.Dialog_LaunchRegion_Toast_YouAreIn, Strings.Menu_Integrations_Region_Value_Oceania),
                    RobloxRegionPreference.MiddleEastAfrica => string.Format(Strings.Dialog_LaunchRegion_Toast_YouAreIn, Strings.Menu_Integrations_Region_Value_MiddleEastAfrica),
                    _ => Strings.Dialog_LaunchRegion_Toast_RegionUndetermined,
                };

                string? detail = FormatNetworkLocation(info);
                string? detailLine = string.IsNullOrEmpty(detail)
                    ? null
                    : string.Format(Strings.Dialog_LaunchRegion_Toast_NetworkLocation, detail);

                return (bucketLine, detailLine);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to resolve device region via ipinfo.io/json");
                App.Logger.WriteException(LOG_IDENT, ex);
                return (null, null);
            }
        }

        private static string? FormatNetworkLocation(IpInfoSelfResponse info)
        {
            if (!string.IsNullOrEmpty(info.City) && !string.IsNullOrEmpty(info.Region) && !string.IsNullOrEmpty(info.Country))
            {
                if (info.City == info.Region)
                    return $"{info.Region}, {info.Country}";
                return $"{info.City}, {info.Region}, {info.Country}";
            }

            if (!string.IsNullOrEmpty(info.Country))
                return info.Country;

            return info.Ip;
        }
    }
}
