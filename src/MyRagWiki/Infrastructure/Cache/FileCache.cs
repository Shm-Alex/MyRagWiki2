using System.Text.Json;
using MyRagWiki.Application.Interfaces;

namespace MyRagWiki.Infrastructure.Cache;

public class FileCache : ICache
{
    private readonly string _cacheFolder;
    private readonly Dictionary<string, CacheEntry> _entries;

    public FileCache(string cacheFolder)
    {
        _cacheFolder = cacheFolder;
        Directory.CreateDirectory(_cacheFolder);
        _entries = LoadEntries();
    }

    public string? GetCachedText(string filePath, string contentHash)
    {
        var key = GetKey(filePath, contentHash);
        return _entries.TryGetValue(key, out var entry) ? entry.Text : null;
    }

    public void SaveToCache(string filePath, string contentHash, string text)
    {
        var key = GetKey(filePath, contentHash);
        _entries[key] = new CacheEntry
        {
            FilePath = filePath,
            ContentHash = contentHash,
            Text = text,
            CachedAt = DateTime.UtcNow
        };
        SaveEntries();
    }

    public bool IsCacheValid(string filePath, string contentHash)
    {
        var key = GetKey(filePath, contentHash);
        return _entries.ContainsKey(key);
    }

    private string GetKey(string filePath, string contentHash)
        => $"{filePath}|{contentHash}";

    private Dictionary<string, CacheEntry> LoadEntries()
    {
        var cacheFile = Path.Combine(_cacheFolder, "cache.json");
        if (!File.Exists(cacheFile)) return new();

        var json = File.ReadAllText(cacheFile);
        return JsonSerializer.Deserialize<Dictionary<string, CacheEntry>>(json)
               ?? new();
    }

    private void SaveEntries()
    {
        var cacheFile = Path.Combine(_cacheFolder, "cache.json");
        var json = JsonSerializer.Serialize(_entries, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(cacheFile, json);
    }

    private record CacheEntry
    {
        public string FilePath { get; init; } = "";
        public string ContentHash { get; init; } = "";
        public string Text { get; init; } = "";
        public DateTime CachedAt { get; init; }
    }
}