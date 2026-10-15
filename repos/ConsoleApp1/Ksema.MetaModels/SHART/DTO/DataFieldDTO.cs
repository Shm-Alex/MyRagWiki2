namespace Shart.ModelDef
{
    public class DataFieldDTO : FieldDTO, IDataFieldMetaData
    {
        public bool IsAnalysis { get; set; }
        public bool IsIndexed { get; set; }
    }
}