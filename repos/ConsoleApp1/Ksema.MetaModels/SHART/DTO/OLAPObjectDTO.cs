using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq;

namespace Shart.ModelDef
{
    [DataContract]
    public class OLAPObjectDTO : BaseDefDTO, IOLAPObjectMetaData
    {
        [DataMember]
        public IDictionary<string, FieldDTO> Fields { get; internal set; }

        public OLAPObjectDTO()
        {
            Fields = new Dictionary<string, FieldDTO>();
        }

        [DataMember]
        public string ModelName { get; set; }

        [DataMember]
        public bool IsSystem { get; set; }
        [DataMember]
        public bool IsHistoric { get; set; }
        [DataMember]
        public string TablePostfix { get; set; }
        [DataMember]
        public string IDName { get; set; }
        [DataMember]
        public IShartModelMetaData Model { get; set; }
        [DataMember]
        public string SetTemplate { get; set; }
        [DataMember]
        public string ClassName { get; set; }
        [DataMember]
        public string SetName { get; set; }

        public IFieldMetaData GetFieldByName(string name)
        {
            return Fields[name];
        }

        public ICollection<IFieldMetaData> GetAllFields()
        {
            return Fields.Values.Cast<IFieldMetaData>().ToList();
        }

        public ICollection<IDataFieldMetaData> GetDataFields()
        {
            return Fields.Values.Where(f => f is IDataFieldMetaData).Cast<IDataFieldMetaData>().ToList();
        }

        public ICollection<IReferenceMetaData> GetReferences()
        {
            return Fields.Values.Where(f => f is IReferenceMetaData).Cast<IReferenceMetaData>().ToList();            
        }

		public object GetValue( string propertyName )
		{
			return null;
		}

		public T GetValue<T>( string propertyName )
		{
			return default(T);
		}

		public string UIProfile
		{
			get
			{
				return null;
			}
		}

	}
}