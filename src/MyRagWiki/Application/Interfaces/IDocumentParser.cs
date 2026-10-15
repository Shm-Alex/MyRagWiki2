using MyRagWiki.Domain;

namespace MyRagWiki.Application.Interfaces;

public interface IDocumentParser
{
    DocumentType SupportedType { get; }
    string ExtractText(string filePath);
}