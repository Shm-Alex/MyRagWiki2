using System;
using System.Collections.Generic;
using System.Runtime.Serialization;


namespace Shart.ModelDef
{
    [DataContract]
    public class BaseDefDTO : IBaseMetaDataDef
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Description { get; set; }
        [DataMember]
        public string GUID { get; set; }
        [DataMember]
        public string NameTemplate { get; set; }
        [DataMember]
        public int Version { get; set; }

		public IDictionary<string, ObjectProperty> Properties{ get { return null; } }
    }
}