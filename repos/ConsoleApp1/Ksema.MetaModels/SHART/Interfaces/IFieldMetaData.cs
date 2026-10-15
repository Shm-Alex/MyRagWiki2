namespace Shart.ModelDef
{
    public interface IFieldMetaData : IBaseMetaDataDef
    {
        IOLAPObjectMetaData Owner { get; }
        string RuntimeDataTypeName { get; }
		object DefaultValue { get; }
        DataType DataType { get; }
        int DataSize { get; }
		bool IsNull { get; }
		bool IsComputed { get; }
		string Formula { get; }
    }
}