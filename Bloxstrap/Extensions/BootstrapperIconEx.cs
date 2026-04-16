using System.Drawing;

namespace Bloxstrap.Extensions
{
    static class BootstrapperIconEx
    {
        public static IReadOnlyCollection<BootstrapperIcon> Selections => new BootstrapperIcon[]
        {
            BootstrapperIcon.IconBloxstrap,
            BootstrapperIcon.Icon2022,
            BootstrapperIcon.Icon2019,
            BootstrapperIcon.Icon2017,
            BootstrapperIcon.IconLate2015,
            BootstrapperIcon.IconEarly2015,
            BootstrapperIcon.Icon2011,
            BootstrapperIcon.Icon2008,
            BootstrapperIcon.IconBloxstrapClassic,
            BootstrapperIcon.IconCustom
        };

        // small note on handling icon sizes
        // i'm using multisize icon packs here with sizes 16, 24, 32, 48, 64 and 128
        // use this for generating multisize packs: https://www.aconvert.com/icon/

        private static Icon? TryLoadCustomIcon()
        {
            const string LOG_IDENT = "BootstrapperIconEx::TryLoadCustomIcon";
            string location = App.Settings.Prop.BootstrapperIconCustomLocation;

            if (String.IsNullOrEmpty(location))
            {
                App.Logger.WriteLine(LOG_IDENT, "Warning: custom icon is not set.");
                return null;
            }

            try
            {
                return new Icon(location);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to load custom icon!");
                App.Logger.WriteException(LOG_IDENT, ex);
                return null;
            }
        }

        /// <summary>Full icon set (e.g. Appearance settings preview).</summary>
        public static Icon GetIcon(this BootstrapperIcon icon)
        {
            if (icon == BootstrapperIcon.IconCustom)
                return TryLoadCustomIcon() ?? Properties.Resources.IconBloxstrap;

            return icon switch
            {
                BootstrapperIcon.IconBloxstrap => Properties.Resources.IconBloxstrap,
                BootstrapperIcon.Icon2008 => Properties.Resources.Icon2008,
                BootstrapperIcon.Icon2011 => Properties.Resources.Icon2011,
                BootstrapperIcon.IconEarly2015 => Properties.Resources.IconEarly2015,
                BootstrapperIcon.IconLate2015 => Properties.Resources.IconLate2015,
                BootstrapperIcon.Icon2017 => Properties.Resources.Icon2017,
                BootstrapperIcon.Icon2019 => Properties.Resources.Icon2019,
                BootstrapperIcon.Icon2022 => Properties.Resources.Icon2022,
                BootstrapperIcon.IconBloxstrapClassic => Properties.Resources.IconBloxstrapClassic,
                _ => Properties.Resources.IconBloxstrap
            };
        }

        /// <summary>Icon shown in bootstrapper/installer UI: white branded mark, or the user's custom file when selected.</summary>
        public static Icon GetBootstrapperUiIcon(this BootstrapperIcon icon)
        {
            if (icon == BootstrapperIcon.IconCustom)
                return TryLoadCustomIcon() ?? Properties.Resources.IconBloxstrap;
            return Properties.Resources.IconBloxstrap;
        }
    }
}
