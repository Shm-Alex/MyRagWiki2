
namespace MyRagWiki.Domain.KnowledgeGraph
{
    public class KnowledgeNode
    {
        public string Id { get; init; }
        public string Title { get; init; }
        public string Type { get; init; } // Technology, Company, Project, Pattern
        public List<string> Mentions { get; init; } = new(); // ID чанков, где упоминается

        public KnowledgeNode(string id, string title, string type)
        {
            Id = id;
            Title = title;
            Type = type;
        }
    }
}
