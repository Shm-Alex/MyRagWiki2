namespace MyRagWiki.Domain;

public class DocumentChunk
{
    public string Id { get; init; }
    public string SourceFile { get; init; }
    public string Header { get; init; }
    public string Text { get; init; }
    public float[] Embedding { get; set; } = Array.Empty<float>();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public DocumentChunk(string id, string sourceFile, string header, string text)
    {
        Id = id;
        SourceFile = sourceFile;
        Header = header;
        Text = text;
    }
}