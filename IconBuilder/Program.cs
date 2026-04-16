using ImageMagick;

// Builds a crisp in-app PNG + multi-size ICOs for Windows shell (taskbar, title bar).
// Run from repo root: dotnet run --project IconBuilder/IconBuilder.csproj

var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
var bloxstrap = Path.Combine(repoRoot, "Bloxstrap");
var pngPath = Path.Combine(bloxstrap, "Resources", "StabilisLogo.png");
var icoApp = Path.Combine(bloxstrap, "Bloxstrap.ico");
var icoEmb = Path.Combine(bloxstrap, "Resources", "IconBloxstrap.ico");

if (!File.Exists(pngPath))
{
    Console.Error.WriteLine($"Missing: {pngPath}");
    return 1;
}

// High-res PNG for WPF Image (downscale = sharp). Lanczos reduces mushy upscales.
const int logoPx = 1024;
using (var hi = new MagickImage(pngPath))
{
    hi.FilterType = FilterType.Lanczos;
    hi.Resize(new MagickGeometry(logoPx, logoPx) { IgnoreAspectRatio = true });
    hi.Write(pngPath);
    Console.WriteLine($"Wrote {logoPx}x{logoPx} {pngPath}");
}

// Multi-size ICO for Explorer / taskbar / HWND icon
var sizes = new[] { 256, 128, 96, 64, 48, 32, 24, 16 };
using var collection = new MagickImageCollection();
foreach (var s in sizes)
{
    using var layer = new MagickImage(pngPath);
    layer.FilterType = FilterType.Lanczos;
    layer.Resize(new MagickGeometry(s, s) { IgnoreAspectRatio = true });
    collection.Add(layer.Clone());
}

collection.Write(icoApp);
File.Copy(icoApp, icoEmb, overwrite: true);
Console.WriteLine($"Wrote {icoApp}");
Console.WriteLine($"Wrote {icoEmb}");
return 0;
