using MyRagWiki.Domain;
using MyRagWiki.Domain.KnowledgeGraph;
using System.Text;

namespace MyRagWiki.Application.Services;

public class ObsidianExporter
{
    private readonly string _outputFolder;

    public ObsidianExporter(string outputFolder)
    {
        _outputFolder = outputFolder;
        Directory.CreateDirectory(_outputFolder);
    }

    /// <summary>
    /// Экспортирует граф знаний в папку Obsidian в виде .md файлов с вики-ссылками
    /// </summary>
    public async Task ExportAsync(KnowledgeGraph graph, List<DocumentChunk> chunks)
    {
        Console.WriteLine($"\n[EXPORT] Генерация {graph.Nodes.Count} статей в {_outputFolder}...");

        foreach (var node in graph.Nodes.Values)
        {
            var markdown = BuildMarkdown(node, graph, chunks);
            var filePath = Path.Combine(_outputFolder, $"{node.Title}.md");
            await File.WriteAllTextAsync(filePath, markdown, Encoding.UTF8);
            Console.WriteLine($"  [OK] {node.Title}.md");
        }

        // Создаём MOC (карту знаний) — обзорный файл со всеми связями
        await ExportMocAsync(graph);
        Console.WriteLine($"  [OK] 00_MOC_Graph.md (карта знаний)");

        Console.WriteLine($"\n✅ Экспорт завершён! Откройте папку в Obsidian.");
    }

    private string BuildMarkdown(KnowledgeNode node, KnowledgeGraph graph, List<DocumentChunk> chunks)
    {
        var sb = new StringBuilder();

        // Заголовок
        sb.AppendLine($"# {node.Title}");
        sb.AppendLine();

        // Тип сущности
        sb.AppendLine($"**Тип:** {node.Type}");
        sb.AppendLine();

        // Связи (wiki-links)
        var relatedNodes = graph.Edges
            .Where(e => e.SourceId == node.Id || e.TargetId == node.Id)
            .Select(e => e.SourceId == node.Id ? e.TargetId : e.SourceId)
            .Select(id => graph.Nodes.TryGetValue(id, out var n) ? n.Title : id)
            .Distinct()
            .ToList();

        sb.AppendLine("## Связи");
        if (relatedNodes.Any())
        {
            foreach (var related in relatedNodes)
            {
                sb.AppendLine($"- [[{related}]]");
            }
        }
        else
        {
            sb.AppendLine("- _(нет связей)_");
        }
        sb.AppendLine();

        // Упоминания в чанках (цитаты из исходных документов)
        sb.AppendLine("## Упоминания в документах");
        var mentionChunks = chunks.Where(c => node.Mentions.Contains(c.Id)).ToList();
        if (mentionChunks.Any())
        {
            foreach (var chunk in mentionChunks.Take(3)) // ограничим 3 цитатами
            {
                var shortText = chunk.Text.Length > 200
                    ? chunk.Text.Substring(0, 200) + "..."
                    : chunk.Text;
                sb.AppendLine();
                sb.AppendLine($"### {chunk.Header}");
                sb.AppendLine($"_Источник: {Path.GetFileName(chunk.SourceFile)}_");
                sb.AppendLine();
                sb.AppendLine(shortText);
            }
        }
        else
        {
            sb.AppendLine("- _(не найдено)_");
        }

        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine($"*Теги: #{node.Type} #KnowledgeGraph*");

        return sb.ToString();
    }

    private async Task ExportMocAsync(KnowledgeGraph graph)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# ️ Карта знаний (Graph MOC)");
        sb.AppendLine();
        sb.AppendLine("Автоматически сгенерированная карта связей вашей базы знаний.");
        sb.AppendLine();

        // Группируем по типу
        var byType = graph.Nodes.Values.GroupBy(n => n.Type);
        foreach (var group in byType)
        {
            sb.AppendLine($"## {group.Key}");
            sb.AppendLine();
            foreach (var node in group.OrderBy(n => n.Title))
            {
                sb.AppendLine($"- [[{node.Title}]] ({node.Mentions.Count} упоминаний)");
            }
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine($"*Всего узлов: {graph.Nodes.Count}, связей: {graph.Edges.Count}*");

        var filePath = Path.Combine(_outputFolder, "00_MOC_Graph.md");
        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
    }
}