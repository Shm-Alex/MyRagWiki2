using MyRagWiki.Application.Interfaces;
using MyRagWiki.Application.Services;
using MyRagWiki.Domain;
using MyRagWiki.Infrastructure.Cache;
using MyRagWiki.Infrastructure.Ollama;
using MyRagWiki.Infrastructure.Parsers;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("=== 🧠 MyRagWiki v2: DDD + Парсинг + Кэш ===\n");

// === Dependency Injection вручную (для простоты) ===
var ollama = new OllamaClient("http://localhost:11434");
var cache = new FileCache(Path.Combine(Directory.GetCurrentDirectory(), ".cache"));

IEnumerable<IDocumentParser> parsers = new IDocumentParser[]
{
    new PdfParser(),
    new HtmlParser(),
    new MarkdownParser(),
    new TextParser(),
    new DocxParser()
};

IChunker chunker = new SmartChunker();
IDocumentLoader loader = new DocumentLoader(parsers, chunker, cache);

// === Шаг 1: Загрузка документов ===
var docsFolder = @"C:\Users\alexandershm\Documents\Obsidian Vault\00_Raw_Data";

Console.WriteLine($"[1/4] Сканирование папки: {docsFolder}");
var chunks = await loader.LoadFromFolderAsync(docsFolder);

if (chunks.Count == 0)
{
    Console.WriteLine(" Не найдено ни одного чанка.");
    return;
}
Console.WriteLine($"\n✅ Всего чанков: {chunks.Count}\n");

// === Шаг 2: Векторизация ===
Console.WriteLine("[2/4] Векторизация (nomic-embed-text)...");
foreach (var chunk in chunks)
{
    chunk.Embedding = await ollama.GetEmbeddingAsync(chunk.Text, "nomic-embed-text");
    Console.Write(".");
}
Console.WriteLine("\n✅ Векторизация завершена.\n");

// === Шаг 3: Запрос пользователя ===
Console.WriteLine("Введите вопрос (или Enter для теста):");
var input = Console.ReadLine();
var query = string.IsNullOrWhiteSpace(input)
    ? "Расскажи про мой опыт с Kafka и миграцией баз данных"
    : input;

Console.WriteLine("\n[3/4] Поиск...");
var queryEmbedding = await ollama.GetEmbeddingAsync(query, "nomic-embed-text");

var validChunks = chunks.Where(c => c.Embedding.Length > 0).ToList();
var bestMatches = validChunks
    .Select(c => new { Chunk = c, Similarity = CosineSimilarity(queryEmbedding, c.Embedding) })
    .OrderByDescending(x => x.Similarity)
    .Take(3)
    .ToList();

Console.WriteLine($"✅ Найдено {bestMatches.Count} релевантных фрагментов:");
foreach (var m in bestMatches)
    Console.WriteLine($"   - [{m.Chunk.Header}] из {Path.GetFileName(m.Chunk.SourceFile)} ({m.Similarity:P1})");

// === Шаг 4: Генерация ответа ===
var combinedFacts = string.Join("\n\n",
    bestMatches.Select(m => $"[{m.Chunk.Header}]\n{m.Chunk.Text}"));

var prompt = $@"Ты — помощник по анализу резюме Senior .NET разработчика.
Используй ТОЛЬКО следующие факты:

{combinedFacts}

ВОПРОС: {query}

Дай структурированный ответ на русском. Не выдумывай факты.";

Console.WriteLine("\n[4/4] Генерация (qwen2.5-coder:14b)...");
var response = await ollama.GenerateAsync(prompt, "qwen2.5-coder:14b");

Console.WriteLine("\n" + new string('=', 60));
Console.WriteLine("🤖 ОТВЕТ:");
Console.WriteLine(new string('=', 60));
Console.WriteLine(response);

// === Утилиты ===
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