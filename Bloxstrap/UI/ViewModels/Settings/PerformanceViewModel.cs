using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
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

        private void ApplyFpsCapFlags()
        {
            int? cap = App.Settings.Prop.PerformanceFPSCap;
            bool useCap = App.Settings.Prop.PerformanceManualFPSOverride && cap.HasValue && cap.Value > 0;
            string? value = useCap ? cap!.Value.ToString() : null;

            App.FastFlags.SetValue(FpsCapFlag, value);
            App.FastFlags.SetValue(FpsCapFlagLegacy, value);
        }

        private void Apply()
        {
            PerformanceProfileManager.ApplyPreset(SelectedProfile);
            ApplyFpsCapFlags();

            // persist fast flags and settings so the preset takes effect immediately
            try
            {
                App.FastFlags.Save();
                App.Settings.Save();
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
            PerformanceProfileManager.ApplyPreset(SelectedProfile);
            ApplyFpsCapFlags();
        }
    }
}
