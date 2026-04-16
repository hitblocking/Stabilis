using System.Runtime.InteropServices;

internal static class NativeMethods
{
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    internal static extern bool DestroyIcon(IntPtr handle);
}
