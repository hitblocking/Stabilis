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

    public class PerformanceViewModel : NotifyPropertyChangedViewModel
    {
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
            set => App.Settings.Prop.PerformanceFPSCap = value;
        }

        public bool ManualOverride
        {
            get => App.Settings.Prop.PerformanceManualFPSOverride;
            set => App.Settings.Prop.PerformanceManualFPSOverride = value;
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

        private void Apply()
        {
            PerformanceProfileManager.ApplyPreset(SelectedProfile);

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
            PerformanceProfileManager.ApplyPreset(SelectedProfile);
        }
    }
}
