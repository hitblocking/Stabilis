using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Wpf.Ui.Appearance;

namespace Bloxstrap.UI.ViewModels.Bootstrapper
{
    public class FluentDialogViewModel : BootstrapperDialogViewModel
    {
        public BackgroundType WindowBackdropType { get; set; } = BackgroundType.None;

        public SolidColorBrush BackgroundColourBrush { get; set; } = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));

        [Obsolete("Do not use this! This is for the designer only.", true)]
        public FluentDialogViewModel() : base()
        { }

        public FluentDialogViewModel(IBootstrapperDialog dialog, bool aero) : base(dialog)
        {
            // Keep launcher fully opaque black so there is no gray/aero tint.
            WindowBackdropType = BackgroundType.None;
            BackgroundColourBrush = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
        }
    }
}
