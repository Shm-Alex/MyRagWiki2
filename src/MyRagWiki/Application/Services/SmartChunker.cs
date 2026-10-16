using System.Text.RegularExpressions;
using MyRagWiki.Application.Interfaces;
using MyRagWiki.Domain;

namespace MyRagWiki.Application.Services;

public class SmartChunker : IChunker
{
    public List<DocumentChunk> Chunk(string text, string sourceFile)
    {
        var chunks = new List<DocumentChunk>();
        var lines = text.Split('\n');

        // 1. Попытка разбить по заголовкам Markdown/HTML
        var headerPattern = new Regex(@"^(#{1,6}\s+.+|<h[1-6][^>]*>(.+?)</h[1-6]>)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        string currentHeader = "Introduction";
        var currentContent = new List<string>();
        int chunkIndex = 0;

        foreach (var line in lines)
        {
            var match = headerPattern.Match(line.Trim());
            if (match.Success)
            {
                SaveCurrentChunk(chunks, sourceFile, currentHeader, currentContent, ref chunkIndex);
                currentHeader = match.Groups[2].Success
                    ? match.Groups[2].Value.Trim()
                    : line.Trim().TrimStart('#').Trim();
                currentContent = new List<string>();
            }
            else
            {
                currentContent.Add(line);
            }
        }

        SaveCurrentChunk(chunks, sourceFile, currentHeader, currentContent, ref chunkIndex);

        // 2. FALLBACK 1: Если заголовков не было, пробуем разбить по абзацам (\n\n)
        if (chunks.Count == 1 && chunks[0].Header == "Introduction" && chunks[0].Text.Length > 1000)
        {
            var paragraphs = chunks[0].Text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (paragraphs.Length > 1)
            {
                var fallbackChunks = new List<DocumentChunk>();
                for (int i = 0; i < paragraphs.Length; i++)
                {
                    var pText = paragraphs[i].Trim();
                    if (pText.Length > 50)
                    {
                        fallbackChunks.Add(new DocumentChunk(
                            id: $"{Path.GetFileNameWithoutExtension(sourceFile)}_para_{i}",
                            sourceFile: sourceFile,
                            header: "Section",
                            text: pText
                        ));
                    }
                }
                chunks = fallbackChunks;
            }
        }

        // 3. FALLBACK 2 (ULTIMATE): Если всё ещё 1 огромный чанк, режем по символам с перекрытием (Sliding Window)
        // Это спасение для PDF, где PdfPig выдает сплошной текст с одиночными \n
        if (chunks.Count == 1 && chunks[0].Text.Length > 800)
        {
            var textToChunk = chunks[0].Text;
            var newChunks = new List<DocumentChunk>();

            int chunkSize = 800;  // ~150-200 слов, идеально для эмбеддингов
            int overlap = 150;    // Перекрытие, чтобы не резать предложения посередине

            for (int i = 0; i < textToChunk.Length; i += (chunkSize - overlap))
            {
                // Находим ближайший пробел или перенос строки, чтобы не резать слова
                int length = Math.Min(chunkSize, textToChunk.Length - i);
                if (i + length < textToChunk.Length)
                {
                    int lastSpace = textToChunk.LastIndexOfAny(new[] { ' ', '\n', '\r', '.', '●' }, i + length - 1, length);
                    if (lastSpace > i)
                    {
                        length = lastSpace - i;
                    }
                }

                var chunkText = textToChunk.Substring(i, length).Trim();

                // Игнорируем слишком короткие "хвосты" или мусор
                if (chunkText.Length > 100)
                {
                    newChunks.Add(new DocumentChunk(
                        id: $"{Path.GetFileNameWithoutExtension(sourceFile)}_slide_{newChunks.Count}",
                        sourceFile: sourceFile,
                        header: "Extracted Block",
                        text: chunkText
                    ));
                }
            }

            // Заменяем один гигантский чанк на массив нормальных блоков
            if (newChunks.Count > 1)
            {
                chunks = newChunks;
            }
        }

        return chunks;
    }

    private void SaveCurrentChunk(List<DocumentChunk> chunks, string sourceFile,
        string header, List<string> content, ref int index)
    {
        var text = string.Join("\n", content).Trim();
        if (string.IsNullOrWhiteSpace(text)) return;

        var id = $"{Path.GetFileNameWithoutExtension(sourceFile)}_{SanitizeId(header)}_{index++}";
        chunks.Add(new DocumentChunk(id, sourceFile, header, text));
    }

    private static string SanitizeId(string header)
    {
        return Regex.Replace(header, @"[^a-zA-Z0-9_а-яА-Я]", "_").ToLower();
    }
}