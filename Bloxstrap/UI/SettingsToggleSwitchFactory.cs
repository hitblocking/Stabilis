using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Bloxstrap.UI
{
    /// <summary>
    /// Builds WPF UI <see cref="Wpf.Ui.Controls.ToggleSwitch"/> controls for code-behind settings pages.
    /// Code-created switches must use <see cref="DefaultUiToggleSwitchStyle"/> explicitly; otherwise WPF can fall back to
    /// an unstyled <see cref="System.Windows.Controls.Primitives.ToggleButton"/> template (outline / checkbox-like chrome).
    /// </summary>
    public static class SettingsToggleSwitchFactory
    {
        /// <summary>
        /// Applies the themed pill template from application resources (call again after the control is in the visual tree if needed).
        /// </summary>
        public static void ApplyTheme(Wpf.Ui.Controls.ToggleSwitch sw)
        {
            if (Application.Current is not { } app)
                return;

            if (app.TryFindResource("DefaultUiToggleSwitchStyle") is Style baseStyle)
            {
                sw.Style = new Style(typeof(Wpf.Ui.Controls.ToggleSwitch), baseStyle);
                return;
            }

            if (app.TryFindResource(typeof(Wpf.Ui.Controls.ToggleSwitch)) is Style implicitStyle)
                sw.Style = implicitStyle;
        }

        public static Wpf.Ui.Controls.ToggleSwitch Create()
        {
            var sw = new Wpf.Ui.Controls.ToggleSwitch
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 0, 0),
                Focusable = true,
                Cursor = Cursors.Hand
            };
            Panel.SetZIndex(sw, 1);
            ApplyTheme(sw);
            return sw;
        }
    }
}
