using System.Drawing;

// Converts Bloxstrap.ico and Resources/IconBloxstrap.ico to white (preserves alpha / logo shape).
// Run: dotnet run --project Bloxstrap/tools/WhiteAppIcon/WhiteAppIcon.csproj

var bloxstrapProject = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var paths = new[]
{
    Path.Combine(bloxstrapProject, "Bloxstrap.ico"),
    Path.Combine(bloxstrapProject, "Resources", "IconBloxstrap.ico"),
};

foreach (var path in paths)
{
    if (!File.Exists(path))
    {
        Console.WriteLine($"Skip (missing): {path}");
        continue;
    }

    using var readMs = new MemoryStream(File.ReadAllBytes(path));
    using var icon = new Icon(readMs);
    using var bmp = icon.ToBitmap();

    for (int y = 0; y < bmp.Height; y++)
    {
        for (int x = 0; x < bmp.Width; x++)
        {
            var c = bmp.GetPixel(x, y);
            if (c.A == 0)
                continue;
            bmp.SetPixel(x, y, Color.FromArgb(c.A, 255, 255, 255));
        }
    }

    var h = bmp.GetHicon();
    try
    {
        using var fromHandle = Icon.FromHandle(h);
        using var newIcon = (Icon)fromHandle.Clone();
        var tmp = path + ".new.ico";
        using (var outFs = File.Create(tmp))
            newIcon.Save(outFs);

        File.Delete(path);
        File.Move(tmp, path);
        Console.WriteLine($"Updated: {path}");
    }
    finally
    {
        NativeMethods.DestroyIcon(h);
    }
}
