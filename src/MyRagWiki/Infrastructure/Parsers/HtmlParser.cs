using HtmlAgilityPack;
using MyRagWiki.Application.Interfaces;
using MyRagWiki.Domain;

namespace MyRagWiki.Infrastructure.Parsers;

public class HtmlParser : IDocumentParser
{
    public DocumentType SupportedType => DocumentType.Html;

    public string ExtractText(string filePath)
    {
        var doc = new HtmlDocument();
        doc.Load(filePath);
        return doc.DocumentNode.InnerText;
    }
}