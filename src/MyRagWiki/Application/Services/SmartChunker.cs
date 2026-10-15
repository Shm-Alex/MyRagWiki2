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

        // Паттерн для заголовков Markdown (##) и HTML (<h1>-<h6>)
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
                // Сохраняем предыдущий чанк
                SaveCurrentChunk(chunks, sourceFile, currentHeader, currentContent, ref chunkIndex);

                // Извлекаем текст заголовка
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

        // Последний чанк
        SaveCurrentChunk(chunks, sourceFile, currentHeader, currentContent, ref chunkIndex);

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