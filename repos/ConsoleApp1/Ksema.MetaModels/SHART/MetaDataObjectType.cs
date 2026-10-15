using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shart.ModelDef
{
	public enum MetaDataObjectType
	{
		Any,
		Dimension,
		Cube,
		DataField,
		Reference,
		Index,
		IndexField,
		SQLTemplate,
		Context,
		PartitionFunction,
		PartitionScheme,
		UpdateScript
	}
}
