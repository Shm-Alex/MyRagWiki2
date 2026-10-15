namespace Shart.ModelDef
{
    public interface IReferenceMetaData : IFieldMetaData
    {
        bool IsIdentifying { get; }

        IDimensionMetaData Target { get; }
        string TargetName { get; }

        string BackRefTemplate { get; }

        bool HasBackRef
        {
            get;
        }

        string RefPropertyName { get; }
        string BackRefPropertyName { get; }
    }
}