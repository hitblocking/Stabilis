using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Bloxstrap
{
    /// <summary>
    /// Patches Roblox's <c>%LocalAppData%\Roblox\GlobalBasicSettings_*.xml</c> (same store as the in-game Settings menu).
    /// This is separate from <see cref="FastFlagManager"/> / ClientAppSettings.json.
    /// </summary>
    public static class RobloxGlobalBasicSettings
    {
        private const string LOG_IDENT = "RobloxGlobalBasicSettings";

        /// <summary>
        /// Writes Escape-menu settings to <c>GlobalBasicSettings_*.xml</c> (FramerateCap, optional toggles). Same file Roblox’s in-game Settings uses.
        /// </summary>
        public static void ApplyInGameMenuSettings()
        {
            if (!NeedsAnyPatch())
                return;

            try
            {
                string robloxDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Roblox");
                if (!Directory.Exists(robloxDir))
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Roblox AppData folder not found yet ({robloxDir}). Launch Roblox once, then save settings again.");
                    return;
                }

                string? path = FindLatestSettingsFile(robloxDir);
                if (path is null)
                {
                    App.Logger.WriteLine(LOG_IDENT, "No GlobalBasicSettings_*.xml found. Launch Roblox once to create it, then apply again.");
                    return;
                }

                var doc = XDocument.Load(path, LoadOptions.PreserveWhitespace);
                bool modified = false;

                modified |= TryApplyFramerateCap(doc);
                modified |= TryApplyInGameClientPatches(doc);

                if (!modified)
                    return;

                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    OmitXmlDeclaration = false,
                    Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                    NewLineHandling = NewLineHandling.Replace
                };

                using (var writer = XmlWriter.Create(path, settings))
                    doc.Save(writer);

                App.Logger.WriteLine(LOG_IDENT, $"Updated {Path.GetFileName(path)}");
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to patch GlobalBasicSettings (close Roblox and retry if the file is locked).");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        /// <summary>Same as <see cref="ApplyInGameMenuSettings"/> (legacy name).</summary>
        public static void ApplyFromStabilisSettings() => ApplyInGameMenuSettings();

        /// <summary>Back-compat name; calls <see cref="ApplyInGameMenuSettings"/>.</summary>
        public static void ApplyFramerateCapFromSettings() => ApplyInGameMenuSettings();

        /// <summary>
        /// Startup / Play button path: only sync XML when <see cref="Models.Persistable.Settings.RobloxXmlSyncOnLaunchAndStartup"/> is on.
        /// Saving settings from Stabilis always uses <see cref="ApplyInGameMenuSettings"/> instead.
        /// </summary>
        public static void ApplyInGameMenuSettingsFromLaunchPipelineIfEnabled()
        {
            if (!App.Settings.Prop.RobloxXmlSyncOnLaunchAndStartup)
            {
                App.Logger.WriteLine(LOG_IDENT, "Skipping GlobalBasicSettings sync from launch pipeline (RobloxXmlSyncOnLaunchAndStartup is false).");
                return;
            }

            ApplyInGameMenuSettings();
        }

        private static bool NeedsAnyPatch()
        {
            var s = App.Settings.Prop;
            if (s.PerformanceManualFPSOverride && s.PerformanceFPSCap.HasValue && s.PerformanceFPSCap.Value > 0)
                return true;
            if (s.RobloxXmlForcePerformanceStatsOff || s.RobloxXmlGraphicsQualityAutomatic || s.RobloxXmlDisableChatTranslation)
                return true;
            return false;
        }

        private static bool TryApplyFramerateCap(XDocument doc)
        {
            if (!App.Settings.Prop.PerformanceManualFPSOverride
                || !App.Settings.Prop.PerformanceFPSCap.HasValue
                || App.Settings.Prop.PerformanceFPSCap.Value <= 0)
                return false;

            int cap = Math.Clamp(App.Settings.Prop.PerformanceFPSCap.Value, 1, 1000);
            if (!TrySetFramerateCap(doc, cap.ToString()))
            {
                App.Logger.WriteLine(LOG_IDENT, "FramerateCap element not found in XML; Roblox may have changed the format.");
                return false;
            }

            App.Logger.WriteLine(LOG_IDENT, $"Set FramerateCap={cap}");
            return true;
        }

        private static bool TryApplyInGameClientPatches(XDocument doc)
        {
            var s = App.Settings.Prop;
            bool any = false;

            if (s.RobloxXmlForcePerformanceStatsOff)
            {
                if (TrySetBoolSetting(doc, "PerformanceStatsVisible", false))
                {
                    App.Logger.WriteLine(LOG_IDENT, "Set PerformanceStatsVisible=false (in-game Performance Stats)");
                    any = true;
                }
                else
                    App.Logger.WriteLine(LOG_IDENT, "PerformanceStatsVisible not found in XML — open in-game Settings once so Roblox creates this key.");
            }

            if (s.RobloxXmlGraphicsQualityAutomatic)
            {
                // Enum.SavedQualitySetting.Automatic == 0 (matches Roblox GameSettings.lua)
                if (TrySetNumericSetting(doc, "SavedQualityLevel", 0))
                {
                    App.Logger.WriteLine(LOG_IDENT, "Set SavedQualityLevel=0 (Graphics Mode: Automatic)");
                    any = true;
                }
                else
                    App.Logger.WriteLine(LOG_IDENT, "SavedQualityLevel not found in XML — open in-game Settings once so Roblox creates this key.");
            }

            if (s.RobloxXmlDisableChatTranslation)
            {
                if (TrySetBoolSetting(doc, "ChatTranslationEnabled", false))
                {
                    App.Logger.WriteLine(LOG_IDENT, "Set ChatTranslationEnabled=false (Automatic Chat Translation)");
                    any = true;
                }
                else
                    App.Logger.WriteLine(LOG_IDENT, "ChatTranslationEnabled not found in XML — open in-game Settings once so Roblox creates this key.");
            }

            return any;
        }

        private static string? FindLatestSettingsFile(string robloxDir)
        {
            string[] files = Directory.GetFiles(robloxDir, "GlobalBasicSettings_*.xml", SearchOption.TopDirectoryOnly);
            if (files.Length == 0)
                return null;

            return files
                .Select(f => (Path: f, Ver: ParseVersionSuffix(Path.GetFileNameWithoutExtension(f))))
                .OrderByDescending(x => x.Ver)
                .First()
                .Path;
        }

        private static int ParseVersionSuffix(string fileNameWithoutExt)
        {
            int i = fileNameWithoutExt.LastIndexOf('_');
            if (i < 0 || i >= fileNameWithoutExt.Length - 1)
                return 0;
            return int.TryParse(fileNameWithoutExt.AsSpan((i + 1)), out int v) ? v : 0;
        }

        private static bool TrySetFramerateCap(XDocument doc, string value)
        {
            foreach (XElement el in doc.Descendants())
            {
                XAttribute? nameAttr = el.Attribute("name");
                if (nameAttr is not null && string.Equals(nameAttr.Value, "FramerateCap", StringComparison.Ordinal))
                {
                    el.Value = value;
                    return true;
                }
            }

            foreach (XElement el in doc.Descendants())
            {
                if (string.Equals(el.Name.LocalName, "FramerateCap", StringComparison.OrdinalIgnoreCase))
                {
                    el.Value = value;
                    return true;
                }
            }

            return false;
        }

        private static bool TrySetBoolSetting(XDocument doc, string name, bool value)
        {
            string s = value ? "true" : "false";
            foreach (XElement el in doc.Descendants())
            {
                if (el.Attribute("name") is not XAttribute a || !string.Equals(a.Value, name, StringComparison.Ordinal))
                    continue;
                if (el.Name.LocalName is "bool")
                {
                    el.Value = s;
                    return true;
                }
            }

            return false;
        }

        private static bool TrySetNumericSetting(XDocument doc, string name, int value)
        {
            string s = value.ToString(CultureInfo.InvariantCulture);
            foreach (XElement el in doc.Descendants())
            {
                if (el.Attribute("name") is not XAttribute a || !string.Equals(a.Value, name, StringComparison.Ordinal))
                    continue;
                if (el.Name.LocalName is "uint" or "int")
                {
                    el.Value = s;
                    return true;
                }
            }

            return false;
        }
    }
}
