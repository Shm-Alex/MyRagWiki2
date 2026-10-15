using System;
using System.Collections.Generic;
using System.Threading;
using Framework.Utils;
using Ksema.MetaModels.OLTP;
using Shart.ModelDef;
using System.Data;

namespace Ksema.MetaModels.SHART.Shart2OLTP
{
    public interface IShartModelMapping
    {
        OLTPSchema ModelMapping { get;}
		IDimensionMapping GetDimensionMapping(string sysName);
		IDimensionMapping GetDimensionMapping(string sysName, bool throwOnError);
		ICubeMapping GetCubeMapping(string sysName);
		ICubeMapping GetCubeMapping(string sysName, bool throwOnError);
        IDimensionMapping GetDimensionMappingByField(IEntityMapping cm, string fieldName);
    }

    public interface IEntityMapping
    {
        Table ViewTable { get;}
        Column GetFieldMapping(string fieldName);
        FKColumn GetReferenceMapping(string fieldName);
        string IDName { get;}
        DbType IDDbType { get; }
        Column IDColumn { get; }
        IOLAPObjectMetaData Entity{get;}
        IShartModelMapping ModelMap { get;}
		string GetFSuppString(string fsupp);
    }

    public interface IDimensionMapping : IEntityMapping
    {
		string LevelName { get; }
		string VersionID { get; }
		Table MainTable { get; }
        IList<Column> GetAnalysisFieldColumns(string fieldName);
        Column ParentIDColumn { get;}
        IList<Column> GetParentColumns();
        IDimensionMetaData Dimension { get;}
    }

    public interface ICubeMapping : IEntityMapping
    {
        ICubeMetaData Cube { get;}
    }

    public class ShartModelMapping : IShartModelMapping
    {
        OLTPSchema _schema;
        IShartModelMetaData _model;
        Dictionary<string, IDimensionMapping> _dimMaps = new Dictionary<string, IDimensionMapping>();
        Dictionary<string, ICubeMapping> _cubeMaps = new Dictionary<string, ICubeMapping>();
		ReaderWriterLockSlim _dimLock = new ReaderWriterLockSlim();
		ReaderWriterLockSlim _cubeLock = new ReaderWriterLockSlim();

        public ShartModelMapping(IShartModelMetaData aModel, string schemaOwner)
        {
            _model = aModel;
            _schema = new ShartModel2OLTPSchema(aModel, schemaOwner).CreateOLTPSchema();
        }

        public OLTPSchema ModelMapping
        {
            get { return _schema; }
        }

        public IDimensionMapping GetDimensionMapping(string sysName)
        {
			return GetDimensionMapping(sysName, true);
        }

        public IDimensionMapping GetDimensionMapping(string sysName, bool throwOnError)
        {
            IDimensionMapping map = null;
			_dimLock.EnterUpgradeableReadLock();
            try
            {
                if (!_dimMaps.TryGetValue(sysName, out map))
                {
                    IDimensionMetaData dim = _model.TryGetDimensionMetaData(sysName);
					if (dim == null)
					{
						if (throwOnError)
							throw new Exception("Cannot find dimension: " + sysName);
					}
					else
					{
						map = new DimensionMapping(dim, _schema, this);
						_dimLock.EnterWriteLock();
						try
						{
							_dimMaps.Add(sysName, map);
						}
						finally
						{
							_dimLock.ExitWriteLock();
						}
					}
                }
            }
			finally
            {
				_dimLock.ExitUpgradeableReadLock();
            }

            return map;
        }

        public IDimensionMapping GetDimensionMappingByField(IEntityMapping cm, string fieldName)
        {
            IReferenceMetaData aRef = (IReferenceMetaData) cm.Entity.GetFieldByName(fieldName);
            if (aRef != null)
                return GetDimensionMapping(aRef.Target.Name);
            else
            {
                if (cm is IDimensionMapping && (fieldName == ShartMappingUtils.ParentIDName
                    || fieldName.StartsWith(ShartMappingUtils.ParentLevelPrefixName)))
                    return (IDimensionMapping)cm;
                else
                    throw new NotFoundException("Cannot find mapping for reference: " + cm.Entity.Name + "." + fieldName);

            }
        }

