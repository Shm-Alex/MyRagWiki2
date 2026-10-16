using MyRagWiki.Application.Interfaces;
using MyRagWiki.Application.Services;
using MyRagWiki.Domain;
using MyRagWiki.Infrastructure.Cache;
using MyRagWiki.Infrastructure.Ollama;
using MyRagWiki.Infrastructure.Parsers;

Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine("=== 🧠 MyRagWiki v3: Graph RAG + Obsidian Exporter ===\n");
Console.WriteLine("Выберите режим:");
Console.WriteLine("  1 — Вопрос-Ответ (RAG)");
Console.WriteLine("  2 — Построить LLM Wiki (Graph RAG + Obsidian Export)");
Console.Write("\nВаш выбор (1/2): ");

var mode = Console.ReadLine()?.Trim();

// === Общие зависимости ===
var ollama = new OllamaClient("http://localhost:11434");
var cache = new FileCache(Path.Combine(Directory.GetCurrentDirectory(), ".cache"));

IEnumerable<IDocumentParser> parsers = new IDocumentParser[]
{
    new HtmlParser(),
    new MarkdownParser(),
    new TextParser(),
    new DocxParser(),
    new PdfParser() // раскомментируйте, если PdfPig установлен
};

IChunker chunker = new SmartChunker();
IDocumentLoader loader = new DocumentLoader(parsers, chunker, cache);

var docsFolder = @"C:\Users\alexandershm\Documents\Obsidian Vault\00_Raw_Data";

Console.WriteLine($"\n[1/4] Сканирование папки: {docsFolder}");
var chunks = await loader.LoadFromFolderAsync(docsFolder);

if (chunks.Count == 0)
{
    Console.WriteLine("❌ Не найдено ни одного чанка.");
    return;
}
Console.WriteLine($"\n✅ Всего чанков: {chunks.Count}\n");

if (mode == "2")
{
    // === РЕЖИМ 2: Graph RAG + Export ===
    Console.WriteLine("[2/4] Построение графа знаний...");
    var graphBuilder = new GraphBuilder();
    var graph = graphBuilder.Build(chunks);

    Console.WriteLine($"✅ Граф построен: {graph.Nodes.Count} узлов, {graph.Edges.Count} связей");

    // Выведем топ-10 самых связанных сущностей
    Console.WriteLine("\n🔝 Топ-10 сущностей по количеству связей:");
    var topNodes = graph.Nodes.Values
        .OrderByDescending(n => graph.Edges.Count(e => e.SourceId == n.Id || e.TargetId == n.Id))
        .Take(10);
    foreach (var node in topNodes)
    {
        var edgeCount = graph.Edges.Count(e => e.SourceId == node.Id || e.TargetId == node.Id);
        Console.WriteLine($"   - {node.Title} ({node.Type}): {edgeCount} связей");
    }

    Console.WriteLine("\n[3/4] Экспорт в Obsidian...");
    var obsidianFolder = Path.Combine(Directory.GetCurrentDirectory(), "obsidian_export");
    var exporter = new ObsidianExporter(obsidianFolder);
    await exporter.ExportAsync(graph, chunks);

    Console.WriteLine($"\n Файлы сохранены в: {obsidianFolder}");
    Console.WriteLine("Откройте эту папку как хранилище в Obsidian и наслаждайтесь графом связей!");
}
else
{
    // === РЕЖИМ 1: Классический RAG (вопрос-ответ) ===
    Console.WriteLine("[2/4] Векторизация (nomic-embed-text)...");
    foreach (var chunk in chunks)
    {
        chunk.Embedding = await ollama.GetEmbeddingAsync(chunk.Text, "nomic-embed-text");
        Console.Write(".");
    }
    Console.WriteLine("\n✅ Векторизация завершена.\n");

    Console.WriteLine("Введите вопрос (или Enter для теста):");
    var input = Console.ReadLine();
    var query = string.IsNullOrWhiteSpace(input)
        ? "Составь список: Технология => Компания => Годы работы"
        : input;

    Console.WriteLine("\n[3/4] Поиск...");
    var queryEmbedding = await ollama.GetEmbeddingAsync(query, "nomic-embed-text");

    var validChunks = chunks.Where(c => c.Embedding.Length > 0).ToList();
    var bestMatches = validChunks
        .Select(c => new { Chunk = c, Similarity = CosineSimilarity(queryEmbedding, c.Embedding) })
        .OrderByDescending(x => x.Similarity)
        .Take(6)
        .ToList();

    Console.WriteLine($"✅ Найдено {bestMatches.Count} релевантных фрагментов:");
    foreach (var m in bestMatches)
        Console.WriteLine($"   - [{m.Chunk.Header}] ({m.Similarity:P1})");

    var combinedFacts = string.Join("\n\n",
        bestMatches.Select(m => $"[{m.Chunk.Header}]\n{m.Chunk.Text}"));

    var prompt = $@"Ты — эксперт по анализу резюме.
Используй ТОЛЬКО следующие факты:

{combinedFacts}

ЗАПРОС: {query}

Дай структурированный ответ на русском. Не выдумывай факты.";

    Console.WriteLine("\n[4/4] Генерация (qwen2.5-coder:14b)...");
    var response = await ollama.GenerateAsync(prompt, "qwen2.5-coder:14b");

    Console.WriteLine("\n" + new string('=', 60));
    Console.WriteLine(" ОТВЕТ:");
    Console.WriteLine(new string('=', 60));
    Console.WriteLine(response);
}

static float CosineSimilarity(float[] a, float[] b)
{
    if (a.Length == 0 || b.Length == 0 || a.Length != b.Length) return 0f;
    float dot = 0, magA = 0, magB = 0;
    for (int i = 0; i < a.Length; i++)
    {
        dot += a[i] * b[i];
        magA += a[i] * a[i];
        magB += b[i] * b[i];
    }
    var denom = MathF.Sqrt(magA) * MathF.Sqrt(magB);
    return denom == 0 ? 0f : dot / denom;
}