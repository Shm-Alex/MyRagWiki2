using System;
using System.Collections.Generic;
using System.Linq;

namespace Shart.ModelDef
{
    public class DimensionDTO : OLAPObjectDTO, IDimensionMetaData
    {
        public int LevelCount { get; set; }
        public bool IsAccessControlled { get; set; }
        public bool HasReferenceRoot { get; set; }
        public bool NeedSearchIndexing { get; set; }

        public ICollection<IDataFieldMetaData> GetAnalysisFields()
        {
            return GetDataFields().Where(f => f.IsAnalysis).ToList();
        }

        public ICollection<IDataFieldMetaData> GetIndexedFields()
        {
            if (!this.NeedSearchIndexing)
            {
                return new List<IDataFieldMetaData>();
            }
            return GetDataFields().Where(f => f.IsIndexed).ToList();
        }

        public IEnumerable<IReferenceMetaData> GetReferencedByListWithBackRefs()
        {
            var list = from dim in Model.GetAllDimensionsMetaData()
                       from aRef in dim.GetReferences()
                       where aRef.Target.Name == Name && aRef.HasBackRef
                       select aRef;
            return list;
        }

        public string[] GetLevelNames()
        {
            throw new NotImplementedException();
        }

        public string GetLevelName(int levelNumber)
        {
            throw new NotImplementedException();
        }

        public SqlAction[] GetTriggerSQL(SQLTemplateType type)
        {
            throw new NotImplementedException();
        }
    }
}