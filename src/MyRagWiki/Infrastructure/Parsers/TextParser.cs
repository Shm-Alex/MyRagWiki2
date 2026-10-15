using MyRagWiki.Application.Interfaces;
using MyRagWiki.Domain;

namespace MyRagWiki.Infrastructure.Parsers;

public class TextParser : IDocumentParser
{
    public DocumentType SupportedType => DocumentType.Text;

    public string ExtractText(string filePath) => File.ReadAllText(filePath);
}