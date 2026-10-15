using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using MyRagWiki.Application.Interfaces;
using MyRagWiki.Domain;

namespace MyRagWiki.Infrastructure.Parsers;

public class DocxParser : IDocumentParser
{
    public Domain.DocumentType SupportedType => Domain.DocumentType.Docx;

    public string ExtractText(string filePath)
    {
        using var doc = WordprocessingDocument.Open(filePath, false);
        var body = doc.MainDocumentPart?.Document.Body;
        if (body == null) return string.Empty;

        var paragraphs = new List<string>();

        foreach (var element in body.Elements())
        {
            switch (element)
            {
                case Paragraph para:
                    paragraphs.Add(para.InnerText);
                    break;
                case Table table:
                    // Извлекаем текст из таблиц
                    foreach (var row in table.Elements<TableRow>())
                    {
                        var cells = row.Elements<TableCell>()
                            .Select(c => c.InnerText)
                            .ToList();
                        paragraphs.Add(string.Join(" | ", cells));
                    }
                    break;
            }
        }

        return string.Join("\n", paragraphs);
    }
}