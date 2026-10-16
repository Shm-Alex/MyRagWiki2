
namespace MyRagWiki.Domain.KnowledgeGraph
{

    public class KnowledgeGraph
    {
        public Dictionary<string, KnowledgeNode> Nodes { get; } = new();
        public List<KnowledgeEdge> Edges { get; } = new();

        public void AddNode(KnowledgeNode node)
        {
            if (!Nodes.ContainsKey(node.Id))
                Nodes[node.Id] = node;
        }

        public void AddEdge(KnowledgeEdge edge)
        {
            // Избегаем дубликатов
            if (!Edges.Any(e => e.SourceId == edge.SourceId && e.TargetId == edge.TargetId))
                Edges.Add(edge);
        }
    }
}
