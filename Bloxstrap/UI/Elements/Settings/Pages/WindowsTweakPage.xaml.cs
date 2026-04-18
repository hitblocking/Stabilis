using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Data;
using Bloxstrap.Resources;
using Bloxstrap.UI.ViewModels.Settings;
using Color = System.Windows.Media.Color;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    public class WindowsTweakPage : UserControl
    {
        private static readonly SolidColorBrush PanelBlack = new(Color.FromRgb(0, 0, 0));
        private static readonly SolidColorBrush HairlineBorder = new(Color.FromRgb(51, 51, 51));

        public WindowsTweakPage()
        {
            var grid = new Grid { Margin = new Thickness(12) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var header = new StackPanel { Orientation = Orientation.Vertical };
            header.Children.Add(new TextBlock
            {
                Text = Strings.Menu_WindowsTweak_Title,
                FontSize = 22,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White
            });
            header.Children.Add(new TextBlock
            {
                Text = Strings.Menu_WindowsTweak_Lead,
                Margin = new Thickness(0, 6, 0, 0),
                FontSize = 14,
                Foreground = Brushes.LightGray,
                TextWrapping = TextWrapping.Wrap
            });
            Grid.SetRow(header, 0);
            grid.Children.Add(header);

            var stack = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 18, 0, 0) };
            stack.Children.Add(new TextBlock
            {
                Text = "These apply HKLM (system) settings: multimedia scheduling, TCP latency, and game task priority. Run Stabilis as Administrator or writes are skipped (see log). Affects all apps using TCP / multimedia scheduling—turn off if anything misbehaves.",
                Foreground = Brushes.LightGray,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12)
            });

            Border MakeToggleRow(string title, string subtitle, string bindingPath)
            {
                var border = new Border
                {
                    Background = PanelBlack,
                    BorderBrush = HairlineBorder,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(12),
                    Margin = new Thickness(0, 6, 0, 6)
                };

                var row = new Grid { ClipToBounds = true };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var textPanel = new StackPanel { Orientation = Orientation.Vertical };
                textPanel.Children.Add(new TextBlock { Text = title, Foreground = Brushes.White, FontWeight = FontWeights.SemiBold });
                textPanel.Children.Add(new TextBlock { Text = subtitle, Foreground = Brushes.LightGray, FontSize = 12, TextWrapping = TextWrapping.Wrap });

                var sw = SettingsToggleSwitchFactory.Create();
                sw.SetBinding(ToggleButton.IsCheckedProperty, new Binding(bindingPath) { Mode = BindingMode.TwoWay });

                Grid.SetColumn(textPanel, 0);
                Grid.SetColumn(sw, 1);
                row.Children.Add(textPanel);
                row.Children.Add(sw);
                border.Child = row;

                return border;
            }

            stack.Children.Add(MakeToggleRow(
                "Multimedia system profile (network throttling + responsiveness)",
                "HKLM …\\Multimedia\\SystemProfile: disables MMCSS network throttling (0xFFFFFFFF) and sets SystemResponsiveness to 10 for snappier foreground work. Off restores indexed throttling (10) and default responsiveness (20).",
                nameof(PerformanceViewModel.PerformanceWinTweakMultimediaSystemProfile)));
            stack.Children.Add(MakeToggleRow(
                "TCP latency (TcpAckFrequency + TCPNoDelay)",
                "HKLM …\\Tcpip\\Parameters: TcpAckFrequency=1 and TCPNoDelay=1 reduce delayed ACKs / Nagle behavior for lower latency on TCP (online games, etc.). System-wide. Off removes these values so Windows defaults apply.",
                nameof(PerformanceViewModel.PerformanceWinTweakTcpLatency)));
            stack.Children.Add(MakeToggleRow(
                "MMCSS Games scheduling (priority + High category)",
                "HKLM …\\Multimedia\\SystemProfile\\Tasks\\Games: raises Priority, Scheduling Category, and SFIO priority for the Games multimedia class. Off restores typical Medium/Normal defaults.",
                nameof(PerformanceViewModel.PerformanceWinTweakMmcssGames)));
            stack.Children.Add(MakeToggleRow(
                "GPU TDR delay (display driver stability)",
                "HKLM …\\Control\\GraphicsDrivers: sets TdrDelay to 10 seconds before Windows resets the GPU after a detected hang—can reduce spurious “display driver stopped” events under heavy 3D load. Off restores the usual 2-second default.",
                nameof(PerformanceViewModel.PerformanceWinTweakGraphicsTdrDelay)));
            stack.Children.Add(MakeToggleRow(
                "NTFS: don’t update last-access time on files",
                "HKLM …\\Control\\FileSystem: NtfsDisableLastAccessUpdate=1 avoids writing last-access timestamps on every file read—less disk churn during large patches and installs. Off removes the value so Windows uses its default policy.",
                nameof(PerformanceViewModel.PerformanceWinTweakNtfsNoLastAccess)));
            stack.Children.Add(MakeToggleRow(
                "Foreground CPU scheduling (Win32PrioritySeparation)",
                "HKLM …\\Control\\PriorityControl: uses a shorter variable quantum (38) so interactive/foreground threads tend to get CPU sooner—can smooth hitching when background tasks compete. Off restores the typical client default (2).",
                nameof(PerformanceViewModel.PerformanceWinTweakWin32PriorityForeground)));

            Grid.SetRow(stack, 1);
            grid.Children.Add(stack);

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                CanContentScroll = false
            };
            scrollViewer.Content = grid;
            Content = scrollViewer;

            DataContext = new PerformanceViewModel();
            Loaded += (_, _) => ApplyToggleSwitchThemeToVisualTree(this);
        }

        private static void ApplyToggleSwitchThemeToVisualTree(DependencyObject root)
        {
            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
                if (child is Wpf.Ui.Controls.ToggleSwitch sw)
                    SettingsToggleSwitchFactory.ApplyTheme(sw);
                ApplyToggleSwitchThemeToVisualTree(child);
            }
        }
    }
}
