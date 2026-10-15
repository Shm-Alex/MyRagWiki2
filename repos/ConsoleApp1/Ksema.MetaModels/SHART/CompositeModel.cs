using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ksema.MetaModels.SHART.Shart2OLTP;
using Ksema.ObjectModel;
using Ksema.ObjectModel.Serialization;
using System.Runtime.Serialization;

namespace Shart.ModelDef
{
    [CSMetaData(typeof(ShartModelMetaDataSerializer))]
	[Serializable]
	public class ShartCompositeModel : IShartModelMetaData 
    {
        [CSReference("NestedModels", true)]
        CSMultiLink<IShartModelMetaData, IShartModelMetaData> _nestedModels;

		[NonSerialized]
        Dictionary<string, IDimensionMetaData> _allDims;
		[NonSerialized]
        Dictionary<string, ICubeMetaData> _allCubes;
		[NonSerialized]
		bool _initialized = false;

        public ShartCompositeModel()
        {
            CSPropMgr.Instance.setupObject(this, false);
        }

    	private Dictionary<string, IDimensionMetaData> AllDims
    	{
    		get
    		{
				if (!_initialized)
					lock (this)
						if (!_initialized)
							ReindexAll();
				return _allDims;
    		}
    	}

    	private Dictionary<string, ICubeMetaData> AllCubes
    	{
    		get
    		{
				if (!_initialized)
					lock (this)
						if (!_initialized)
							ReindexAll();
				return _allCubes;
    		}
    	}

        public IList<IShartModelMetaData> NestedModels
        {
            get { return _nestedModels; }
        }

		public void ReindexAll()
		{
			lock (this)
			{
				_initialized = false;
				try
				{
					_allCubes = new Dictionary<string, ICubeMetaData>();
					_allDims = new Dictionary<string, IDimensionMetaData>();
					foreach (IShartModelMetaData model in NestedModels)
					{
						foreach (IDimensionMetaData dim in model.GetAllDimensionsMetaData())
							_allDims[dim.Name] = dim;
						foreach (ICubeMetaData cube in model.GetAllCubesMetaData())
							_allCubes[cube.Name] = cube;
					}
				}
				finally
				{
					_initialized = true;
				}
			}
		}

        public string Name
        {
            get { return ShartMappingUtils.CompositeModelName; }
        }

        public IDimensionMetaData GetDimensionMetaData(string sysName)
        {
            return AllDims[sysName];
        }

        public ICubeMetaData GetCubeMetaData(string sysName)
        {
            return AllCubes[sysName];
        }

        public ICollection<IDimensionMetaData> GetAllDimensionsMetaData()
        {
            return AllDims.Values;
        }

        public ICollection<ICubeMetaData> GetAllCubesMetaData()
        {
            return AllCubes.Values;
        }

        public string DefaultClassName
        {
            get { return String.Empty; }
        }

        public string DefaultSetName
        {
            get { return String.Empty; }
        }

        public string DefaultRefName
        {
            get { return String.Empty; }
        }

        public string DefaultBackRefName
        {
            get { return String.Empty; }
        }


        public string AssemblyName
        {
            get { return String.Empty; }
        }

        public string Namespace
        {
            get { return String.Empty; }
        }

        public IDimensionMetaData TryGetDimensionMetaData(string sysName)
        {
            IDimensionMetaData result = null;
            AllDims.TryGetValue(sysName,out result);
            return result;
        }

        public ICubeMetaData TryGetCubeMetaData(string sysName)
        {
            ICubeMetaData result = null;
            AllCubes.TryGetValue(sysName,out result);
            return result;
        }

		public IOLAPObjectMetaData TryGetContentMetaData( string sysName )
		{
			IOLAPObjectMetaData result = TryGetDimensionMetaData( sysName );
			if( result == null )
				result = TryGetCubeMetaData( sysName );
			return result;
		}
    }
}
