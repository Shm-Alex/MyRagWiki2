using System;

namespace Shart.ModelDef
{
    public class ReferenceDTO : FieldDTO, IReferenceMetaData
    {
        public bool IsIdentifying { get; set; }
        public IDimensionMetaData Target { get; set; }
        public string TargetName{get;set;}
        public string BackRefTemplate { get; set; }
        public bool HasBackRef { get; set; }
        public string RefPropertyName { get; set; }
        public string BackRefPropertyName { get; set; }
    }
}