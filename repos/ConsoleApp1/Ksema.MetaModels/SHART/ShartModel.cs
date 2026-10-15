using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Framework.Collections;
using Ksema.ObjectModel;
using Ksema.ObjectModel.Serialization;
using Ksema.Framework;
using Microsoft.Practices.EnterpriseLibrary.Validation.Validators;
using Microsoft.Practices.EnterpriseLibrary.Validation;
using Framework.Macro;

namespace Shart.ModelDef
{
    [CSMetaData(typeof(ShartModelMetaDataSerializer))]
    [HasSelfValidation]
	[Serializable]
    public class ShartModelMetaData : IShartModelMetaData, IObjectURIProvider
    {
        [CSAttribute("Name")]
        private string _name = string.Empty;

        [CSReference("SqlTemplates", true, null)]
        CSMultiLink<ShartModelMetaData, SQLTemplateMetaData> _sqlTemplates;

        [CSReference("Contents", true, "Model")]
        private CSKeyMultiLink<ShartModelMetaData, OLAPObjectMetaData, string, OLAPObjectKey> _contents;

//		[CSReference( "DatabaseFiles", true, "Model" )]
//		private CSMultiLink<ShartModelMetaData, DatabaseFile> _databaseFiles = null;

		[CSReference( "DatabaseFilegroups", true, "Model" )]
		private CSMultiLink<ShartModelMetaData, DatabaseFilegroup> _databaseFilegroups = null;

		[CSReference( "Contexts", true, null )]
		private CSMultiLink<ShartModelMetaData, ContextMetaData> _contexts;

		[CSReference( "Database", true, "Owner" )]
		private CSSingleLink<ShartModelMetaData, DatabaseMetaData> _database;


		[CSReference( "UpdateScripts", true, null )]
		CSMultiLink<ShartModelMetaData, UpdateScriptMetaData> _updateScripts;


        [CSAttribute("DefaultClassName")]
        private string _defaultClassName;

        [CSAttribute("DefaultSetName")]
        private string _defaultSetName;

        [CSAttribute("DefaultRefName")]
        private string _defaultRefName;

        [CSAttribute("DefaultBackRefName")]
        private string _defaultBackRefName;

        [CSAttribute("Namespace")]
        string _namespace;

        [CSAttribute("AssemblyName")]
        string _assemblyName;

        private const string c_defaultClassName = "#ctx.Name;NodeData";
        private const string c_defaultSetName = "#ctx.Name;NodeDataSet";
        private const string c_defaultRefName = "#ctx.Name;Node";
        private const string c_defaultBackRefName = "#ctx.Owner.Name;Set";

        public ShartModelMetaData()
        {
            CSPropMgr.Instance.setupObject(this, false);
        }

        public IList<SQLTemplateMetaData> SqlTemplates
        {
            get { return _sqlTemplates; }
        }

		public IList<UpdateScriptMetaData> UpdateScripts
		{
			get
			{
				return _updateScripts;
			}
		}

		public IList<ContextMetaData> Contexts
		{
			get
			{
				return _contexts;
			}
		}

        [MetaDataPropertyAttribute]
        public string Namespace
        {
            get { return _namespace; }
            set { _namespace = value; }
        }

        [MetaDataPropertyAttribute]
        public string AssemblyName
        {
            get { return _assemblyName; }
            set { _assemblyName = value; }
        }

        [MetaDataPropertyAttribute]
        public string DefaultClassName
        {
            get { return string.IsNullOrEmpty(_defaultClassName) ? c_defaultClassName : _defaultClassName; }
            set { _defaultClassName = value; }
        }

        [MetaDataPropertyAttribute]
        public string DefaultSetName
        {
            get { return string.IsNullOrEmpty(_defaultSetName) ? c_defaultSetName : _defaultSetName; }
            set { _defaultSetName = value; }
        }

        [MetaDataPropertyAttribute]
        public string DefaultRefName
        {
            get { return string.IsNullOrEmpty(_defaultRefName) ? c_defaultRefName : _defaultRefName; }
            set { _defaultRefName = value; }
        }

