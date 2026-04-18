using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Data;
using Bloxstrap.Enums;
using Bloxstrap.Resources;
using Bloxstrap.UI.Converters;
using Bloxstrap.UI.ViewModels.Settings;
using Color = System.Windows.Media.Color;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    public partial class PerformancePage : UserControl
    {
        private PerformanceViewModel _viewModel = null!;

        private static readonly SolidColorBrush PanelBlack = new(Color.FromRgb(0, 0, 0));
        private static readonly SolidColorBrush HairlineBorder = new(Color.FromRgb(51, 51, 51));

        private static void StyleSettingsCombo(ComboBox combo)
        {
            combo.MinHeight = 32;
            combo.Padding = new Thickness(10, 6, 10, 6);
            combo.Background = PanelBlack;
            combo.BorderBrush = HairlineBorder;
            combo.BorderThickness = new Thickness(1);
            combo.VerticalContentAlignment = VerticalAlignment.Center;
        }

        public PerformancePage()
        {
            // build UI in code to avoid XAML build generation issues
            var grid = new Grid { Margin = new Thickness(12) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // advanced

            var header = new StackPanel { Orientation = Orientation.Vertical };
            header.Children.Add(new TextBlock { Text = Strings.Menu_Performance_Title, FontSize = 22, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
            header.Children.Add(new TextBlock { Text = Strings.Menu_Performance_Lead, Margin = new Thickness(0, 6, 0, 0), FontSize = 14, Foreground = Brushes.LightGray, TextWrapping = TextWrapping.Wrap });
            Grid.SetRow(header, 0);
            grid.Children.Add(header);

            var combo = new ComboBox { Width = 360, Margin = new Thickness(0, 12, 0, 0), Foreground = Brushes.White };
            StyleSettingsCombo(combo);
            combo.SetBinding(ComboBox.ItemsSourceProperty, new Binding("Profiles"));
            combo.SetBinding(ComboBox.SelectedItemProperty, new Binding("SelectedProfile") { Mode = BindingMode.TwoWay });
            Grid.SetRow(combo, 1);
            grid.Children.Add(combo);

            // Controls row
            var stack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 0) };
            var applyBtn = new Button { Content = "Apply", Foreground = Brushes.White, Padding = new Thickness(12,4,12,4) };
            applyBtn.SetBinding(Button.CommandProperty, new Binding("ApplyCommand"));
            var resetBtn = new Button { Content = "Reset", Margin = new Thickness(8, 0, 0, 0), Foreground = Brushes.White, Padding = new Thickness(12,4,12,4) };
            resetBtn.SetBinding(Button.CommandProperty, new Binding("ResetCommand"));
            stack.Children.Add(applyBtn);
            stack.Children.Add(resetBtn);
            Grid.SetRow(stack, 2);
            grid.Children.Add(stack);

            // Advanced settings (more than just a preset)
            var advPanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 18, 0, 0) };
            var advTitle = new TextBlock { Text = "Advanced Settings", FontWeight = FontWeights.SemiBold, Foreground = Brushes.White };
            advPanel.Children.Add(advTitle);

            Border MakeRuntimeToggleRow(string title, string subtitle, string bindingPath)
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

            // Manual FPS + cap (writes GlobalBasicSettings FramerateCap + FastFlags for launch compatibility)
            var overridePanel = new Grid { Margin = new Thickness(0, 8, 0, 0) };
            overridePanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            overridePanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var manualLabel = new TextBlock { Text = "Manual FPS Override", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold };
            var manualSw = SettingsToggleSwitchFactory.Create();
            manualSw.SetBinding(ToggleButton.IsCheckedProperty, new Binding(nameof(PerformanceViewModel.ManualOverride)) { Mode = BindingMode.TwoWay });
            Grid.SetColumn(manualLabel, 0);
            Grid.SetColumn(manualSw, 1);
            overridePanel.Children.Add(manualLabel);
            overridePanel.Children.Add(manualSw);

            // FPS cap input
            var fpsPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0,8,0,0) };
            var fpsLabel = new TextBlock { Text = "FPS Cap:", VerticalAlignment = VerticalAlignment.Center, Foreground = Brushes.White };
            var fpsBox = new TextBox { Width = 80, Margin = new Thickness(8,0,0,0), Foreground = Brushes.White, Background = Brushes.Transparent };
            fpsBox.SetBinding(TextBox.TextProperty, new Binding("FPSCap")
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Converter = new NullableIntToStringConverter()
            });
            var suggested = new TextBlock { Margin = new Thickness(12,0,0,0), VerticalAlignment = VerticalAlignment.Center, Foreground = Brushes.White };
            suggested.SetBinding(TextBlock.TextProperty, new Binding("SuggestedFPS") { StringFormat = "Suggested: {0} FPS" });
            fpsPanel.Children.Add(fpsLabel);
            fpsPanel.Children.Add(fpsBox);
            fpsPanel.Children.Add(suggested);

            var inGameExpander = new Expander
            {
                Header = "In-game Roblox settings (GlobalBasicSettings.xml)",
                IsExpanded = true,
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                Margin = new Thickness(0, 12, 0, 0),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold
            };
            var inGameStack = new StackPanel { Orientation = Orientation.Vertical };
            inGameStack.Children.Add(new TextBlock
            {
                Text = "These options mirror Escape → Settings (not FastFlags). Run Roblox once so the file exists; some keys appear after you open the in-game settings menu at least once.",
                Foreground = Brushes.LightGray,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            });
            overridePanel.Margin = new Thickness(0, 0, 0, 0);
            inGameStack.Children.Add(overridePanel);
            inGameStack.Children.Add(fpsPanel);
            inGameStack.Children.Add(MakeRuntimeToggleRow(
                "Sync this file when Stabilis starts and before Play",
                "When off, Stabilis only writes GlobalBasicSettings when you change options here and use Apply or Save—so your last in-game Escape menu edits are not overwritten on launch.",
                nameof(PerformanceViewModel.RobloxXmlSyncOnLaunchAndStartup)));
            inGameStack.Children.Add(MakeRuntimeToggleRow(
                "Turn off Performance Stats",
                "Sets Performance Stats to Off in Roblox’s menu (saves overlay work).",
                nameof(PerformanceViewModel.RobloxXmlForcePerformanceStatsOff)));
            inGameStack.Children.Add(MakeRuntimeToggleRow(
                "Graphics Mode: Automatic",
                "Sets Graphics Mode to Automatic (SavedQualityLevel = 0) so Roblox scales quality to performance.",
                nameof(PerformanceViewModel.RobloxXmlGraphicsQualityAutomatic)));
            inGameStack.Children.Add(MakeRuntimeToggleRow(
                "Disable automatic chat translation",
                "Sets Automatic Chat Translation off (ChatTranslationEnabled = false).",
                nameof(PerformanceViewModel.RobloxXmlDisableChatTranslation)));
            inGameExpander.Content = inGameStack;
            advPanel.Children.Add(inGameExpander);

            var launchExpander = new Expander
            {
                Header = "When Stabilis launches Roblox (Windows)",
                IsExpanded = true,
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                Margin = new Thickness(0, 12, 0, 0),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold
            };
            var launchStack = new StackPanel { Orientation = Orientation.Vertical };
            launchStack.Children.Add(new TextBlock
            {
                Text = "Process priority and CPU affinity apply when Roblox starts from Stabilis. GPU preference and fullscreen optimizations are written when you save settings.",
                Foreground = Brushes.LightGray,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            });
            var cpuLine = new TextBlock
            {
                Foreground = Brushes.White,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 4)
            };
            cpuLine.SetBinding(TextBlock.TextProperty, new Binding(nameof(PerformanceViewModel.CpuModel)));
            launchStack.Children.Add(cpuLine);
            var affinityLine = new TextBlock
            {
                Foreground = Brushes.LightGray,
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            };
            affinityLine.SetBinding(TextBlock.TextProperty, new Binding(nameof(PerformanceViewModel.TargetAffinityCoreCount)) { StringFormat = "Current affinity target: {0} logical cores (see mode below)." });
            launchStack.Children.Add(affinityLine);

            var priorityBorder = new Border { Background = PanelBlack, BorderBrush = HairlineBorder, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6), Padding = new Thickness(12), Margin = new Thickness(0, 0, 0, 6) };
            var priorityGrid = new Grid();
            priorityGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            priorityGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var priorityText = new StackPanel { Orientation = Orientation.Vertical };
            priorityText.Children.Add(new TextBlock { Text = "Process priority", Foreground = Brushes.White, FontWeight = FontWeights.SemiBold });
            priorityText.Children.Add(new TextBlock { Text = "Applied to Roblox when launched from Stabilis.", Foreground = Brushes.LightGray, FontSize = 12 });
            var priorityCombo = new ComboBox { Width = 220, Margin = new Thickness(12, 0, 0, 0), Foreground = Brushes.White };
            StyleSettingsCombo(priorityCombo);
            priorityCombo.ItemsSource = PerformanceViewModel.RobloxProcessPriorityChoices;
            priorityCombo.DisplayMemberPath = nameof(LabeledEnumChoice<RobloxProcessPriority>.Display);
            priorityCombo.SelectedValuePath = nameof(LabeledEnumChoice<RobloxProcessPriority>.Value);
            priorityCombo.SetBinding(ComboBox.SelectedValueProperty, new Binding(nameof(PerformanceViewModel.RobloxProcessPriority)) { Mode = BindingMode.TwoWay });
            Grid.SetColumn(priorityText, 0);
            Grid.SetColumn(priorityCombo, 1);
            priorityGrid.Children.Add(priorityText);
            priorityGrid.Children.Add(priorityCombo);
            priorityBorder.Child = priorityGrid;
            launchStack.Children.Add(priorityBorder);

            var affinityBorder = new Border { Background = PanelBlack, BorderBrush = HairlineBorder, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6), Padding = new Thickness(12), Margin = new Thickness(0, 0, 0, 6) };
            var affinityGrid = new Grid();
            affinityGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            affinityGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var affinityText = new StackPanel { Orientation = Orientation.Vertical };
            affinityText.Children.Add(new TextBlock { Text = "CPU affinity", Foreground = Brushes.White, FontWeight = FontWeights.SemiBold });
            affinityText.Children.Add(new TextBlock { Text = "Limits how many logical cores Roblox may use.", Foreground = Brushes.LightGray, FontSize = 12 });
            var affinityCombo = new ComboBox { Width = 260, Margin = new Thickness(12, 0, 0, 0), Foreground = Brushes.White };
            StyleSettingsCombo(affinityCombo);
            affinityCombo.ItemsSource = PerformanceViewModel.RobloxAffinityModeChoices;
            affinityCombo.DisplayMemberPath = nameof(LabeledEnumChoice<RobloxAffinityMode>.Display);
            affinityCombo.SelectedValuePath = nameof(LabeledEnumChoice<RobloxAffinityMode>.Value);
            affinityCombo.SetBinding(ComboBox.SelectedValueProperty, new Binding(nameof(PerformanceViewModel.RobloxAffinityMode)) { Mode = BindingMode.TwoWay });
            Grid.SetColumn(affinityText, 0);
            Grid.SetColumn(affinityCombo, 1);
            affinityGrid.Children.Add(affinityText);
            affinityGrid.Children.Add(affinityCombo);
            affinityBorder.Child = affinityGrid;
            launchStack.Children.Add(affinityBorder);

            launchStack.Children.Add(MakeRuntimeToggleRow(
                "Boost priority when Roblox has focus",
                "Uses Windows PriorityBoost when the game window is focused. Does not use FastFlags.",
                nameof(PerformanceViewModel.RobloxRuntimePriorityBoost)));
            launchStack.Children.Add(MakeRuntimeToggleRow(
                "Disable Windows power throttling (EcoQoS)",
                "Turns off execution-speed throttling for the Roblox process. Does not use FastFlags.",
                nameof(PerformanceViewModel.RobloxRuntimeDisablePowerThrottling)));
            launchStack.Children.Add(MakeRuntimeToggleRow(
                "Prefer high-performance GPU",
                "Sets the per-app DirectX GPU preference (UserGpuPreferences). Useful on laptops with hybrid graphics.",
                nameof(PerformanceViewModel.RobloxShellGpuHighPerformance)));
            launchStack.Children.Add(MakeRuntimeToggleRow(
                "Disable fullscreen optimizations",
                "Adds the Windows compatibility layer flag for the Roblox executables. Can reduce latency in borderless/windowed setups.",
                nameof(PerformanceViewModel.RobloxShellDisableFullscreenOptimizations)));

            launchExpander.Content = launchStack;
            advPanel.Children.Add(launchExpander);

            // Memory & cleanup: collapsible section + working dropdowns (stable ItemsSource, non-transparent combo chrome)
            var memoryCleanupExpander = new Expander
            {
                Header = "Memory & cleanup",
                IsExpanded = true,
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                Margin = new Thickness(0, 18, 0, 0),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold
            };
            var memoryCleanupStack = new StackPanel { Orientation = Orientation.Vertical };

            var memoryTrimBorder = new Border { Background = PanelBlack, BorderBrush = HairlineBorder, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6), Padding = new Thickness(12), Margin = new Thickness(0, 0, 0, 6) };
            var memoryTrimGrid = new Grid { ClipToBounds = true };
            memoryTrimGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            memoryTrimGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var memoryTextPanel = new StackPanel { Orientation = Orientation.Vertical };
            memoryTextPanel.Children.Add(new TextBlock { Text = "Memory trim", Foreground = Brushes.White, FontWeight = FontWeights.SemiBold });
            memoryTextPanel.Children.Add(new TextBlock { Text = "Periodically reclaim memory from the Roblox process while it runs.", Foreground = Brushes.LightGray, FontSize = 12, TextWrapping = TextWrapping.Wrap });
            var memorySw = SettingsToggleSwitchFactory.Create();
            memorySw.SetBinding(ToggleButton.IsCheckedProperty, new Binding(nameof(PerformanceViewModel.MemoryTrimEnabled)) { Mode = BindingMode.TwoWay });
            Grid.SetColumn(memoryTextPanel, 0);
            Grid.SetColumn(memorySw, 1);
            memoryTrimGrid.Children.Add(memoryTextPanel);
            memoryTrimGrid.Children.Add(memorySw);
            memoryTrimBorder.Child = memoryTrimGrid;
            memoryCleanupStack.Children.Add(memoryTrimBorder);

            var intervalBorder = new Border { Background = PanelBlack, BorderBrush = HairlineBorder, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6), Padding = new Thickness(12), Margin = new Thickness(0, 0, 0, 6) };
            var intervalGrid = new Grid();
            intervalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            intervalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var intervalTextPanel = new StackPanel { Orientation = Orientation.Vertical };
            intervalTextPanel.Children.Add(new TextBlock { Text = "Trim interval", Foreground = Brushes.White, FontWeight = FontWeights.SemiBold });
            intervalTextPanel.Children.Add(new TextBlock { Text = "How often to run while memory trim is enabled.", Foreground = Brushes.LightGray, FontSize = 12 });
            var intervalCombo = new ComboBox { Width = 200, Margin = new Thickness(12, 0, 0, 0), Foreground = Brushes.White };
            StyleSettingsCombo(intervalCombo);
            intervalCombo.ItemsSource = PerformanceViewModel.MemoryTrimIntervalChoices;
            intervalCombo.DisplayMemberPath = nameof(LabeledIntChoice.Display);
            intervalCombo.SelectedValuePath = nameof(LabeledIntChoice.Value);
            intervalCombo.SetBinding(ComboBox.SelectedValueProperty, new Binding(nameof(PerformanceViewModel.MemoryTrimIntervalPick)) { Mode = BindingMode.TwoWay });
            intervalCombo.SetBinding(UIElement.IsEnabledProperty, new Binding(nameof(PerformanceViewModel.MemoryTrimEnabled)));
            Grid.SetColumn(intervalTextPanel, 0);
            Grid.SetColumn(intervalCombo, 1);
            intervalGrid.Children.Add(intervalTextPanel);
            intervalGrid.Children.Add(intervalCombo);
            intervalBorder.Child = intervalGrid;
            memoryCleanupStack.Children.Add(intervalBorder);

            // Cleaner card (dropdown + toggles)
            var cleanerBorder = new Border { Background = PanelBlack, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6), Padding = new Thickness(10), Margin = new Thickness(0, 0, 0, 0) };
            cleanerBorder.BorderBrush = HairlineBorder;
            var cleanerStack = new StackPanel { Orientation = Orientation.Vertical };

            var cleanerTitle = new TextBlock { Text = "Disk cleaner", FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, FontSize = 14 };
            var cleanerDesc = new TextBlock { Text = "Remove old cache and log files to save disk space.", Foreground = Brushes.LightGray, Margin = new Thickness(0, 4, 0, 8) };
            cleanerStack.Children.Add(cleanerTitle);
            cleanerStack.Children.Add(cleanerDesc);

            // Row: retention dropdown
            var retentionBorder = new Border { Background = PanelBlack, BorderBrush = HairlineBorder, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6), Padding = new Thickness(12), Margin = new Thickness(0, 6, 0, 6) };
            var retentionGrid = new Grid();
            retentionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            retentionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var retentionTextPanel = new StackPanel { Orientation = Orientation.Vertical };
            var retentionTitle = new TextBlock { Text = "Delete files older than", Foreground = Brushes.White, FontWeight = FontWeights.SemiBold };
            var retentionSub = new TextBlock { Text = "Applies when you run cleanup or auto-clean on apply.", Foreground = Brushes.LightGray, FontSize = 12 };
            retentionTextPanel.Children.Add(retentionTitle);
            retentionTextPanel.Children.Add(retentionSub);

            var retentionCombo = new ComboBox { Width = 220, Margin = new Thickness(12, 0, 0, 0), Foreground = Brushes.White };
            StyleSettingsCombo(retentionCombo);
            retentionCombo.ItemsSource = PerformanceViewModel.CleanerRetentionChoices;
            retentionCombo.DisplayMemberPath = nameof(LabeledIntChoice.Display);
            retentionCombo.SelectedValuePath = nameof(LabeledIntChoice.Value);
            retentionCombo.SetBinding(ComboBox.SelectedValueProperty, new Binding(nameof(PerformanceViewModel.PerformanceCleanerRetentionDays)) { Mode = BindingMode.TwoWay });

            Grid.SetColumn(retentionTextPanel, 0);
            Grid.SetColumn(retentionCombo, 1);
            retentionGrid.Children.Add(retentionTextPanel);
            retentionGrid.Children.Add(retentionCombo);
            retentionBorder.Child = retentionGrid;
            cleanerStack.Children.Add(retentionBorder);

            // Helper to create cleaner option rows (ToggleSwitch — same as runtime / advanced)
            Border MakeToggleRow(string title, string subtitle, string bindingPath)
            {
                var b = new Border { Background = PanelBlack, BorderBrush = HairlineBorder, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6), Padding = new Thickness(12), Margin = new Thickness(0,6,0,6) };
                var g = new Grid { ClipToBounds = true };
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var tp = new StackPanel { Orientation = Orientation.Vertical };
                tp.Children.Add(new TextBlock { Text = title, Foreground = Brushes.White, FontWeight = FontWeights.SemiBold });
                tp.Children.Add(new TextBlock { Text = subtitle, Foreground = Brushes.LightGray, FontSize = 12 });

                var sw = SettingsToggleSwitchFactory.Create();
                sw.SetBinding(ToggleButton.IsCheckedProperty, new Binding(bindingPath) { Mode = BindingMode.TwoWay });

                Grid.SetColumn(tp, 0);
                Grid.SetColumn(sw, 1);
                g.Children.Add(tp);
                g.Children.Add(sw);
                b.Child = g;
                return b;
            }

            cleanerStack.Children.Add(MakeToggleRow("Cache", "Old downloads will be deleted.", "PerformanceCleanerCache"));
            cleanerStack.Children.Add(MakeToggleRow("Logs", "Old log files will be deleted.", "PerformanceCleanerLogs"));
            cleanerStack.Children.Add(MakeToggleRow($"{Bloxstrap.App.ProjectName} logs", $"{Bloxstrap.App.ProjectName} logs will be deleted.", "PerformanceCleanerAppLogs"));

            cleanerBorder.Child = cleanerStack;
            memoryCleanupStack.Children.Add(cleanerBorder);
            memoryCleanupExpander.Content = memoryCleanupStack;
            advPanel.Children.Add(memoryCleanupExpander);

            var advancedPerformanceExpander = new Expander
            {
                Header = "Advanced Performance",
                IsExpanded = false,
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                Margin = new Thickness(0, 12, 0, 0),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold
            };

            var advancedPerformanceStack = new StackPanel { Orientation = Orientation.Vertical };

            Border MakeAdvancedToggleRow(string title, string subtitle, string bindingPath)
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

            advancedPerformanceStack.Children.Add(
                MakeAdvancedToggleRow(
                    "Prefer Direct3D 11",
                    "Forces Roblox to prefer D3D11, which can reduce frame-time spikes on some GPUs.",
                    nameof(PerformanceViewModel.AdvancedPreferD3D11)
                )
            );

            advancedPerformanceStack.Children.Add(
                MakeAdvancedToggleRow(
                    "Disable terrain grass rendering",
                    "Removes dynamic grass rendering overhead to improve FPS stability.",
                    nameof(PerformanceViewModel.AdvancedDisableGrass)
                )
            );

            advancedPerformanceStack.Children.Add(
                MakeAdvancedToggleRow(
                    "Pause terrain voxelizer",
                    "Reduces terrain update cost in-game; useful when frequent terrain updates cause stutter.",
                    nameof(PerformanceViewModel.AdvancedPauseVoxelizer)
                )
            );

            advancedPerformanceStack.Children.Add(
                MakeAdvancedToggleRow(
                    "Force low texture quality",
                    "Uses lower texture quality to reduce VRAM pressure and frame pacing spikes.",
                    nameof(PerformanceViewModel.AdvancedLowTextureQuality)
                )
            );

            advancedPerformanceExpander.Content = advancedPerformanceStack;
            advPanel.Children.Add(advancedPerformanceExpander);

            Grid.SetRow(advPanel, 3);
            grid.Children.Add(advPanel);

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                CanContentScroll = false
            };
            scrollViewer.Content = grid;
            this.Content = scrollViewer;

            _viewModel = new PerformanceViewModel();
            this.DataContext = _viewModel;

            this.Loaded += (_, _) => ApplyToggleSwitchThemeToVisualTree(this);
        }

        private static void ApplyToggleSwitchThemeToVisualTree(DependencyObject root)
        {
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is Wpf.Ui.Controls.ToggleSwitch sw)
                    SettingsToggleSwitchFactory.ApplyTheme(sw);
                ApplyToggleSwitchThemeToVisualTree(child);
            }
        }
    }
}

