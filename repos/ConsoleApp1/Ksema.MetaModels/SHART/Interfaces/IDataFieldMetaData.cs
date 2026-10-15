namespace Shart.ModelDef
{
    public interface IDataFieldMetaData : IFieldMetaData
    {
        bool IsAnalysis { get; }
        bool IsIndexed { get; }		
    }
}