using MyRagWiki.Application.Interfaces;
using MyRagWiki.Domain;

namespace MyRagWiki.Infrastructure.Parsers;

public class MarkdownParser : IDocumentParser
{
    public DocumentType SupportedType => DocumentType.Markdown;

    public string ExtractText(string filePath) => File.ReadAllText(filePath);
}