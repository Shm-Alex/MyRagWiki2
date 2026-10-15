using System;
using System.Collections.Generic;
using System.Text;
using Ksema.ObjectModel;

namespace Shart.ModelDef
{
	[Serializable]
	public class DimensionLevelMetaData : BaseMetaData
    {
        private int _number;

        [CSReference( "Owner", false, "Levels" )]
        private CSSingleLink<DimensionLevelMetaData, DimensionMetaData> _owner = null;


        public DimensionLevelMetaData()
        {
            CSPropMgr.Instance.setupObject( this, false );
        }

        [CSAttribute( "Number" )]
        public int Number
        {
            get { return _number; }
            set { _number = value; }
        }

        public DimensionMetaData Owner
        {
            get { return _owner.LinkedObject; }
            set { _owner.LinkedObject = value; }
        }

		public DimensionLevelMetaData Clone()
		{
			DimensionLevelMetaData result = (DimensionLevelMetaData)MemberwiseClone();
			CSPropMgr.Instance.setupObject( result, true );
			return result;
		}
	}
}
