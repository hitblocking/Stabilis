using System.Collections.ObjectModel;
using System.Windows.Input;

using Microsoft.Win32;

using CommunityToolkit.Mvvm.Input;

using Bloxstrap.Enums;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public sealed record RegionPreferenceOption(RobloxRegionPreference Value, string Label);

    public class IntegrationsViewModel : NotifyPropertyChangedViewModel
    {
        public ICommand AddIntegrationCommand => new RelayCommand(AddIntegration);

        public ICommand DeleteIntegrationCommand => new RelayCommand(DeleteIntegration);

        public ICommand BrowseIntegrationLocationCommand => new RelayCommand(BrowseIntegrationLocation);

        private void AddIntegration()
        {
            CustomIntegrations.Add(new CustomIntegration()
            {
                Name = Strings.Menu_Integrations_Custom_NewIntegration
            });

            SelectedCustomIntegrationIndex = CustomIntegrations.Count - 1;

            OnPropertyChanged(nameof(SelectedCustomIntegrationIndex));
            OnPropertyChanged(nameof(IsCustomIntegrationSelected));
        }

        private void DeleteIntegration()
        {
            if (SelectedCustomIntegration is null)
                return;

            CustomIntegrations.Remove(SelectedCustomIntegration);

            if (CustomIntegrations.Count > 0)
            {
                SelectedCustomIntegrationIndex = CustomIntegrations.Count - 1;
                OnPropertyChanged(nameof(SelectedCustomIntegrationIndex));
            }

            OnPropertyChanged(nameof(IsCustomIntegrationSelected));
        }

        private void BrowseIntegrationLocation()
        {
            if (SelectedCustomIntegration is null)
                return;

            var dialog = new OpenFileDialog
            {
                Filter = $"{Strings.Menu_AllFiles}|*.*"
            };

            if (dialog.ShowDialog() != true)
                return;

            SelectedCustomIntegration.Name = dialog.SafeFileName;
            SelectedCustomIntegration.Location = dialog.FileName;
            OnPropertyChanged(nameof(SelectedCustomIntegration));
        }

        public bool ActivityTrackingEnabled
        {
            get => App.Settings.Prop.EnableActivityTracking;
            set
            {
                App.Settings.Prop.EnableActivityTracking = value;

                if (!value)
                {
                    ShowServerDetailsEnabled = value;
                    DisableAppPatchEnabled = value;
                    DiscordActivityEnabled = value;
                    DiscordActivityJoinEnabled = value;

                    OnPropertyChanged(nameof(ShowServerDetailsEnabled));
                    OnPropertyChanged(nameof(DisableAppPatchEnabled));
                    OnPropertyChanged(nameof(DiscordActivityEnabled));
                    OnPropertyChanged(nameof(DiscordActivityJoinEnabled));
                }
            }
        }

        public bool ShowServerDetailsEnabled
        {
            get => App.Settings.Prop.ShowServerDetails;
            set => App.Settings.Prop.ShowServerDetails = value;
        }

        public bool DiscordActivityEnabled
        {
            get => App.Settings.Prop.UseDiscordRichPresence;
            set
            {
                App.Settings.Prop.UseDiscordRichPresence = value;

                if (!value)
                {
                    DiscordActivityJoinEnabled = value;
                    DiscordAccountOnProfile = value;
                    OnPropertyChanged(nameof(DiscordActivityJoinEnabled));
                    OnPropertyChanged(nameof(DiscordAccountOnProfile));
                }
            }
        }

        public bool DiscordActivityJoinEnabled
        {
            get => !App.Settings.Prop.HideRPCButtons;
            set => App.Settings.Prop.HideRPCButtons = !value;
        }

        public bool DiscordAccountOnProfile
        {
            get => App.Settings.Prop.ShowAccountOnRichPresence;
            set => App.Settings.Prop.ShowAccountOnRichPresence = value;
        }

        public bool DisableAppPatchEnabled
        {
            get => App.Settings.Prop.UseDisableAppPatch;
            set => App.Settings.Prop.UseDisableAppPatch = value;
        }
        public ObservableCollection<CustomIntegration> CustomIntegrations
        {
            get => App.Settings.Prop.CustomIntegrations;
            set => App.Settings.Prop.CustomIntegrations = value;
        }

        public CustomIntegration? SelectedCustomIntegration { get; set; }
        public int SelectedCustomIntegrationIndex { get; set; }
        public bool IsCustomIntegrationSelected => SelectedCustomIntegration is not null;

        public IEnumerable<RegionPreferenceOption> RegionPreferenceOptions => new RegionPreferenceOption[]
        {
            new(RobloxRegionPreference.Auto, Strings.Menu_Integrations_Region_Value_Auto),
            new(RobloxRegionPreference.Americas, Strings.Menu_Integrations_Region_Value_Americas),
            new(RobloxRegionPreference.Europe, Strings.Menu_Integrations_Region_Value_Europe),
            new(RobloxRegionPreference.AsiaPacific, Strings.Menu_Integrations_Region_Value_AsiaPacific),
            new(RobloxRegionPreference.Oceania, Strings.Menu_Integrations_Region_Value_Oceania),
            new(RobloxRegionPreference.MiddleEastAfrica, Strings.Menu_Integrations_Region_Value_MiddleEastAfrica),
        };

        public RobloxRegionPreference PreferredRobloxRegion
        {
            get => App.Settings.Prop.PreferredRobloxRegion;
            set
            {
                App.Settings.Prop.PreferredRobloxRegion = value;
                OnPropertyChanged(nameof(PreferredRobloxRegion));
                OnPropertyChanged(nameof(IsManualRegionPinned));
                OnPropertyChanged(nameof(IsRegionToastApplicable));
            }
        }

        public bool IsManualRegionPinned => PreferredRobloxRegion != RobloxRegionPreference.Auto;

        public bool IsRegionToastApplicable => PreferredRobloxRegion == RobloxRegionPreference.Auto;

        public bool OfferLowPingServerPickerWhenRegionPinned
        {
            get => App.Settings.Prop.OfferLowPingServerPickerWhenRegionPinned;
            set => App.Settings.Prop.OfferLowPingServerPickerWhenRegionPinned = value;
        }

        public bool ShowRegionDetectionToastOnLaunch
        {
            get => App.Settings.Prop.ShowRegionDetectionToastOnLaunch;
            set => App.Settings.Prop.ShowRegionDetectionToastOnLaunch = value;
        }
    }
}
