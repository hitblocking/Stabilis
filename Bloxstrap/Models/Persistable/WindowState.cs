namespace Bloxstrap.Models.Persistable
{
    public class WindowState
    {
        public double Width { get; set; }

        public double Height { get; set; }

        public double Left { get; set; }

        public double Top { get; set; }

        /// <summary>Whether the settings window was last closed while maximized.</summary>
        public bool Maximized { get; set; }
    }
}
