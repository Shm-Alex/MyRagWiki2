using System;
using System.Collections.Generic;
using System.Linq;
using Ksema.Framework;
using Ksema.ObjectModel;

namespace Shart.ModelDef
{
	[Serializable]
	public class ContextMetaData : BaseMetaData, IObjectURIProvider
	{
		[CSReference( "AttachedObjects", false, "Contexts" )]
		CSMultiLink<ContextMetaData, BaseMetaData> _attachedObjects;

		public ContextMetaData()
		{
			CSPropMgr.Instance.setupObject( this, false );
		}

		public IList<BaseMetaData> AttachedObjects
		{
			get
			{
				return _attachedObjects;
			}
		}

		public ObjectURI CreateURI()
		{
			ObjectURI result = new ObjectURI( "context/" + GUID );
			return result;
		}
	}
}
