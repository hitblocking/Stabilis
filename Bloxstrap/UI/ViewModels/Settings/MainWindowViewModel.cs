using System.Windows;
using System.Windows.Input;
using Bloxstrap.UI.Elements.About;
using CommunityToolkit.Mvvm.Input;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class MainWindowViewModel : NotifyPropertyChangedViewModel
    {
        public ICommand OpenAboutCommand => new RelayCommand(OpenAbout);
        
        public ICommand SaveSettingsCommand => new RelayCommand(SaveSettings);

        public ICommand SaveAndLaunchCommand => new RelayCommand(SaveAndLaunch);
        
        public ICommand CloseWindowCommand => new RelayCommand(CloseWindow);

        public string SaveAndLaunchButtonText => App.IsPlayerInstalled
            ? Strings.LaunchMenu_SaveAndLaunchRoblox
            : Strings.LaunchMenu_InstallAndSave;

        public EventHandler? RequestSaveNoticeEvent;
        
        public EventHandler? RequestCloseWindowEvent;

        public bool TestModeEnabled
        {
            get => App.LaunchSettings.TestModeFlag.Active;
            set
            {
                if (value)
                {
                    var result = Frontend.ShowMessageBox(Strings.Menu_TestMode_Prompt, MessageBoxImage.Information, MessageBoxButton.YesNo);

                    if (result != MessageBoxResult.Yes)
                        return;
                }

                App.LaunchSettings.TestModeFlag.Active = value;
            }
        }

        private void OpenAbout() => new MainWindow().ShowDialog();

        private void CloseWindow() => RequestCloseWindowEvent?.Invoke(this, EventArgs.Empty);

        private void SaveSettings()
        {
            LaunchHandler.SaveApplicationSettings();
            RequestSaveNoticeEvent?.Invoke(this, EventArgs.Empty);
        }

        private void SaveAndLaunch()
        {
            LaunchHandler.SaveApplicationSettings();
            LaunchHandler.LaunchRoblox(LaunchMode.Player);
        }
    }
}