        [MetaDataPropertyAttribute]
        public string DefaultBackRefName
        {
            get { return string.IsNullOrEmpty(_defaultBackRefName) ? c_defaultBackRefName : _defaultBackRefName; }
            set { _defaultBackRefName = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

		[NonSerialized]
        MacroManager _macroMgr;
        internal IMacroManager MacroMgr
        {
            get 
            {
                if (_macroMgr == null)
                {
					lock(this)
						if (_macroMgr == null)
						{
							_macroMgr = new MacroManager("#");
							_macroMgr.RegisterMacro("Lib", new StandardMacro());
							_macroMgr.RegisterMacro("Model", this);
						}
                }
                return _macroMgr;
            }
        }


		//public IList<DatabaseFile> DatabaseFiles
		//{
		//    get
		//    {
		//        return _databaseFiles;
		//    }
		//}

		public DatabaseMetaData Database
		{
			get
			{
				return _database.LinkedObject;
			}
			set
			{
				_database.LinkedObject = value;
			}
		}

		public IList<DatabaseFilegroup> DatabaseFilegroups
		{
			get
			{
				return _databaseFilegroups;
			}
		}

		public void AddFilegroup( DatabaseFilegroup group )
		{
			if( !_databaseFilegroups.Any( g => ReferenceEquals( group, g ) ) )
			{
				if( _databaseFilegroups.Any( g => String.Compare( group.Name, g.Name, true ) == 0 ) )
					throw new Exception( String.Format( "Database filegroup with name '{0}' already exists", group.Name ) );
				_databaseFilegroups.Add( group );
			}
		}

		public void RemoveUpdateScript( UpdateScriptMetaData data )
		{
			_updateScripts.Remove( data );
		}


		public void RemoveFilegroup( DatabaseFilegroup group )
		{
			//Если вообще есть такая группа
			if( _databaseFilegroups.Any( g => ReferenceEquals( group, g )) )
			{
//				if( group.IsDefault )
//					throw new Exception( "Default filegroup can not be removed" );
				DatabaseFilegroup defaultGroup = _databaseFilegroups.FirstOrDefault( g => g.IsDefault );
				foreach( OLAPObjectMetaData obj in GetAllContentsMetaData() )
				{
					if( ReferenceEquals( obj.MainTableFilegroup, group ) )
						obj.MainTableFilegroup = defaultGroup;
					if( ReferenceEquals( obj.ViewTableFilegroup, group ) )
						obj.ViewTableFilegroup = defaultGroup;
					foreach( var ix in obj.Indexes.Where( i => ReferenceEquals( i.Filegroup, group ) ) )
						ix.Filegroup = defaultGroup;
				}
				_databaseFilegroups.Remove( group );
			}
		}

        internal void UpdateContentMetaDataKey(string oldSysName)
        {
            OLAPObjectMetaData olapObj;
            if (_contents.TryGetValue(oldSysName, out olapObj))
            {
                _contents.removeObjectByKey(oldSysName);
                string newKey = OLAPObjectKey.GetKey(olapObj);
                if (!_contents.ContainsKey(newKey))
                    _contents.addLinkedObject(olapObj);
                else
                    throw new Exception(string.Format("Object with the same key, already exists. Key: {0}", newKey));
            }
        }

        public void RemoveCube(CubeMetaData cube)
        {
			RemoveLinkedObjects( cube );

			_contents.removeLinkedObject( cube );
            //			bool result = _contents.Remove( OLAPObjectKey.GetKey( cube ) );
            //			return result;
        }

        public bool RemoveDimension(DimensionMetaData dim)
        {
            string errorMessage;
            return TryRemoveDimension(dim, out errorMessage, true);
        }

        public bool TryRemoveDimension(DimensionMetaData dim, out string errorMessage)
        {
            return TryRemoveDimension(dim, out errorMessage, false);
        }

		private void RemoveLinkedObjects( OLAPObjectMetaData obj )
		{
			obj.TriggerTemplates.Clear();

			foreach( ContextMetaData ctx in new List<ContextMetaData>( obj.Contexts ) )
			{
				ctx.AttachedObjects.Remove( obj );
			}
			foreach( FieldMetaData fld in obj.GetAllFields() )
			{
				foreach( ContextMetaData fieldCtx in new List<ContextMetaData>( fld.Contexts ) )
				{
					fieldCtx.AttachedObjects.Remove( fld );
				}
				fld.Contexts.Clear();
			}
			obj.Contexts.Clear();

			foreach( var script in UpdateScripts.Where( s => ReferenceEquals( s.Target, obj ) ) )
			{
				script.Target = null;
			}
		}

        private bool TryRemoveDimension(DimensionMetaData dim, out string errorMessage, bool throwException)
        {
            bool result;
            ReferenceMetaData reference = GetReferenceByDimension(dim);
            if (reference == null)
            {
				RemoveLinkedObjects( dim );

				_contents.removeLinkedObject( dim );

                result = true;
                errorMessage = "";
                //result = _contents.Remove( OLAPObjectKey.GetKey( dim ) );
                //if( !result )
                //    errorMessage = string.Format( "Не удалось удалить измерение {0}", dim.Name );
                //else
                //    errorMessage = "";
            }
            else
            {
                result = false;
                errorMessage = string.Format("Невозможно удалить измерение {0} т.к. на него ссылается {1}", dim.Name, reference.Owner.Name + "." + reference.Name);
                if (throwException)
                    throw new ConstraintException(errorMessage);
            }
            return result;
        }

		public SQLTemplateMetaData TryGetSQLTemplateByGuid( string guid )
		{
			var result = _sqlTemplates.FirstOrDefault( obj => obj.GUID == guid );
			return result;
		}

		public UpdateScriptMetaData TryGetUpdateScriptByGuid( string guid )
		{
			UpdateScriptMetaData result = _updateScripts.FirstOrDefault( obj => obj.GUID == guid );
			return result;
		}

        private ReferenceMetaData GetReferenceByDimension(DimensionMetaData dim)
        {
            ReferenceMetaData result = null;
            foreach (OLAPObjectMetaData olapObj in _contents.Values)
            {
                result = olapObj.GetReferenceByDimension(dim);
                if (result != null)
                    break;
            }
            return result;
        }


        //public bool HasKey

        public bool HasContentMetaData(string sysName)
        {
            return _contents.Keys.Contains(sysName);
        }

        public IOLAPObjectMetaData TryGetContentMetaData(string sysName)
        {
            IOLAPObjectMetaData result = null;
            if (_contents.Keys.Contains(sysName))
                result = _contents[sysName];
            return result;
        }

		public OLAPObjectMetaData TryGetContentMetaDataByGuid( string guid )
		{
			OLAPObjectMetaData result = null;
			result =_contents.Values.FirstOrDefault( obj => obj.GUID == guid );
			return result;
		}

        public ICubeMetaData TryGetCubeMetaData(string sysName)
        {
            ICubeMetaData result = null;
            IOLAPObjectMetaData obj = TryGetContentMetaData(sysName);
            if (obj is ICubeMetaData)
            {
                result = (ICubeMetaData)obj;
            }
            return result;
        }

        public IDimensionMetaData TryGetDimensionMetaData(string sysName)
        {
            IDimensionMetaData result = null;
            IOLAPObjectMetaData obj = TryGetContentMetaData(sysName);
            if (obj is IDimensionMetaData)
            {
                result = (IDimensionMetaData)obj;
            }
            return result;
        }

        public IOLAPObjectMetaData GetContentMetaData(string sysName)
        {
            return _contents[sysName];
        }

        public IDimensionMetaData GetDimensionMetaData(string sysName)
        {
            return (IDimensionMetaData)_contents[sysName];
        }

        public ICubeMetaData GetCubeMetaData(string sysName)
        {
            return (ICubeMetaData)_contents[sysName];
        }

        public void AddCube(CubeMetaData obj)
        {
            AddContent(obj);
        }

        public void AddDimension(DimensionMetaData obj)
        {
            AddContent(obj);
        }

        private void AddContent(OLAPObjectMetaData obj)
        {
            _contents.addLinkedObject(obj);

			var defaultFG = DatabaseFilegroups.FirstOrDefault( g => g.IsDefault );
			if( obj.MainTableFilegroup == null || !DatabaseFilegroups.Contains( obj.MainTableFilegroup ) )
				obj.MainTableFilegroup = defaultFG;
			if( obj.ViewTableFilegroup == null || !DatabaseFilegroups.Contains( obj.ViewTableFilegroup ) )
				obj.ViewTableFilegroup = defaultFG;
        }

        public ICollection<IDimensionMetaData> GetAllDimensionsMetaData()
        {
            List<IDimensionMetaData> result = new List<IDimensionMetaData>(_contents.Values.Count);
            foreach (OLAPObjectMetaData data in _contents.Values)
            {
                if (data is DimensionMetaData)
                    result.Add((DimensionMetaData)data);
            }
            return result;
        }

        public ICollection<ICubeMetaData> GetAllCubesMetaData()
        {
            List<ICubeMetaData> result = new List<ICubeMetaData>(_contents.Values.Count);
            foreach (OLAPObjectMetaData data in _contents.Values)
            {
                if (data is CubeMetaData)
                    result.Add((CubeMetaData)data);
            }
            return result;
        }

        public ICollection<IOLAPObjectMetaData> GetAllContentsMetaData()
        {
            List<IOLAPObjectMetaData> result = new List<IOLAPObjectMetaData>(_contents.Values.Count);
            foreach (OLAPObjectMetaData data in _contents.Values)
            {
                result.Add(data);
            }
            return result;
        }

        #region IObjectURIProvider Members

        public ObjectURI CreateURI()
        {
            return new ObjectURI("ShartModelMetaData/" + Name);
        }

        #endregion

        [SelfValidation]
        public void DoValidate(ValidationResults results)
        {
            Dictionary<string, OLAPObjectMetaData> olapObjs = new Dictionary<string, OLAPObjectMetaData>();
            foreach (OLAPObjectMetaData olapObj in GetAllContentsMetaData())
            {
                ValidationResults validResults = null;
                if (olapObj is DimensionMetaData)
                {
                    validResults = Validation.Validate((DimensionMetaData)olapObj);
                }
                else if (olapObj is CubeMetaData)
                {
                    validResults = Validation.Validate((CubeMetaData)olapObj);
                }
                if (validResults != null && !validResults.IsValid)
                {
                    foreach (ValidationResult vr in validResults)
                    {
                        results.AddResult(vr);
                    }
                }


                if (olapObj.TablePostfix != null && olapObj.TablePostfix.Trim().Length > 0)
                {
                    if (olapObjs.ContainsKey(olapObj.TablePostfix.ToLower()))
                    {
                        const string errorMeassage = "Value must be unique within the model";
                        ValidationResult vr = new ValidationResult(errorMeassage, olapObj, "TablePostfix", null, null);
                        results.AddResult(vr);
                    }
                    else
                    {
                        olapObjs.Add(olapObj.TablePostfix.ToLower(), olapObj);
                    }
                }
            }
        }

        /// <summary>
        /// Cоздает индексы на поле Code у измерений, если их нет
        /// </summary>
        public void AddDimsCodeFieldIndex()
        {
            foreach (DimensionMetaData dimensionMetaData in GetAllDimensionsMetaData())
            {
                dimensionMetaData.TablePostfix = dimensionMetaData.TablePostfix;
            }
        }

        public void UpdateDimensionLevels()
        {
            foreach (DimensionMetaData dimensionMetaData in GetAllDimensionsMetaData())
            {
                dimensionMetaData.UpdateDimensionLevels();
            }
        }

        /// <summary>
        /// Переносит содержимое из models в новую композитную модель. 
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        public static ShartCompositeModel Combine(IShartModelMetaData[] models)
        {
            ShartCompositeModel compositeModel = new ShartCompositeModel();
            foreach (IShartModelMetaData model in models)
                compositeModel.NestedModels.Add(model);

            compositeModel.ReindexAll();
            return compositeModel;
        }
    }

	[Serializable]
	public class OLAPObjectKey : IIndexer<OLAPObjectMetaData, string>
	{
		#region IIndexer Members
		string IIndexer<OLAPObjectMetaData, string>.GetIndexKey( OLAPObjectMetaData obj )
		{
			return GetKey( obj );
		}

	    public IEqualityComparer<string> GetKeyComparer()
	    {
            return StringComparer.OrdinalIgnoreCase;
	    }

	    #endregion

		public static string GetKey( IOLAPObjectMetaData obj )
		{
			if( obj == null )
				throw new ArgumentNullException( "obj" );
			return obj.Name;
		}
	}

	[CSMetaData( typeof( ShartModelMetaDataSerializer ) )]
	public class ShartModelBackup
	{
		[CSAttribute( "Version" )]
		private int _version;
		//private ShartModelMetaData _shartModel;

		[CSReference( "ShartModel", false )]
		private CSSingleLink<ShartModelBackup, ShartModelMetaData> _shartModel = null;

		public ShartModelBackup()
		{
			CSPropMgr.Instance.setupObject( this, false );
		}

		public ShartModelBackup( ShartModelMetaData model )
			: this()
		{
			ShartModel = model;
		}

		public int Version
		{
			get { return _version; }
			set { _version = value; }
		}

		public ShartModelMetaData ShartModel
		{
			get
			{
				return _shartModel.LinkedObject;
			}
			set
			{
				_shartModel.LinkedObject = value;
			}
		}
	}
}
