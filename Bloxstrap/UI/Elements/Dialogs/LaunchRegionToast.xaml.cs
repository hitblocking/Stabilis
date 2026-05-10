using System.Windows;

using Bloxstrap.UI.Elements.Base;

namespace Bloxstrap.UI.Elements.Dialogs
{
    public partial class LaunchRegionToast : WpfUiWindow
    {
        public LaunchRegionToast(string bucketLine, string? networkDetailLine)
        {
            InitializeComponent();
            BucketLineText.Text = bucketLine;

            if (string.IsNullOrEmpty(networkDetailLine))
                DetailLineText.Visibility = Visibility.Collapsed;
            else
                DetailLineText.Text = networkDetailLine;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Left = SystemParameters.WorkArea.Right - ActualWidth - 24;
            Top = SystemParameters.WorkArea.Bottom - ActualHeight - 24;
        }
    }
}
