using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

using Wpf.Ui.Controls.Interfaces;
using Wpf.Ui.Mvvm.Contracts;

using Bloxstrap.UI.ViewModels.Settings;

namespace Bloxstrap.UI.Elements.Settings
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INavigationWindow
    {
        private Models.Persistable.WindowState _state => App.State.Prop.SettingsWindow;

        public MainWindow(bool showAlreadyRunningWarning)
        {
            var viewModel = new MainWindowViewModel();

            viewModel.RequestSaveNoticeEvent += (_, _) => SettingsSavedSnackbar.Show();
            viewModel.RequestCloseWindowEvent += (_, _) => Close();

            DataContext = viewModel;
            
            InitializeComponent();

            App.Logger.WriteLine("MainWindow", "Initializing settings window");

            if (showAlreadyRunningWarning)
                ShowAlreadyRunningSnackbar();

            LoadState();
        }

        public void LoadState()
        {
            var virtualBounds = new Rect(
                SystemParameters.VirtualScreenLeft,
                SystemParameters.VirtualScreenTop,
                SystemParameters.VirtualScreenWidth,
                SystemParameters.VirtualScreenHeight
            );

            bool validSize = _state.Width > MinWidth && _state.Height > MinHeight;
            if (validSize)
            {
                Width = Math.Min(_state.Width, virtualBounds.Width);
                Height = Math.Min(_state.Height, virtualBounds.Height);
            }

            var savedBounds = new Rect(_state.Left, _state.Top, Math.Max(_state.Width, MinWidth), Math.Max(_state.Height, MinHeight));
            if (savedBounds.IntersectsWith(virtualBounds))
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
                Left = Math.Clamp(_state.Left, virtualBounds.Left, virtualBounds.Right - Width);
                Top = Math.Clamp(_state.Top, virtualBounds.Top, virtualBounds.Bottom - Height);
            }

            if (_state.Maximized)
                this.WindowState = System.Windows.WindowState.Maximized;
        }

        private async void ShowAlreadyRunningSnackbar()
        {
            await Task.Delay(500); // wait for everything to finish loading
            AlreadyRunningSnackbar.Show();
        }

        #region INavigationWindow methods

        public Frame GetFrame() => RootFrame;

        public INavigation GetNavigation() => RootNavigation;

        public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

        public void SetPageService(IPageService pageService) => RootNavigation.PageService = pageService;

        public void ShowWindow() => Show();

        public void CloseWindow() => Close();

        #endregion INavigationWindow methods

        private void WpfUiWindow_Closing(object sender, CancelEventArgs e)
        {
            if (App.FastFlags.Changed || App.PendingSettingTasks.Any())
            {
                var result = Frontend.ShowMessageBox(Strings.Menu_UnsavedChanges, MessageBoxImage.Warning, MessageBoxButton.YesNo);

                if (result != MessageBoxResult.Yes)
                    e.Cancel = true;
            }

            if (e.Cancel)
                return;

            _state.Maximized = this.WindowState == System.Windows.WindowState.Maximized;

            Rect bounds = this.WindowState == System.Windows.WindowState.Normal
                ? new Rect(Left, Top, Width, Height)
                : RestoreBounds;

            if (bounds.Width > 0)
            {
                _state.Width = bounds.Width;
                _state.Height = bounds.Height;
            }

            if (!double.IsNaN(bounds.Left) && !double.IsNaN(bounds.Top) && !double.IsInfinity(bounds.Left) && !double.IsInfinity(bounds.Top))
            {
                _state.Left = bounds.Left;
                _state.Top = bounds.Top;
            }

            App.State.Save();
        }

        private void WpfUiWindow_Closed(object sender, EventArgs e)
        {
            if (App.LaunchSettings.TestModeFlag.Active)
                LaunchHandler.LaunchRoblox(LaunchMode.Player);
            else
                App.SoftTerminate();
        }
    }
}
