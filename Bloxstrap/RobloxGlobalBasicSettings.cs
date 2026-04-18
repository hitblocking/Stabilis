using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Bloxstrap
{
    /// <summary>
    /// Patches Roblox's <c>%LocalAppData%\Roblox\GlobalBasicSettings_*.xml</c> <c>FramerateCap</c> value.
    /// Modern clients often follow this file (and the in-game FPS control) more reliably than <c>DFIntTaskSchedulerTargetFps</c> alone.
    /// </summary>
    public static class RobloxGlobalBasicSettings
    {
        private const string LOG_IDENT = "RobloxGlobalBasicSettings";

        /// <summary>
        /// When manual FPS is enabled, writes <see cref="Models.Persistable.Settings.PerformanceFPSCap"/> into the newest GlobalBasicSettings XML.
        /// No-op if the Roblox folder or settings file does not exist yet (user must run Roblox at least once).
        /// </summary>
        public static void ApplyFramerateCapFromSettings()
        {
            if (!App.Settings.Prop.PerformanceManualFPSOverride
                || !App.Settings.Prop.PerformanceFPSCap.HasValue
                || App.Settings.Prop.PerformanceFPSCap.Value <= 0)
            {
                return;
            }

            int cap = Math.Clamp(App.Settings.Prop.PerformanceFPSCap.Value, 1, 1000);
            string value = cap.ToString();

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
                    App.Logger.WriteLine(LOG_IDENT, "No GlobalBasicSettings_*.xml found. Launch Roblox once to create it, then apply your FPS cap again.");
                    return;
                }

                var doc = XDocument.Load(path, LoadOptions.PreserveWhitespace);
                if (!TrySetFramerateCap(doc, value))
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Could not find FramerateCap in {Path.GetFileName(path)}; Roblox may have changed the XML format.");
                    return;
                }

                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    OmitXmlDeclaration = false,
                    Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                    NewLineHandling = NewLineHandling.Replace
                };

                using (var writer = XmlWriter.Create(path, settings))
                    doc.Save(writer);

                App.Logger.WriteLine(LOG_IDENT, $"Set FramerateCap={value} in {Path.GetFileName(path)}");
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to patch GlobalBasicSettings (close Roblox and retry if the file is locked).");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
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
            // Typical: <uint name="FramerateCap">144</uint> (or int)
            foreach (XElement el in doc.Descendants())
            {
                XAttribute? nameAttr = el.Attribute("name");
                if (nameAttr is not null && string.Equals(nameAttr.Value, "FramerateCap", StringComparison.Ordinal))
                {
                    el.Value = value;
                    return true;
                }
            }

            // Fallback: element name
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
    }
}
