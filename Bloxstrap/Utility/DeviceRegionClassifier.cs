using Bloxstrap.Enums;
using Bloxstrap.Models.APIs;

namespace Bloxstrap.Utility
{
    /// <summary>
    /// Maps ipinfo fields to coarse region buckets for user-facing copy.
    /// </summary>
    internal static class DeviceRegionClassifier
    {
        private static readonly HashSet<string> AmericasCc = new(StringComparer.OrdinalIgnoreCase)
        {
            "US", "CA", "MX", "BR", "AR", "CL", "CO", "PE", "VE", "UY", "PY", "BO", "EC", "CR", "PA", "GT", "HN", "NI", "SV", "DO", "CU", "JM", "TT", "BS", "BZ", "SR", "GY", "GF", "GL"
        };

        private static readonly HashSet<string> EuropeCc = new(StringComparer.OrdinalIgnoreCase)
        {
            "GB", "IE", "FR", "DE", "ES", "IT", "NL", "BE", "CH", "AT", "SE", "NO", "DK", "FI", "PL", "CZ", "SK", "HU", "RO", "BG", "HR", "SI", "RS", "BA", "ME", "MK", "AL", "GR", "PT", "LU", "MT", "CY", "EE", "LV", "LT", "IS", "LI", "AD", "MC", "SM", "VA", "UA", "MD", "BY", "XK", "TR"
        };

        private static readonly HashSet<string> AsiaPacificCc = new(StringComparer.OrdinalIgnoreCase)
        {
            "CN", "JP", "KR", "IN", "SG", "TH", "VN", "PH", "MY", "ID", "BD", "PK", "LK", "NP", "MM", "KH", "LA", "BN", "MN", "HK", "TW", "MO", "MV", "BT", "TL", "GE", "FJ", "NC", "PF", "PG", "GU", "AS", "MP", "WS", "TO", "VU", "SB", "KI", "FM", "MH", "PW", "CK", "NU", "NR", "TV"
        };

        private static readonly HashSet<string> OceaniaCc = new(StringComparer.OrdinalIgnoreCase)
        {
            "AU", "NZ", "NF"
        };

        private static readonly HashSet<string> MiddleEastAfricaCc = new(StringComparer.OrdinalIgnoreCase)
        {
            "ZA", "EG", "NG", "KE", "AE", "SA", "IL", "IQ", "IR", "QA", "KW", "BH", "OM", "YE", "JO", "LB", "SY", "PS", "MA", "DZ", "TN", "LY", "ET", "GH", "TZ", "UG", "AO", "MZ", "ZW", "BW", "NA", "SN", "CI", "CM", "RW", "BI", "MG", "MU", "SC", "SD", "SS", "SO", "DJ", "ER", "TD", "NE", "ML", "BF", "MR", "GM", "GW", "GN", "SL", "LR", "TG", "BJ", "GA", "CG", "CD", "CF", "GQ", "ST", "CV"
        };

        public static RobloxRegionPreference Classify(IpInfoSelfResponse info)
        {
            if (!string.IsNullOrEmpty(info.Timezone))
            {
                int slash = info.Timezone.IndexOf('/');
                string prefix = slash > 0 ? info.Timezone[..slash] : info.Timezone;

                return prefix switch
                {
                    "America" => RobloxRegionPreference.Americas,
                    "Europe" => RobloxRegionPreference.Europe,
                    "Asia" => RobloxRegionPreference.AsiaPacific,
                    "Australia" or "Pacific" => RobloxRegionPreference.Oceania,
                    "Africa" or "Indian" => RobloxRegionPreference.MiddleEastAfrica,
                    "Atlantic" => RobloxRegionPreference.Europe,
                    "Arctic" => RobloxRegionPreference.Europe,
                    _ => ClassifyByCountry(info.Country),
                };
            }

            return ClassifyByCountry(info.Country);
        }

        private static RobloxRegionPreference ClassifyByCountry(string? cc)
        {
            if (string.IsNullOrEmpty(cc))
                return RobloxRegionPreference.Auto;

            cc = cc.Trim();

            if (AmericasCc.Contains(cc))
                return RobloxRegionPreference.Americas;
            if (EuropeCc.Contains(cc))
                return RobloxRegionPreference.Europe;
            if (OceaniaCc.Contains(cc))
                return RobloxRegionPreference.Oceania;
            if (MiddleEastAfricaCc.Contains(cc))
                return RobloxRegionPreference.MiddleEastAfrica;
            if (AsiaPacificCc.Contains(cc))
                return RobloxRegionPreference.AsiaPacific;

            return RobloxRegionPreference.Auto;
        }
    }
}
