using MyRagWiki.Domain;

namespace MyRagWiki.Application.Interfaces;

public interface IChunker
{
    List<DocumentChunk> Chunk(string text, string sourceFile);
}