        public ICubeMapping GetCubeMapping(string sysName)
        {
			return GetCubeMapping(sysName, true);
        }

        public ICubeMapping GetCubeMapping(string sysName, bool throwOnError)
        {
            ICubeMapping map = null;

			_cubeLock.EnterUpgradeableReadLock();
			try
            {
                if (!_cubeMaps.TryGetValue(sysName, out map))
                {
                    ICubeMetaData cube = _model.TryGetCubeMetaData(sysName);
					if (cube == null)
					{
						if (throwOnError)
							throw new Exception("Cannot find cube: " + sysName);
					}
					else
					{
						map = new CubeMapping(cube, _schema, this);
						_cubeLock.EnterWriteLock();
						try
						{
							_cubeMaps.Add(sysName, map);
						}
						finally
						{
							_cubeLock.ExitWriteLock();
						}
					}
                }

            }
			finally
			{
				_cubeLock.ExitUpgradeableReadLock();
			}

            return map;
        }
    }

    internal abstract class EntityMapping : IEntityMapping
    {
        private IOLAPObjectMetaData _entity;
        private Table _ViewTable;
    	protected OLTPSchema _schema;
        IShartModelMapping _modelMap;

        public EntityMapping(IOLAPObjectMetaData entity, OLTPSchema schema, IShartModelMapping modelMap)
        {
            if (schema == null)
                throw new ArgumentNullException("schema");
            if (entity == null)
                throw new ArgumentNullException("entity");

            _schema = schema;
            _modelMap = modelMap;
            _entity = entity;
        }

    	public abstract string GetFSuppString(string fsupp);
    	protected abstract string GetViewTableName();

        public virtual DbType IDDbType
        {
            get { throw new System.NotImplementedException(); }
        }

        public Table ViewTable
        {
            get 
            {
                if (_ViewTable == null)
                    _ViewTable = _schema.GetTable(GetViewTableName());
                return _ViewTable; 
            }
        }

        public virtual Column GetFieldMapping(string fieldName)
        {
            Column col = ViewTable.GetColumn(fieldName, false);
            return col;
        }

        public FKColumn GetReferenceMapping(string fieldName)
        {
            Column col = ViewTable.GetColumn(fieldName, false);
			//if (col == null)
			//    col = MainTable.GetColumn(fieldName, true);
            string fkID = ShartModel2OLTPSchema.GetReferenceFKName(col);
            FK fk = col.Owner.GetFK(fkID);
            return fk.FKColumns[0];
        }

        public abstract string IDName { get; }

    	public abstract Column IDColumn
		{
			get;
		}

        public IOLAPObjectMetaData Entity
        {
            get { return _entity; }
        }

        public IShartModelMapping ModelMap
        {
            get { return _modelMap; }
        }
    }

    internal class DimensionMapping : EntityMapping, IDimensionMapping
	{
		private Table _MainTable;
        private readonly IDimensionMetaData _dim;

        public DimensionMapping(IDimensionMetaData aDim, OLTPSchema aSchema, IShartModelMapping modelMap)
            :base(aDim, aSchema, modelMap)
        {
            _dim = aDim;
        }

		public override string GetFSuppString(string fsupp)
		{
			if (!fsupp.Contains("."))
			{
				if (fsupp.StartsWith("@"))
				{
					string fid = fsupp.TrimStart('@');
					fid = fid.ToUpper();
					switch (fid)
					{
						case "ID":
							return MainTable.FullName + "." + IDName;

						case "GUID":
							return ViewTable.FullName + "." + ShartMappingUtils.GuidName;

						case "LEVEL":
							return MainTable.FullName + "." + LevelName;

						case "VERSIONID":
							return ViewTable.FullName + "." + VersionID;

						case "DATECLOSE":
							return ViewTable.FullName + "." + ShartMappingUtils.DateCloseName;

						case "DATECREATE":
							return ViewTable.FullName + "." + ShartMappingUtils.DateCreateName;

						case "DATECHANGE":
							return ViewTable.FullName + "." + ShartMappingUtils.DateChangeName;

						default:
							throw new ArgumentException("FieldName");
					}
				}

				Column clmn = GetFieldMapping(fsupp);
				return clmn.Owner.FullName + ".[" + clmn.Name + "]";
			}
			return fsupp;
		}

