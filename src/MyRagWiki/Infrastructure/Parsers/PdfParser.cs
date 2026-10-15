using MyRagWiki.Application.Interfaces;
using MyRagWiki.Domain;
using UglyToad.PdfPig;

namespace MyRagWiki.Infrastructure.Parsers;

public class PdfParser : IDocumentParser
{
    public DocumentType SupportedType => DocumentType.Pdf;

    public string ExtractText(string filePath)
    {
        using var pdf = PdfDocument.Open(filePath);
        return string.Join("\n", pdf.GetPages().Select(p => p.Text));
    }
}