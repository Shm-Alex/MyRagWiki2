using MyRagWiki.Domain;
using MyRagWiki.Domain.KnowledgeGraph;

namespace MyRagWiki.Application.Services;

public class GraphBuilder
{
    // Словарь известных сущностей. В реальном проекте это можно вынести в JSON-файл.
    private static readonly Dictionary<string, string> KnownEntities = new(StringComparer.OrdinalIgnoreCase)
    {
        // Технологии
        { "C#", "Technology" }, { "C++", "Technology" }, { "Java", "Technology" },
        { ".NET", "Technology" }, { ".NET Core", "Technology" }, { ".NET 6", "Technology" },
        { ".NET 8", "Technology" }, { "ASP.NET Core", "Technology" },
        { "Entity Framework Core", "Technology" }, { "EF Core", "Technology" },
        { "LINQ", "Technology" }, { "Dapper", "Technology" }, { "MediatR", "Technology" },
        
        // БД
        { "PostgreSQL", "Technology" }, { "MS SQL Server", "Technology" },
        { "SQL Server", "Technology" }, { "Redis", "Technology" }, { "Couchbase", "Technology" },
        
        // Инфраструктура
        { "Docker", "Technology" }, { "Kafka", "Technology" }, { "Apache Kafka", "Technology" },
        { "RabbitMQ", "Technology" }, { "Debezium", "Technology" }, { "ELK", "Technology" },
        { "ElasticSearch", "Technology" }, { "Git", "Technology" },
        
        // Архитектура
        { "DDD", "Pattern" }, { "Domain-Driven Design", "Pattern" },
        { "Clean Architecture", "Pattern" }, { "CQRS", "Pattern" },
        { "SOLID", "Pattern" }, { "REST", "Pattern" }, { "REST API", "Pattern" },
        { "gRPC", "Pattern" }, { "Microservices", "Pattern" },
        
        // Компании/проекты
        { "TERN", "Company" }, { "Amdocs", "Company" },
        { "Банк ПСБ", "Company" }, { "ПСБ", "Company" },
        { "IBS", "Company" }, { "Mont", "Company" },
        { "Ruspo", "Company" }, { "IdentityServer4", "Technology" },
        { "Universe.Service", "Project" }, { "SchoolRowingApp", "Project" },
        { "AutoMergeWeb", "Project" }
    };

    public KnowledgeGraph Build(List<DocumentChunk> chunks)
    {
        var graph = new KnowledgeGraph();

        foreach (var chunk in chunks)
        {
            // 1. Находим все сущности в тексте чанка
            var foundEntities = FindEntitiesInText(chunk.Text);

            // 2. Создаём узлы
            foreach (var (entity, type) in foundEntities)
            {
                var nodeId = SanitizeId(entity);
                graph.AddNode(new KnowledgeNode(nodeId, entity, type));

                // Запоминаем, в каком чанке упоминается
                if (graph.Nodes[nodeId].Mentions.Contains(chunk.Id)) continue;
                graph.Nodes[nodeId].Mentions.Add(chunk.Id);
            }

            // 3. Строим рёбра между всеми найденными в одном чанке сущностями
            var entityList = foundEntities.Select(e => SanitizeId(e.Key)).ToList();
            for (int i = 0; i < entityList.Count; i++)
            {
                for (int j = i + 1; j < entityList.Count; j++)
                {
                    graph.AddEdge(new KnowledgeEdge(entityList[i], entityList[j], "co-occurs_in"));
                }
            }
        }

        return graph;
    }

    private List<KeyValuePair<string, string>> FindEntitiesInText(string text)
    {
        var found = new List<KeyValuePair<string, string>>();
        foreach (var (entity, type) in KnownEntities)
        {
            if (text.Contains(entity, StringComparison.OrdinalIgnoreCase))
            {
                found.Add(new KeyValuePair<string, string>(entity, type));
            }
        }
        return found;
    }

    private static string SanitizeId(string name)
    {
        return System.Text.RegularExpressions.Regex
            .Replace(name, @"[^a-zA-Z0-9_а-яА-Я]", "_")
            .ToLower();
    }
}