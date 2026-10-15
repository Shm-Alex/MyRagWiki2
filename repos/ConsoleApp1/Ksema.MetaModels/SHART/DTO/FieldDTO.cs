using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Shart.ModelDef
{
    [DataContract]
    public class FieldDTO : BaseDefDTO, IFieldMetaData
    {
        [DataMember]
        public IOLAPObjectMetaData Owner { get; set; }
        [DataMember]
        public string RuntimeDataTypeName { get; set; }

		public object DefaultValue { get; set; }

        [DataMember]
        public DataType DataType { get; set; }
        [DataMember]
        public int DataSize { get; set; }

		[DataMember]
		public bool IsComputed{get;set;}
		[DataMember]
		public string Formula { get; set; }

		public bool IsNull{ get; set; }
	}
}