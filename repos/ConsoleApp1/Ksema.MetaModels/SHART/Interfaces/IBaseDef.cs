using System.Collections.Generic;
namespace Shart.ModelDef
{
	public interface IBaseMetaDataDef
	{
		string Name{ get; }
		
		string Description { get; }

		string GUID { get; }

        string NameTemplate { get; }

		int Version { get; }

		IDictionary<string, ObjectProperty> Properties { get; }
//        IDictionary<string, object> ExtendedProperties { get; }
	}
}
