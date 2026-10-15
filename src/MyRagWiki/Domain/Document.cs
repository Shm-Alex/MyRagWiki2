namespace MyRagWiki.Domain;

public class Document
{
    public string FilePath { get; init; }
    public string FileName { get; init; }
    public DocumentType Type { get; init; }
    public long FileSize { get; init; }
    public string ContentHash { get; init; }
    public DateTime LastModified { get; init; }

    public Document(string filePath, DocumentType type, string contentHash, long fileSize, DateTime lastModified)
    {
        FilePath = filePath;
        FileName = Path.GetFileName(filePath);
        Type = type;
        ContentHash = contentHash;
        FileSize = fileSize;
        LastModified = lastModified;
    }
}