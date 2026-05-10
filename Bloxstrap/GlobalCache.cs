namespace Bloxstrap
{
    public readonly record struct ServerGeoCacheEntry(string? Location, string? Region);

    public static class GlobalCache
    {
        public static readonly Dictionary<string, ServerGeoCacheEntry> ServerGeoByIp = new();
    }
}
