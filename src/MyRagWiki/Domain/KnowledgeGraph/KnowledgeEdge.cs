
namespace MyRagWiki.Domain.KnowledgeGraph
{
    public class KnowledgeEdge
    {
        public string SourceId { get; init; }
        public string TargetId { get; init; }
        public string Relation { get; init; }

        public KnowledgeEdge(string sourceId, string targetId, string relation)
        {
            SourceId = sourceId;
            TargetId = targetId;
            Relation = relation;
        }
    }
}
