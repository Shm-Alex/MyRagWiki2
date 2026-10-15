namespace MyRagWiki.Application.Interfaces;

public interface ICache
{
    string? GetCachedText(string filePath, string contentHash);
    void SaveToCache(string filePath, string contentHash, string text);
    bool IsCacheValid(string filePath, string contentHash);
}