using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Bloxstrap
{
    public class PerformanceProfile
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int? FPSCap { get; set; } = null;
        public Dictionary<string, string?> FastFlags { get; set; } = new();
    }

    public static class PerformanceProfileManager
    {
        private static readonly string PresetsFolder = Path.Combine(Paths.Base, "FastFlagsPresets");
        private static readonly List<PerformanceProfile> _presets = new();

        static PerformanceProfileManager()
        {
            LoadPresets();
        }

        public static IReadOnlyList<PerformanceProfile> Presets => _presets;

        public static void LoadPresets()
        {
            _presets.Clear();

            try
            {
                if (Directory.Exists(PresetsFolder))
                {
                    foreach (var file in Directory.GetFiles(PresetsFolder, "*.json"))
                    {
                        try
                        {
                            var p = JsonSerializer.Deserialize<PerformanceProfile>(File.ReadAllText(file));
                            if (p is not null)
                                _presets.Add(p);
                        }
                        catch { }
                    }
                }
            }
            catch { }

            // ensure defaults
            if (!_presets.Any(x => x.Name == "Balanced"))
                _presets.Add(new PerformanceProfile
                {
                    Name = "Balanced",
                    Description = "Balanced (default)",
                    FPSCap = null,
                    FastFlags = new Dictionary<string, string?>
                    {
                        { "DFFlagDisableDPIScale", "False" },
                        { "DFIntCSGLevelOfDetailSwitchingDistanceL12", "2000" },
                        { "DFIntCSGLevelOfDetailSwitchingDistanceL34", "1500" },
                        { "DFIntCSGLevelOfDetailSwitchingDistanceL23", "1000" },
                        { "DFIntCSGLevelOfDetailSwitchingDistance", "3000" },
                        { "FFlagHandleAltEnterFullscreenManually", "True" },
                        { "FIntDebugForceMSAASamples", null }
                    }
                });

            if (!_presets.Any(x => x.Name == "Performance"))
            {
                _presets.Add(new PerformanceProfile
                {
                    Name = "Performance",
                    Description = "Reduce visual load and suggest a stable FPS target",
                    FPSCap = SuggestFPSCap(),
                    FastFlags = new Dictionary<string, string?>
                    {
                        { "DFFlagDisableDPIScale", "True" },
                        { "DFIntCSGLevelOfDetailSwitchingDistanceL12", "1500" },
                        { "DFIntCSGLevelOfDetailSwitchingDistanceL34", "1000" },
                        { "DFIntCSGLevelOfDetailSwitchingDistanceL23", "500" },
                        { "DFIntCSGLevelOfDetailSwitchingDistance", "2000" },
                        { "FFlagHandleAltEnterFullscreenManually", "False" },
                        { "FIntDebugForceMSAASamples", "2" },
                        { "Rendering.TextureQuality.OverrideEnabled", "True" },
                        { "Rendering.TextureQuality.Level", "0" }
                    }
                });
            }

            if (!_presets.Any(x => x.Name == "Potato"))
            {
                _presets.Add(new PerformanceProfile
                {
                    Name = "Potato",
                    Description = "Lowest graphics for maximum stability on weak machines",
                    FPSCap = Math.Min(SuggestFPSCap(), 60),
                    FastFlags = new Dictionary<string, string?>
                    {
                        { "DFIntCSGLevelOfDetailSwitchingDistance", "800" },
                        { "DFIntCSGLevelOfDetailSwitchingDistanceL12", "400" },
                        { "DFIntCSGLevelOfDetailSwitchingDistanceL23", "250" },
                        { "DFIntCSGLevelOfDetailSwitchingDistanceL34", "150" },
                        { "FFlagHandleAltEnterFullscreenManually", "False" },
                        { "DFFlagTextureQualityOverrideEnabled", "True" },
                        { "DFIntTextureQualityOverride", "0" },
                        { "FIntDebugForceMSAASamples", "0" },
                        { "DFFlagDisableDPIScale", "True" },
                        { "FFlagDebugGraphicsPreferD3D11", "True" },
                        { "FFlagDebugSkyGray", "True" },
                        { "DFFlagDebugPauseVoxelizer", "True" },
                        { "DFIntDebugFRMQualityLevelOverride", "0" },
                        { "FIntFRMMaxGrassDistance", "0" },
                        { "FIntFRMMinGrassDistance", "0" }
                    }
                });
            }

            if (!_presets.Any(x => x.Name == "UltraStability"))
            {
                _presets.Add(new PerformanceProfile
                {
                    Name = "UltraStability",
                    Description = "Aggressive stutter reduction and strict FPS cap",
                    FPSCap = Math.Min(SuggestFPSCap(), 60),
                    FastFlags = new Dictionary<string, string?>
                    {
                        { "DFFlagDisableDPIScale", "True" },
                        { "DFIntCSGLevelOfDetailSwitchingDistanceL12", "1200" },
                        { "DFIntCSGLevelOfDetailSwitchingDistanceL34", "800" },
                        { "DFIntCSGLevelOfDetailSwitchingDistanceL23", "400" },
                        { "DFIntCSGLevelOfDetailSwitchingDistance", "1400" },
                        { "FFlagHandleAltEnterFullscreenManually", "False" },
                        { "FIntDebugForceMSAASamples", "1" },
                        { "Rendering.TextureQuality.OverrideEnabled", "True" },
                        { "Rendering.TextureQuality.Level", "0" }
                    }
                });
            }
        }

        public static PerformanceProfile? GetPreset(string name) => _presets.FirstOrDefault(x => x.Name == name);

        public static void ApplyPreset(string name)
        {
            var preset = GetPreset(name);
            if (preset is null)
                return;

            // apply fastflags
            foreach (var kv in preset.FastFlags)
            {
                // if the key matches a preset mapping like "Rendering.DisableScaling", map to the actual flag id
                try
                {
                    if (FastFlagManager.PresetFlags.ContainsKey(kv.Key))
                    {
                        var flagId = FastFlagManager.PresetFlags[kv.Key];
                        if (kv.Value is null)
                            App.FastFlags.SetValue(flagId, null);
                        else
                            App.FastFlags.SetValue(flagId, kv.Value);
                    }
                    else
                    {
                        if (kv.Value is null)
                            App.FastFlags.SetValue(kv.Key, null);
                        else
                            App.FastFlags.SetValue(kv.Key, kv.Value);
                    }
                }
                catch
                {
                    // swallow to avoid failing profile application; log could be added if desired
                }
            }

            // apply fps cap into settings unless user overrides manually
            if (!App.Settings.Prop.PerformanceManualFPSOverride)
            {
                App.Settings.Prop.PerformanceFPSCap = preset.FPSCap;
            }

            // persist a simple profile file in modifications so it can be applied via existing flow
            try
            {
                string modFolder = Path.Combine(Paths.Modifications, "ClientSettings");
                Directory.CreateDirectory(modFolder);
                var perfObj = new { Profile = preset.Name, FPSCap = App.Settings.Prop.PerformanceFPSCap };
                File.WriteAllText(Path.Combine(modFolder, "PerformanceProfile.json"), JsonSerializer.Serialize(perfObj, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        public static int SuggestFPSCap()
        {
            int refresh = GetPrimaryMonitorRefreshRate();

            int[] candidates = new[] { 240, 144, 120, 75, 60 };

            foreach (var c in candidates)
            {
                if (refresh >= c)
                    return c;
            }

            return 60;
        }

        private static int GetPrimaryMonitorRefreshRate()
        {
            try
            {
                // use EnumDisplaySettings to get dmDisplayFrequency
                DEVMODE vDevMode = new DEVMODE();
                vDevMode.dmSize = (short)Marshal.SizeOf(vDevMode);
                if (EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref vDevMode))
                    return (int)vDevMode.dmDisplayFrequency;
            }
            catch { }

            return 60;
        }

        private const int ENUM_CURRENT_SETTINGS = -1;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public int dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        private static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);
    }
}