		public override Column GetFieldMapping(string fieldName)
		{
			Column col = base.GetFieldMapping(fieldName);
			if (col == null)
				col = MainTable.GetColumn(fieldName);
			return col;
		}

		public override Column IDColumn
		{
			get { return MainTable.GetColumn(IDName); }
		}

    	public Table MainTable
		{
			get
			{
				if (_MainTable == null)
					_MainTable = _schema.GetTable(GetMainTableName());
				return _MainTable;
			}
		}

        public override DbType IDDbType
        {
            get
            {
                return DbType.Int32;
            }
        }

        string GetMainTableName()
        {
            return ShartMappingUtils.GetDimMainTableName(_dim);
        }

        protected override string GetViewTableName()
        {
            return ShartMappingUtils.GetDimViewTableName(_dim);
        }

        public IList<Column> GetAnalysisFieldColumns(string fieldName)
        {
            List<Column> list = new List<Column>(_dim.LevelCount);
            for (int i = 1; i <= _dim.LevelCount; i++)
            {
                string colName = ShartMappingUtils.GetAnalysisColumnName(fieldName, i);
                Column col = ViewTable.GetColumn(colName);
                list.Add(col);
            }
            return list;
        }

        public Column ParentIDColumn
        {
            get 
            {
                return ViewTable.GetColumn(ShartMappingUtils.ParentIDName);
            }
        }

        public override string IDName
        {
            get { return ShartMappingUtils.GetDimIDName(_dim); }
        }

        public IList<Column> GetParentColumns()
        {
            List<Column> list = new List<Column>(_dim.LevelCount + 1);
            for (int i = 0; i <= _dim.LevelCount; i++)
            {
                Column col = MainTable.GetColumn(ShartMappingUtils.GetParentColumnName(i));
                list.Add(col);
            }

            return list;
        }

        public IDimensionMetaData Dimension
        {
            get { return _dim; }
        }

    	public string LevelName
    	{
    		get { return ShartMappingUtils.LevelName; }
    	}

    	public virtual  string VersionID
    	{
    		get { return ShartMappingUtils.VersionIDName; }
    	}
	}

    internal class CubeMapping : EntityMapping, ICubeMapping
    {
        private readonly ICubeMetaData _cube;

        public CubeMapping(ICubeMetaData aCube, OLTPSchema aSchema, IShartModelMapping modelMap) 
            : base(aCube, aSchema, modelMap)
        {
            _cube = aCube;
        }

    	public override string GetFSuppString(string fsupp)
    	{
			if (!fsupp.Contains("."))
			{
				if (fsupp.StartsWith("@"))
				{
					string fid = fsupp.TrimStart('@');
					fid = fid.ToUpper();
					switch (fid)
					{
						case "LONGID":
							return ViewTable.FullName + "." + IDName;
						case "ID":
							return ViewTable.FullName + "." + IDName;

						case "DATECREATE":
							return ViewTable.FullName + "." + ShartMappingUtils.DateCreateName;

						default:
							throw new ArgumentException("FieldName");
					}
				}

				Column clmn = ViewTable.GetColumn(fsupp, true);					
				return clmn.Owner.FullName + "." + clmn.Name;
			}
			return fsupp;
    		
    	}

    	public override Column IDColumn
		{
			get { return ViewTable.GetColumn(IDName); }
		}

        public override string IDName
        {
            get { return ShartMappingUtils.GetCubeIDName(_cube); }
        }

        public override DbType IDDbType
        {
            get
            {
                return DbType.Int64;
            }
        }

        public ICubeMetaData Cube
        {
            get { return _cube; }
        }


        protected override string GetViewTableName()
        {
            return ShartMappingUtils.GetCubeViewTableName(_cube);
        }
    }
}
