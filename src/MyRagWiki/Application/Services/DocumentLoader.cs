using System.Security.Cryptography;
using System.Text;
using MyRagWiki.Application.Interfaces;
using MyRagWiki.Domain;

namespace MyRagWiki.Application.Services;

public class DocumentLoader : IDocumentLoader
{
    private readonly IEnumerable<IDocumentParser> _parsers;
    private readonly IChunker _chunker;
    private readonly ICache _cache;

    public DocumentLoader(IEnumerable<IDocumentParser> parsers, IChunker chunker, ICache cache)
    {
        _parsers = parsers;
        _chunker = chunker;
        _cache = cache;
    }

    public async Task<List<DocumentChunk>> LoadFromFolderAsync(string folderPath)
    {
        var allChunks = new List<DocumentChunk>();
        var files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);

        Console.WriteLine($"[INFO] Найдено файлов: {files.Length}");

        foreach (var file in files)
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            var parser = _parsers.FirstOrDefault(p =>
                p.SupportedType == GetDocumentType(ext));

            if (parser == null)
            {
                Console.WriteLine($"  [SKIP] Неизвестный формат: {Path.GetFileName(file)}");
                continue;
            }

            try
            {
                var fileInfo = new FileInfo(file);
                var contentHash = ComputeFileHash(file);

                Console.WriteLine($"[PARSE] {fileInfo.Name}...");

                // Проверяем кэш
                string? text = null;
                if (_cache.IsCacheValid(file, contentHash))
                {
                    text = _cache.GetCachedText(file, contentHash);
                    Console.WriteLine($"  [CACHE] Использован кэш");
                }

                // Если нет в кэше — парсим
                if (text == null)
                {
                    text = parser.ExtractText(file);
                    _cache.SaveToCache(file, contentHash, text);
                    Console.WriteLine($"  [OK] Извлечено {text.Length} символов");
                }

                if (string.IsNullOrWhiteSpace(text))
                {
                    Console.WriteLine($"  [WARN] Пустой текст");
                    continue;
                }

                // Разбиваем на чанки
                var chunks = _chunker.Chunk(text, file);
                allChunks.AddRange(chunks);
                Console.WriteLine($"  [CHUNKS] Создано {chunks.Count} чанков");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [ERROR] {ex.Message}");
            }
        }

        return allChunks;
    }

    private static DocumentType GetDocumentType(string ext) => ext switch
    {
        ".pdf" => DocumentType.Pdf,
        ".html" or ".htm" => DocumentType.Html,
        ".md" => DocumentType.Markdown,
        ".txt" => DocumentType.Text,
        ".docx" => DocumentType.Docx,
        _ => DocumentType.Unknown
    };

    private static string ComputeFileHash(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha256.ComputeHash(stream);
        return Convert.ToBase64String(hash);
    }
}

public interface IDocumentLoader
{
    Task<List<DocumentChunk>> LoadFromFolderAsync(string folderPath);
}