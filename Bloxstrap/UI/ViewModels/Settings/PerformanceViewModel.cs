using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Bloxstrap;
using CommunityToolkit.Mvvm.Input;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public sealed class LabeledIntChoice
    {
        public int Value { get; init; }
        public string Display { get; init; } = "";
    }

    public sealed class LabeledEnumChoice<T> where T : struct, Enum
    {
        public T Value { get; init; }
        public string Display { get; init; } = "";
    }

    public class PerformanceViewModel : NotifyPropertyChangedViewModel
    {
        private const string FpsCapFlag = "FIntTaskSchedulerTargetFps";
        private const string FpsCapFlagLegacy = "DFIntTaskSchedulerTargetFps";
        /// <summary>
        /// Lets Roblox honor <see cref="FpsCapFlagLegacy"/> via the in-game FPS control path (see community fast flag lists).
        /// </summary>
        private const string FpsCapEnableMenuFlag = "FFlagGameBasicSettingsFramerateCap";

        public static readonly LabeledIntChoice[] CleanerRetentionChoices =
        {
            new() { Value = 0, Display = "Never" },
            new() { Value = 7, Display = "Older than 7 days" },
            new() { Value = 14, Display = "Older than 14 days" },
            new() { Value = 30, Display = "Older than 30 days" },
            new() { Value = 60, Display = "Older than 60 days" }
        };

        public static readonly LabeledIntChoice[] MemoryTrimIntervalChoices =
        {
            new() { Value = 5, Display = "Every 5 minutes" },
            new() { Value = 10, Display = "Every 10 minutes" },
            new() { Value = 15, Display = "Every 15 minutes" },
            new() { Value = 30, Display = "Every 30 minutes" },
            new() { Value = 60, Display = "Every 60 minutes" }
        };

        public static readonly LabeledEnumChoice<RobloxProcessPriority>[] RobloxProcessPriorityChoices =
        {
            new() { Value = RobloxProcessPriority.Normal, Display = "Normal" },
            new() { Value = RobloxProcessPriority.AboveNormal, Display = "Above Normal" },
            new() { Value = RobloxProcessPriority.High, Display = "High (Caution)" }
        };

        public static readonly LabeledEnumChoice<RobloxAffinityMode>[] RobloxAffinityModeChoices =
        {
            new() { Value = RobloxAffinityMode.Auto, Display = "Auto (CPU model based)" },
            new() { Value = RobloxAffinityMode.AllCores, Display = "All cores" },
            new() { Value = RobloxAffinityMode.Percent75, Display = "75% of logical cores" },
            new() { Value = RobloxAffinityMode.Percent50, Display = "50% of logical cores" }
        };

        public IEnumerable<string> Profiles => PerformanceProfileManager.Presets.Select(x => x.Name);

        public string SelectedProfile
        {
            get => App.Settings.Prop.SelectedPerformanceProfile;
            set => App.Settings.Prop.SelectedPerformanceProfile = value;
        }

        public int SuggestedFPS => PerformanceProfileManager.SuggestFPSCap();

        public int? FPSCap
        {
            get => App.Settings.Prop.PerformanceFPSCap;
            set
            {
                if (App.Settings.Prop.PerformanceFPSCap == value)
                    return;

                App.Settings.Prop.PerformanceFPSCap = value;
                ApplyFpsCapFlags();
            }
        }

        public bool ManualOverride
        {
            get => App.Settings.Prop.PerformanceManualFPSOverride;
            set
            {
                if (App.Settings.Prop.PerformanceManualFPSOverride == value)
                    return;

                App.Settings.Prop.PerformanceManualFPSOverride = value;
                ApplyFpsCapFlags();
            }
        }

        public ICommand ApplyCommand => new RelayCommand(Apply);
        public ICommand ResetCommand => new RelayCommand(Reset);
        public ICommand CleanNowCommand => new RelayCommand(() => Maintenance.CleanTempAndLogs(PerformanceCleanerRetentionDays, PerformanceCleanerCache, PerformanceCleanerLogs, PerformanceCleanerAppLogs));

        public int PerformanceCleanerRetentionDays
        {
            get => App.Settings.Prop.PerformanceCleanerRetentionDays;
            set
            {
                if (App.Settings.Prop.PerformanceCleanerRetentionDays == value)
                    return;
                App.Settings.Prop.PerformanceCleanerRetentionDays = value;
                OnPropertyChanged(nameof(PerformanceCleanerRetentionDays));
            }
        }

        public bool PerformanceCleanerCache
        {
            get => App.Settings.Prop.PerformanceCleanerCache;
            set => App.Settings.Prop.PerformanceCleanerCache = value;
        }

        public bool PerformanceCleanerLogs
        {
            get => App.Settings.Prop.PerformanceCleanerLogs;
            set => App.Settings.Prop.PerformanceCleanerLogs = value;
        }

        public bool PerformanceCleanerAppLogs
        {
            get => App.Settings.Prop.PerformanceCleanerAppLogs;
            set => App.Settings.Prop.PerformanceCleanerAppLogs = value;
        }

        public bool MemoryTrimEnabled
        {
            get => App.Settings.Prop.MemoryTrimEnabled;
            set
            {
                if (App.Settings.Prop.MemoryTrimEnabled == value)
                    return;
                App.Settings.Prop.MemoryTrimEnabled = value;
                if (value && App.Settings.Prop.MemoryTrimIntervalMinutes <= 0)
                {
                    App.Settings.Prop.MemoryTrimIntervalMinutes = MemoryTrimIntervalChoices[2].Value;
                    OnPropertyChanged(nameof(MemoryTrimIntervalPick));
                }
                if (value)
                    Maintenance.StartMemoryTrimmer(App.Settings.Prop.MemoryTrimIntervalMinutes);
                else
                    Maintenance.StopMemoryTrimmer();
                OnPropertyChanged(nameof(MemoryTrimEnabled));
            }
        }

        /// <summary>
        /// Interval shown in the UI (always one of <see cref="MemoryTrimIntervalChoices"/>). When trimming is off and the stored value is 0, the UI shows 15 until the user saves a choice.
        /// </summary>
        public int MemoryTrimIntervalPick
        {
            get
            {
                var s = App.Settings.Prop.MemoryTrimIntervalMinutes;
                if (s > 0 && MemoryTrimIntervalChoices.Any(x => x.Value == s))
                    return s;
                return MemoryTrimIntervalChoices[2].Value;
            }
            set
            {
                var coerced = MemoryTrimIntervalChoices.Any(x => x.Value == value)
                    ? value
                    : MemoryTrimIntervalChoices[2].Value;
                if (App.Settings.Prop.MemoryTrimIntervalMinutes == coerced)
                {
                    if (App.Settings.Prop.MemoryTrimEnabled)
                        Maintenance.StartMemoryTrimmer(coerced);
                    return;
                }
                App.Settings.Prop.MemoryTrimIntervalMinutes = coerced;
                if (App.Settings.Prop.MemoryTrimEnabled)
                    Maintenance.StartMemoryTrimmer(coerced);
                OnPropertyChanged(nameof(MemoryTrimIntervalPick));
            }
        }

        public bool AutoCleanOnApply
        {
            get => App.Settings.Prop.PerformanceAutoCleanTempLogs;
            set => App.Settings.Prop.PerformanceAutoCleanTempLogs = value;
        }

        public string CpuModel => RobloxRuntimeOptimizer.GetCpuModel();

        public int SuggestedAffinityCoreCount => RobloxRuntimeOptimizer.SuggestAutoCoreCount();

        public int LogicalCoreCount => Environment.ProcessorCount;

        public RobloxProcessPriority RobloxProcessPriority
        {
            get => App.Settings.Prop.RobloxProcessPriority;
            set => App.Settings.Prop.RobloxProcessPriority = value;
        }

        public RobloxAffinityMode RobloxAffinityMode
        {
            get => App.Settings.Prop.RobloxAffinityMode;
            set
            {
                if (App.Settings.Prop.RobloxAffinityMode == value)
                    return;

                App.Settings.Prop.RobloxAffinityMode = value;
                OnPropertyChanged(nameof(RobloxAffinityMode));
                OnPropertyChanged(nameof(TargetAffinityCoreCount));
            }
        }

        public int TargetAffinityCoreCount => RobloxRuntimeOptimizer.GetTargetCoreCount(RobloxAffinityMode);

        public bool RobloxRuntimePriorityBoost
        {
            get => App.Settings.Prop.RobloxRuntimePriorityBoost;
            set
            {
                if (App.Settings.Prop.RobloxRuntimePriorityBoost == value)
                    return;

                App.Settings.Prop.RobloxRuntimePriorityBoost = value;
                OnPropertyChanged(nameof(RobloxRuntimePriorityBoost));
            }
        }

        public bool RobloxRuntimeDisablePowerThrottling
        {
            get => App.Settings.Prop.RobloxRuntimeDisablePowerThrottling;
            set
            {
                if (App.Settings.Prop.RobloxRuntimeDisablePowerThrottling == value)
                    return;

                App.Settings.Prop.RobloxRuntimeDisablePowerThrottling = value;
                OnPropertyChanged(nameof(RobloxRuntimeDisablePowerThrottling));
            }
        }

        public bool RobloxShellGpuHighPerformance
        {
            get => App.Settings.Prop.RobloxShellGpuHighPerformance;
            set
            {
                if (App.Settings.Prop.RobloxShellGpuHighPerformance == value)
                    return;

                App.Settings.Prop.RobloxShellGpuHighPerformance = value;
                RobloxRuntimeOptimizer.ApplyShellPreferencesFromSettings();
                OnPropertyChanged(nameof(RobloxShellGpuHighPerformance));
            }
        }

        public bool RobloxShellDisableFullscreenOptimizations
        {
            get => App.Settings.Prop.RobloxShellDisableFullscreenOptimizations;
            set
            {
                if (App.Settings.Prop.RobloxShellDisableFullscreenOptimizations == value)
                    return;

                App.Settings.Prop.RobloxShellDisableFullscreenOptimizations = value;
                RobloxRuntimeOptimizer.ApplyShellPreferencesFromSettings();
                OnPropertyChanged(nameof(RobloxShellDisableFullscreenOptimizations));
            }
        }

        public bool RobloxXmlForcePerformanceStatsOff
        {
            get => App.Settings.Prop.RobloxXmlForcePerformanceStatsOff;
            set
            {
                if (App.Settings.Prop.RobloxXmlForcePerformanceStatsOff == value)
                    return;

                App.Settings.Prop.RobloxXmlForcePerformanceStatsOff = value;
                RobloxGlobalBasicSettings.ApplyInGameMenuSettings();
                OnPropertyChanged(nameof(RobloxXmlForcePerformanceStatsOff));
            }
        }

        public bool RobloxXmlGraphicsQualityAutomatic
        {
            get => App.Settings.Prop.RobloxXmlGraphicsQualityAutomatic;
            set
            {
                if (App.Settings.Prop.RobloxXmlGraphicsQualityAutomatic == value)
                    return;

                App.Settings.Prop.RobloxXmlGraphicsQualityAutomatic = value;
                RobloxGlobalBasicSettings.ApplyInGameMenuSettings();
                OnPropertyChanged(nameof(RobloxXmlGraphicsQualityAutomatic));
            }
        }

        public bool RobloxXmlDisableChatTranslation
        {
            get => App.Settings.Prop.RobloxXmlDisableChatTranslation;
            set
            {
                if (App.Settings.Prop.RobloxXmlDisableChatTranslation == value)
                    return;

                App.Settings.Prop.RobloxXmlDisableChatTranslation = value;
                RobloxGlobalBasicSettings.ApplyInGameMenuSettings();
                OnPropertyChanged(nameof(RobloxXmlDisableChatTranslation));
            }
        }

        public bool RobloxXmlSyncOnLaunchAndStartup
        {
            get => App.Settings.Prop.RobloxXmlSyncOnLaunchAndStartup;
            set
            {
                if (App.Settings.Prop.RobloxXmlSyncOnLaunchAndStartup == value)
                    return;

                App.Settings.Prop.RobloxXmlSyncOnLaunchAndStartup = value;
                OnPropertyChanged(nameof(RobloxXmlSyncOnLaunchAndStartup));
            }
        }

        public bool PerformanceWinTweakMultimediaSystemProfile
        {
            get => App.Settings.Prop.PerformanceWinTweakMultimediaSystemProfile;
            set
            {
                if (App.Settings.Prop.PerformanceWinTweakMultimediaSystemProfile == value)
                    return;
                App.Settings.Prop.PerformanceWinTweakMultimediaSystemProfile = value;
                WindowsPerformanceTweaks.ApplyFromSettings();
                OnPropertyChanged(nameof(PerformanceWinTweakMultimediaSystemProfile));
            }
        }

        public bool PerformanceWinTweakTcpLatency
        {
            get => App.Settings.Prop.PerformanceWinTweakTcpLatency;
            set
            {
                if (App.Settings.Prop.PerformanceWinTweakTcpLatency == value)
                    return;
                App.Settings.Prop.PerformanceWinTweakTcpLatency = value;
                WindowsPerformanceTweaks.ApplyFromSettings();
                OnPropertyChanged(nameof(PerformanceWinTweakTcpLatency));
            }
        }

        public bool PerformanceWinTweakMmcssGames
        {
            get => App.Settings.Prop.PerformanceWinTweakMmcssGames;
            set
            {
                if (App.Settings.Prop.PerformanceWinTweakMmcssGames == value)
                    return;
                App.Settings.Prop.PerformanceWinTweakMmcssGames = value;
                WindowsPerformanceTweaks.ApplyFromSettings();
                OnPropertyChanged(nameof(PerformanceWinTweakMmcssGames));
            }
        }

        public bool PerformanceWinTweakGraphicsTdrDelay
        {
            get => App.Settings.Prop.PerformanceWinTweakGraphicsTdrDelay;
            set
            {
                if (App.Settings.Prop.PerformanceWinTweakGraphicsTdrDelay == value)
                    return;
                App.Settings.Prop.PerformanceWinTweakGraphicsTdrDelay = value;
                WindowsPerformanceTweaks.ApplyFromSettings();
                OnPropertyChanged(nameof(PerformanceWinTweakGraphicsTdrDelay));
            }
        }

        public bool PerformanceWinTweakNtfsNoLastAccess
        {
            get => App.Settings.Prop.PerformanceWinTweakNtfsNoLastAccess;
            set
            {
                if (App.Settings.Prop.PerformanceWinTweakNtfsNoLastAccess == value)
                    return;
                App.Settings.Prop.PerformanceWinTweakNtfsNoLastAccess = value;
                WindowsPerformanceTweaks.ApplyFromSettings();
                OnPropertyChanged(nameof(PerformanceWinTweakNtfsNoLastAccess));
            }
        }

        public bool PerformanceWinTweakWin32PriorityForeground
        {
            get => App.Settings.Prop.PerformanceWinTweakWin32PriorityForeground;
            set
            {
                if (App.Settings.Prop.PerformanceWinTweakWin32PriorityForeground == value)
                    return;
                App.Settings.Prop.PerformanceWinTweakWin32PriorityForeground = value;
                WindowsPerformanceTweaks.ApplyFromSettings();
                OnPropertyChanged(nameof(PerformanceWinTweakWin32PriorityForeground));
            }
        }

        public bool AdvancedPreferD3D11
        {
            get => App.Settings.Prop.PerformanceAdvPreferD3D11;
            set
            {
                if (App.Settings.Prop.PerformanceAdvPreferD3D11 == value)
                    return;

                App.Settings.Prop.PerformanceAdvPreferD3D11 = value;
                ApplyAdvancedPerformanceFlags();
                OnPropertyChanged(nameof(AdvancedPreferD3D11));
            }
        }

        public bool AdvancedDisableGrass
        {
            get => App.Settings.Prop.PerformanceAdvDisableGrass;
            set
            {
                if (App.Settings.Prop.PerformanceAdvDisableGrass == value)
                    return;

                App.Settings.Prop.PerformanceAdvDisableGrass = value;
                ApplyAdvancedPerformanceFlags();
                OnPropertyChanged(nameof(AdvancedDisableGrass));
            }
        }

        public bool AdvancedPauseVoxelizer
        {
            get => App.Settings.Prop.PerformanceAdvPauseVoxelizer;
            set
            {
                if (App.Settings.Prop.PerformanceAdvPauseVoxelizer == value)
                    return;

                App.Settings.Prop.PerformanceAdvPauseVoxelizer = value;
                ApplyAdvancedPerformanceFlags();
                OnPropertyChanged(nameof(AdvancedPauseVoxelizer));
            }
        }

        public bool AdvancedLowTextureQuality
        {
            get => App.Settings.Prop.PerformanceAdvLowTextureQuality;
            set
            {
                if (App.Settings.Prop.PerformanceAdvLowTextureQuality == value)
                    return;

                App.Settings.Prop.PerformanceAdvLowTextureQuality = value;
                ApplyAdvancedPerformanceFlags();
                OnPropertyChanged(nameof(AdvancedLowTextureQuality));
            }
        }

        private void ApplyAdvancedPerformanceFlags()
        {
            App.FastFlags.SetValue("FFlagDebugGraphicsPreferD3D11", AdvancedPreferD3D11 ? "True" : null);
            App.FastFlags.SetValue("FFlagDebugGraphicsPreferD3D11FL10", AdvancedPreferD3D11 ? "True" : null);

            App.FastFlags.SetValue("FIntFRMMaxGrassDistance", AdvancedDisableGrass ? "0" : null);
            App.FastFlags.SetValue("FIntFRMMinGrassDistance", AdvancedDisableGrass ? "0" : null);

            App.FastFlags.SetValue("DFFlagDebugPauseVoxelizer", AdvancedPauseVoxelizer ? "True" : null);

            App.FastFlags.SetValue("DFFlagTextureQualityOverrideEnabled", AdvancedLowTextureQuality ? "True" : null);
            App.FastFlags.SetValue("DFIntTextureQualityOverride", AdvancedLowTextureQuality ? "0" : null);
        }

        /// <summary>
        /// Writes task-scheduler FPS flags from <see cref="Models.Persistable.Settings"/> into the fast flag set.
        /// Call before <see cref="FastFlagManager.Save"/> and before launching Roblox so ClientAppSettings.json matches settings even if the Performance page was never opened this session.
        /// </summary>
        public static void ApplyTaskSchedulerFpsFromSettings()
        {
            int? cap = App.Settings.Prop.PerformanceFPSCap;
            bool useCap = App.Settings.Prop.PerformanceManualFPSOverride && cap.HasValue && cap.Value > 0;
            string? value = useCap ? cap!.Value.ToString() : null;

            App.FastFlags.SetValue(FpsCapFlag, value);
            App.FastFlags.SetValue(FpsCapFlagLegacy, value);
            App.FastFlags.SetValue(FpsCapEnableMenuFlag, useCap ? "True" : null);
        }

        private void ApplyFpsCapFlags() => ApplyTaskSchedulerFpsFromSettings();

        private void Apply()
        {
            PerformanceProfileManager.ApplyPreset(SelectedProfile);
            ApplyFpsCapFlags();

            // persist fast flags and settings so the preset takes effect immediately
            try
            {
                App.FastFlags.Save();
                App.Settings.Save();
                RobloxRuntimeOptimizer.ApplyShellPreferencesFromSettings();
                RobloxGlobalBasicSettings.ApplyInGameMenuSettings();
                WindowsPerformanceTweaks.ApplyFromSettings();
            }
            catch { }

            // auto-clean temp and logs if enabled
            try
            {
                if (AutoCleanOnApply)
                    Maintenance.CleanTempAndLogs(PerformanceCleanerRetentionDays, PerformanceCleanerCache, PerformanceCleanerLogs, PerformanceCleanerAppLogs);
            }
            catch { }

            // if manual override is set, ensure the chosen FPS is persisted in modifications
            if (ManualOverride)
                PerformanceProfileManager.ApplyPreset(SelectedProfile); // ApplyPreset will respect manual override flag
        }

        private void Reset()
        {
            SelectedProfile = "Balanced";
            ManualOverride = false;
            FPSCap = null;
            AdvancedPreferD3D11 = false;
            AdvancedDisableGrass = false;
            AdvancedPauseVoxelizer = false;
            AdvancedLowTextureQuality = false;
            PerformanceWinTweakMultimediaSystemProfile = false;
            PerformanceWinTweakTcpLatency = false;
            PerformanceWinTweakMmcssGames = false;
            PerformanceWinTweakGraphicsTdrDelay = false;
            PerformanceWinTweakNtfsNoLastAccess = false;
            PerformanceWinTweakWin32PriorityForeground = false;
            PerformanceProfileManager.ApplyPreset(SelectedProfile);
            ApplyFpsCapFlags();
        }
    }
}
