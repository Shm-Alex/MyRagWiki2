using System.Collections.Generic;

namespace Shart.ModelDef
{
    public interface IOLAPObjectMetaData : IBaseMetaDataDef
    {
        bool IsSystem { get; }
        bool IsHistoric { get; }

        string TablePostfix { get; }
        string IDName { get;}

        IFieldMetaData GetFieldByName(string name);
        ICollection<IFieldMetaData> GetAllFields();
        ICollection<IDataFieldMetaData> GetDataFields();
        ICollection<IReferenceMetaData> GetReferences();

		object GetValue( string propertyName );
		T GetValue<T>( string propertyName );

        IShartModelMetaData Model { get; }
        string SetTemplate { get; }
        string ClassName { get; }
        string SetName { get; }
        string ModelName { get; }
		string UIProfile { get; }
    }
}