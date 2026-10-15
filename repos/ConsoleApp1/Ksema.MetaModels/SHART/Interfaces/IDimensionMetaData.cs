using System.Collections.Generic;

namespace Shart.ModelDef
{
    public interface IDimensionMetaData: IOLAPObjectMetaData
    {
        int LevelCount { get; }
        bool IsAccessControlled { get; set; }
        ICollection<IDataFieldMetaData> GetAnalysisFields();
        ICollection<IDataFieldMetaData> GetIndexedFields();

        bool HasReferenceRoot { get;}
        bool NeedSearchIndexing { get;}

        string[] GetLevelNames();

        string GetLevelName( int levelNumber );

        IEnumerable<IReferenceMetaData> GetReferencedByListWithBackRefs();

        SqlAction[] GetTriggerSQL(SQLTemplateType type);
    }

    public class SqlAction
    {
        public string Sql { get; set; }
        public string Error { get; set; }
    }

    public enum SQLTemplateType { ModelTrigger, SQLTrigger }

}