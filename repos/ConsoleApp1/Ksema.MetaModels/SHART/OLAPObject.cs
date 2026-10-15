using System;
using System.Collections.Generic;
using System.Data;
using Framework.Collections;
using Framework.Macro;
using Framework.Utils;
using Ksema.ObjectModel;
using Ksema.MetaModels.SHART.Shart2OLTP;
using Microsoft.Practices.EnterpriseLibrary.Validation.Validators;
using Ksema.Framework;
using Microsoft.Practices.EnterpriseLibrary.Validation;
using System.Linq;
using Ksema.Framework.Caching;

namespace Shart.ModelDef
{
    /// <summary>
	/// OLAP Объект
	/// </summary>
    [HasSelfValidation]
	[Serializable]
	public class OLAPObjectMetaData : BaseMetaData, IOLAPObjectMetaData, IObjectURIProvider
	{

		[CSReference( "Model", false, "Contents" )]
		private CSSingleLink<OLAPObjectMetaData, ShartModelMetaData> _model;

		[CSReference( "Indexes", true, "Owner" )]
		private CSMultiLink<OLAPObjectMetaData, IndexMetaData> _indexes;
		[CSReference( "Fields", true, "Owner" )]
		private CSKeyMultiLink<OLAPObjectMetaData, FieldMetaData, string, FieldKey> _fields;

        [CSReference("TriggerTemplates", false, "AttachedObjects")]
        private CSMultiLink<OLAPObjectMetaData, SQLTemplateMetaData> _triggerTemplates;

		[CSReference( "Contexts", false, "AttachedObjects" )]
		private CSMultiLink<BaseMetaData, ContextMetaData> _contexts;

		[CSAttribute( "IsHistoric" )]
		private bool _isHistoric;

		[CSAttribute( "TablePostfix" )]
		private string _tablePostfix = string.Empty;

		[CSReference( "MainTableFilegroup" )]
		private CSSingleLink<OLAPObjectMetaData, DatabaseDataSpace> _mainTableFilegroup;

		[CSReference( "ViewTableFilegroup" )]
		private CSSingleLink<OLAPObjectMetaData, DatabaseDataSpace> _viewTableFilegroup;

        [CSAttribute("SetTemplate")]
		private string _SetTemplate = string.Empty;

        [CSAttribute("IsSystem")]
        bool _isSystem;

		[CSAttribute( "MainTablePartitionedField" )]
		private string _mainTablePartitionedField;

		[CSAttribute( "ViewTablePartitionedField" )]
		private string _viewTablePartitionedField;


		[NonSerialized]
		SingleItemDataCache<ICollection<IFieldMetaData>> _allFields;
		[NonSerialized]
		SingleItemDataCache<ICollection<IDataFieldMetaData>> _allDataFields;
		[NonSerialized]
		SingleItemDataCache<ICollection<IReferenceMetaData>> _allRefs;

		private SingleItemDataCache<ICollection<IFieldMetaData>> AllFields
		{
			get
			{
				if (_allFields == null)
					lock (this)
						if (_allFields == null)
							_allFields = new SingleItemDataCache<ICollection<IFieldMetaData>>("allFields" + Guid.NewGuid(), GetAllFieldsImpl);
				return _allFields;
			}
		}

		private SingleItemDataCache<ICollection<IReferenceMetaData>> AllRefs
		{
			get
			{
				if (_allRefs == null)
					lock (this)
						if (_allRefs == null)
							_allRefs = new SingleItemDataCache<ICollection<IReferenceMetaData>>("allRefs" + Guid.NewGuid(), GetReferencesImpl);
				return _allRefs;
			}
		}

		private SingleItemDataCache<ICollection<IDataFieldMetaData>> AllDataFields
		{
			get
			{
				if (_allDataFields == null)
					lock (this)
						if (_allDataFields == null)
							_allDataFields = new SingleItemDataCache<ICollection<IDataFieldMetaData>>("allDataFields" + Guid.NewGuid(), GetDataFieldsImpl);
				return _allDataFields;
			}
		}

	    public OLAPObjectMetaData()
		{
			CSPropMgr.Instance.setupObject( this, false );
			Initialize();
		}

		private void Initialize()
		{
			_fields.LinkAdded += _fields_LinkAdded;
			_fields.LinkDeleted += _fields_LinkDeleted;
		}

		void _fields_LinkDeleted(object sender, LinkChangedEventArgs args)
		{
			ResetFieldsCache();
		}

		void _fields_LinkAdded(object sender, LinkChangedEventArgs args)
		{
			ResetFieldsCache();
		}


        [MetaDataPropertyAttribute]
	    public string SetTemplate
	    {
	        get { return _SetTemplate; }
            set 
			{ 
				_SetTemplate = value;
				_setName = null;
			}
	    }

		string _className;
        public string ClassName
        {
            get
            {
				if (string.IsNullOrEmpty(_className))
				{
					string macro = NameTemplate;
					if (string.IsNullOrEmpty(macro))
						macro = Model.DefaultClassName;
					_className = (string) MacroMgr.ParseString(macro, "ctx", this);
				}
				return _className;
            }
        }

		protected override void OnNameTemplateChanged()
		{
			base.OnNameTemplateChanged();
			_className = null;
		}

		string _setName;
        public string SetName
        {
            get
            {
				if (string.IsNullOrEmpty(_setName))
				{
					string macro = SetTemplate;
					if (string.IsNullOrEmpty(macro))
						macro = Model.DefaultSetName;
					_setName = (string) MacroMgr.ParseString(macro, "ctx", this);
				}
				return _setName;
            }
        }

        public string ModelName
        {
            get { return Model.Name; }
        }

        protected IMacroManager MacroMgr
        {
            get { return Model.MacroMgr; }
        }


	    public bool IsSystem
	    {
	        get { return _isSystem; }
	        set { _isSystem = value; }
	    }

	    [NotNullValidator]
        [StringLengthValidator( 1, 64, MessageTemplate = "Длина строки должна быть от 1 до 64 символов" )]
        [RegexValidator( @"[0-9a-zA-Z\s\-\.']*" )]
        public override string Name
        {
            get { return _name; }
            set
            {
                if( Model != null && _name != value )
                {
                    string newName = value;
                    IOLAPObjectMetaData olapObj = Model.TryGetContentMetaData( newName );
                    if( olapObj != null && !ReferenceEquals( this, olapObj ) )
                    {
                        throw new Exception( string.Format( "Object with the same key, already exists. Key: {0}", newName ) );
                    }
                    string oldName = _name;
                    _name = newName;
                    Model.UpdateContentMetaDataKey( oldName );
                }
                else
                {
                    _name = value;
                }
            }
        }


        IShartModelMetaData IOLAPObjectMetaData.Model
        {
            get { return _model.LinkedObject; }
        }

		public ShartModelMetaData Model
		{
			get { return _model.LinkedObject; }
			set { _model.LinkedObject = value; }
		}

		public override string ToString()
		{
			return Name;
		}

		public IList<IndexMetaData> Indexes
		{
			get { return _indexes; }
		}

        public IList<SQLTemplateMetaData> TriggerTemplates
        {
            get { return _triggerTemplates; }
        }

		public IList<ContextMetaData> Contexts
		{
			get
			{
				return _contexts;
			}
		}

		public bool IsHistoric
		{
			get
			{
			     return _isHistoric;
			}
			set
			{
			    _isHistoric = value;
                //if( _isHistoric )
                //{
                //    foreach(IndexMetaData index in Indexes)
                //    {
                //        if( index.IsUnique )
                //        {
                //            index.IsUnique = false;
                //        }
                //    }
                //}
                //else
                //{
                //    UpdateCodeIndex();
                //}
			}
		}

        [NotNullValidator]
        [StringLengthValidator( 1, 255, MessageTemplate = "Длина строки должна быть от 1 до 255 символов" )]
        [RegexValidator( @"[0-9a-zA-Z\s\-\.']*" )]
        public string TablePostfix
		{
			get { return _tablePostfix; }
			set
			{
			     _tablePostfix = value;
                 //UpdateCodeIndex();
			}
		}

	    public virtual string IDName
	    {
	        get { throw new NotSupportedException(); }
	    }

		public virtual DatabaseDataSpace MainTableFilegroup
		{
			get
			{
				return _mainTableFilegroup.LinkedObject;
			}
			set
			{
				_mainTableFilegroup.LinkedObject = value;

				var index = Indexes.FirstOrDefault( i => i.IsClustered && i.DBTable == DatabaseTable.MainTable );
				if( index != null )
					index.Filegroup = value;
			}
		}

		public DatabaseDataSpace ViewTableFilegroup
		{
			get
			{
				return _viewTableFilegroup.LinkedObject;
			}
			set
			{
				_viewTableFilegroup.LinkedObject = value;
				var index = Indexes.FirstOrDefault( i => i.IsClustered && i.DBTable == DatabaseTable.ViewTable );
				if( index != null )
					index.Filegroup = value;
			}
		}

		/// <summary>
		/// Поле используемое в партицировании
		/// </summary>
		public string MainTablePartitionedField
		{
			get
			{
				return _mainTablePartitionedField;
			}
			set
			{
				_mainTablePartitionedField = value;
			}
		}

		public string ViewTablePartitionedField
		{
			get
			{
				return _viewTablePartitionedField;
			}
			set
			{
				_viewTablePartitionedField = value;
			}
		}

		public IDictionary<string, FieldMetaData> Fields
		{
            get { return _fields; }
		}

		public void AddField( FieldMetaData field )
		{
            _fields.addLinkedObject(field);

            if( this is IDimensionMetaData
                && field is DataFieldMetaData
                && String.Compare( field.Name, ShartMappingUtils.CodeName, true ) == 0 )
            {
                AddFieldIndex( field, true  );
            }
		}

        private void AddFieldIndex( FieldMetaData field, bool unique )
        {
            string indexName = string.Format( "IX_{0}_{1}", ShartMappingUtils.GetDimMainTableName( this ), field.Name );
            IndexMetaData index = new IndexMetaData();
        	index.GUID = Guid.NewGuid().ToString();
        	index.Name = indexName;
        	index.IsUnique = (unique && !IsHistoric);
        	index.Version = field.Version;
        	IndexFieldMetaData indexField = new IndexFieldMetaData {Field = field};
            index.Fields.Add(indexField);
            Indexes.Add(index);

			index.Filegroup = (index.DBTable == DatabaseTable.MainTable ? MainTableFilegroup : ViewTableFilegroup);
        }

        private void UpdateCodeIndex()
        {
            DataFieldMetaData field = GetFieldByName( ShartMappingUtils.CodeName, false ) as DataFieldMetaData;
            if( field != null)
            {
                if( !UpdateIndex( field ) )
                {
                    AddFieldIndex( field, true );
                }
            }
        }

        private bool UpdateIndex( IDataFieldMetaData field )
        {
            bool result = false;
            string indexName = string.Format( "IX_{0}_{1}", ShartMappingUtils.GetDimMainTableName( this ), field.Name );
            foreach(IndexMetaData index in Indexes )
            {
                if( index.Fields.Count == 1
                    && index.Fields[0].FieldName == field.Name )
                {
                    if( index.Name != indexName )
                        index.Name = indexName;
                    index.IsUnique = !IsHistoric;

                    result = true;
                    break;
                }
            }
            return result;
        }

		public bool RemoveField( FieldMetaData field )
		{
			string errorMessage;
			return TryRemoveField( field, out errorMessage, true );
		}

		public bool TryRemoveField( FieldMetaData field, out string errorMessage )
		{
			return TryRemoveField( field, out errorMessage, false );
		}

		private bool TryRemoveField( FieldMetaData field, out string errorMessage, bool throwException  )
		{
			bool result;
			IndexMetaData index = GetIndexByField( field );
			if( index == null )
			{
				field.Contexts.Clear();
				_fields.removeLinkedObject( field );// Remove( FieldKey.GetIndexKey( field ) );
				errorMessage = "";
				result = true;

				foreach( var script in Model.UpdateScripts.Where( s => ReferenceEquals( s.Target, field ) ) )
				{
					script.Target = null;
				}
			}
			else
			{
				result = false;
				errorMessage = string.Format( "Невозможно удалить поле {0} т.к. оно входит в индекс {1}", field.Name, index.Name );
				if( throwException )
					throw new ConstraintException( errorMessage ); 
			}
			return result;
		}

		private IndexMetaData GetIndexByField( IFieldMetaData field )
		{
			IndexMetaData result = null;
			foreach( IndexMetaData index in Indexes )
			{
				foreach( BaseIndexFieldMetaData indexField in index.Fields )
				{
                    if( indexField is IndexFieldMetaData
                        && ReferenceEquals( ((IndexFieldMetaData)indexField).Field, field ) )
                    {
                        result = index;
                        break;
                    }
                }
				if( result != null )
					break;
			}
			return result;
		}

		public ReferenceMetaData GetReferenceByDimension( DimensionMetaData dim )
		{
			ReferenceMetaData result = null;
			foreach( FieldMetaData field in _fields.Values)
			{
				if( field is ReferenceMetaData
					&& ((ReferenceMetaData)field).Target == dim )
				{
					result = (ReferenceMetaData) field;
					break;
				}
			}
			return result;
		}

		public IFieldMetaData GetFieldByName( string name )
		{
			return GetFieldByName( name, false );
		}

		void ResetFieldsCache()
		{
			//*
			lock (this)
			{
				_allFields = null;
				_allDataFields = null;
				_allRefs = null;
			}
			/*/
			AllFields.Clear();
			AllDataFields.Clear();
			AllRefs.Clear();
			//*/
		}


		ICollection<IFieldMetaData> GetAllFieldsImpl()
		{
			var allFields = new List<IFieldMetaData>(_fields.Values.Count);
			foreach (var f in _fields.Values)
				allFields.Add(f);
			return allFields;
		}

    	public ICollection<IFieldMetaData> GetAllFields()
		{
			return AllFields.Value;
		}

    	public ICollection<IIndexMetaData> GetIndexes()
		{
			List<IIndexMetaData> result = new List<IIndexMetaData>( _indexes.Count );
			foreach( IIndexMetaData data in _indexes )
			{
				result.Add( data );
			}
			return result;
		}

		public IndexMetaData TryGetIndexByGuid( string guid )
		{
			var result = _indexes.FirstOrDefault( indx => indx.GUID == guid );
			return result;
		}


		public ICollection<IDataFieldMetaData> GetDataFields()
		{
			return AllDataFields.Value;
		}

    	ICollection<IDataFieldMetaData> GetDataFieldsImpl()
		{
			var allDataFields = new List<IDataFieldMetaData>(_fields.Values.Count);
			foreach (FieldMetaData f in _fields.Values)
			{
				if (f is IDataFieldMetaData)
					allDataFields.Add((IDataFieldMetaData)f);
			}
			return allDataFields;
		}

		public ICollection<IReferenceMetaData> GetReferences()
		{
			return AllRefs.Value;
		}

    	ICollection<IReferenceMetaData> GetReferencesImpl()
	    {
			var allRefs = new List<IReferenceMetaData>(_fields.Values.Count);
			foreach (FieldMetaData f in _fields.Values)
			{
				if (f is IReferenceMetaData)
					allRefs.Add((IReferenceMetaData)f);
			}
			return allRefs;
	    }

	    public FieldMetaData GetFieldByName( string name, bool required )
		{
			FieldMetaData f;
            _fields.TryGetValue(name, out f);
			if( required && f == null )
				throw new NotFoundException("Required field is not found: " + name);
			return f;
		}

		public FieldMetaData TryGetFieldByGuid( string guid )
		{
			var result = _fields.Values.FirstOrDefault( fld => fld.GUID == guid );
			return result;
		}

		public virtual IList<IFieldMetaData> GetSystemFields( DatabaseTable dbTable)
		{
			return null;
		}

    	public string FriendlyName
    	{
    		get { return Description!=null ? string.Format("{0} ({1})", Description ,Name) : Name; }
    	}

        #region IObjectURIProvider Members

        public ObjectURI CreateURI()
        {
            ObjectURI result = new ObjectURI( "olapobject/" + GUID );
            return result;
        }

        #endregion

        [SelfValidation]
        public void DoValidate( ValidationResults results )
        {
			if( !(this is ICubeMetaData ) && MainTableFilegroup == null )
			{
				string errorMeassage = "Main table filegroup must be set";
				ValidationResult vr = new ValidationResult( errorMeassage, this, "MainTableFilegroup", null, null );
				results.AddResult( vr );
			}

			if( ViewTableFilegroup == null )
			{
				string errorMeassage = "View table filegroup must be set";
				ValidationResult vr = new ValidationResult( errorMeassage, this, "ViewTableFilegroup", null, null );
				results.AddResult( vr );
			}


			if( !(this is ICubeMetaData) && MainTableFilegroup is IPartitionScheme
				&& String.IsNullOrEmpty( MainTablePartitionedField ) )
			{
				string errorMeassage = "Main table partition field must be set";
				ValidationResult vr = new ValidationResult( errorMeassage, this, "MainTablePartitionedField", null, null );
				results.AddResult( vr );
			}

			if( ViewTableFilegroup is IPartitionScheme
				&& String.IsNullOrEmpty( ViewTablePartitionedField ) )
			{
				string errorMeassage = "View table partition field must be set";
				ValidationResult vr = new ValidationResult( errorMeassage, this, "ViewTablePartitionedField", null, null );
				results.AddResult( vr );
			}

            if( Fields.Count == 0 )
            {
                const string errorMeassage = "OLAPObjectMetaData must contain at least one field";
                ValidationResult vr = new ValidationResult( errorMeassage, this, "Fields", null, null );
                results.AddResult( vr );
            }
            else
            {
                foreach(FieldMetaData field in GetAllFields())
                {
                    ValidationResults validResults = null;
                    if( field is ReferenceMetaData )
                    {
                        validResults = Validation.Validate( (ReferenceMetaData) field );
                    }
                    else if( field is DataFieldMetaData )
                    {
                        validResults = Validation.Validate( (DataFieldMetaData) field );
                    }
                    if( validResults != null && !validResults.IsValid )
                    {
                        foreach(ValidationResult vr in validResults)
                        {
                            results.AddResult( vr );
                        }
                    }
                }

                int mainTablePKCount = 0;
                int viewTablePKCount = 0;
                int mainTableClusteredCount = 0;
                int viewTableClusteredCount = 0;
                foreach(IndexMetaData index in Indexes)
                {
                    if( index.IsPrimaryKey )
                    {
                        if( index.DBTable == DatabaseTable.MainTable )
                            mainTablePKCount++;
                        else
                            viewTablePKCount++;
                    }
                    if( index.IsClustered )
                    {
                        if( index.DBTable == DatabaseTable.MainTable )
                            mainTableClusteredCount++;
                        else
                            viewTableClusteredCount++;
                    }
                    ValidationResults validResults = Validation.Validate( index );
                    if( !validResults.IsValid )
                    {
                        foreach(ValidationResult vr in validResults)
                        {
                            results.AddResult( vr );
                        }
                    }
                }

                if( mainTablePKCount > 1 )
                {
                    const string errorMeassage = "Main table can contain only one primary key index";
                    ValidationResult vr = new ValidationResult( errorMeassage, this, "Indexes", null, null );
                    results.AddResult( vr );
                }
                if( mainTableClusteredCount > 1 )
                {
                    const string errorMeassage = "Main table can contain only one clustered index";
                    ValidationResult vr = new ValidationResult( errorMeassage, this, "Indexes", null, null );
                    results.AddResult( vr );
                }

                if( viewTablePKCount > 1 )
                {
                    const string errorMeassage = "View table can contain only one primary key index";
                    ValidationResult vr = new ValidationResult( errorMeassage, this, "Indexes", null, null );
                    results.AddResult( vr );
                }
                if( viewTableClusteredCount > 1 )
                {
                    const string errorMeassage = "View table can contain only one clustered index";
                    ValidationResult vr = new ValidationResult( errorMeassage, this, "Indexes", null, null );
                    results.AddResult( vr );
                }

				List<string> ixList = new List<string>();
				foreach( var ix in Indexes.Where( ix => ix.DBTable == DatabaseTable.MainTable ) )
				{
					if( ixList.Any( n => String.Compare( n, ix.Name, true ) == 0 ) )
					{
						string errorMeassage = "Duplicate index name " + ix.Name;
						ValidationResult vr = new ValidationResult( errorMeassage, this, "Indexes", null, null );
						results.AddResult( vr );
					}
					ixList.Add( ix.Name );
				}
				ixList.Clear();
				foreach( var ix in Indexes.Where( ix => ix.DBTable == DatabaseTable.ViewTable ) )
				{
					if( ixList.Any( n => String.Compare( n, ix.Name, true ) == 0 ) )
					{
						string errorMeassage = "Duplicate index name " + ix.Name;
						ValidationResult vr = new ValidationResult( errorMeassage, this, "Indexes", null, null );
						results.AddResult( vr );
					}
					ixList.Add( ix.Name );
				}
			}
        }

        internal void UpdateFieldMetaDataKey( string oldSysName )
        {
            FieldMetaData olapObj;
            if( _fields.TryGetValue( oldSysName, out olapObj ) )
            {
                _fields.removeObjectByKey( oldSysName );
                string newKey = FieldKey.GetIndexKey( olapObj );
                if( !_fields.ContainsKey( newKey ) )
                    _fields.addLinkedObject( olapObj );
                else
                    throw new Exception( string.Format( "Field with the same key, already exists. Key: {0}", newKey ) );
            }
        }

        public SqlAction GetSqlAction( SQLTemplateMetaData t )
        {
            var res = new SqlAction
            {
                Sql = string.IsNullOrEmpty(t.Sql) ? "" : (string)MacroMgr.ParseString( t.Sql, "ctx", this ),
                Error = string.IsNullOrEmpty(t.Error) ? "" : (string)MacroMgr.ParseString( t.Error, "ctx", this )
            };
            return res;
        }


		protected OLAPObjectMetaData CloneBase()
		{
			OLAPObjectMetaData result = (OLAPObjectMetaData)MemberwiseClone();
			result.GUID = Guid.NewGuid().ToString();
			CSPropMgr.Instance.setupObject( result, true );
			result.Initialize();
			result.ResetFieldsCache();
			result.ViewTableFilegroup = ViewTableFilegroup;
			result.MainTableFilegroup = MainTableFilegroup;

			//Поля
			foreach( IFieldMetaData field in GetAllFields() )
			{
				FieldMetaData newField = null;
				if( field is DataFieldMetaData )
				{
					var dataField = (DataFieldMetaData)field;
					newField = dataField.Clone();
				}
				else if( field is ReferenceMetaData )
				{
					var refs = (ReferenceMetaData)field;
					newField = refs.Clone();
				}
				if( newField != null )
					result.AddField( newField );
			}

			//Некоторые индексы добавляются атоматически вместе с полями
			result.Indexes.Clear();

			//Индексы
			foreach( IndexMetaData ix in Indexes )
			{
				IndexMetaData newIX = ix.Clone();
				newIX.Filegroup = ix.Filegroup;
				foreach( BaseIndexFieldMetaData ixFld in ix.Fields )
				{
					if( ixFld is IndexFieldMetaData )
					{
						var fld = (IndexFieldMetaData)ixFld;
						IndexFieldMetaData newField = new IndexFieldMetaData();
						newField.Field = (FieldMetaData)result.GetFieldByName( fld.FieldName );
						newIX.Fields.Add( newField );
					}
					else if( ixFld is IndexSystemFieldMetaData )
					{
						var fld = (IndexSystemFieldMetaData)ixFld;
						IndexSystemFieldMetaData newField = fld.Clone();
						newIX.Fields.Add( newField );
					}
					else if( ixFld is IndexSystemIDFieldMetaData )
					{
						var fld = (IndexSystemIDFieldMetaData)ixFld;
						var newField = fld.Clone();
						newIX.Fields.Add( newField );
					}
					else if( ixFld is IndexAnalysisFieldMetaData )
					{
						var fld = (IndexAnalysisFieldMetaData)ixFld;
						var newField = fld.Clone();
						newField.Field = (DataFieldMetaData)result.GetFieldByName( fld.Field.Name );
						newIX.Fields.Add( newField );
					}
					else if( ixFld is IndexParentLevelFieldMetaData )
					{
						var fld = (IndexParentLevelFieldMetaData)ixFld;
						var newField = fld.Clone();
						newIX.Fields.Add( newField );
					}
				}
				result.Indexes.Add( newIX );
			}

			//Шаблоны (коллекция)
			foreach( var trigger in TriggerTemplates )
			{
				result.TriggerTemplates.Add( trigger );
			}

			// Свойства
			foreach (var property in Properties.Values)
			{
				result.AddProperty(property.Clone());
			}

			return result;
		}

		public string UIProfile
		{
			get
			{
				ObjectProperty prop;
				if( Properties.TryGetValue( "UIProfile", out prop ) )
					return prop.StringValue;
				return null;
			}
			set
			{
				ObjectProperty prop;
				if( Properties.TryGetValue( "UIProfile", out prop ) )
				{
					prop.Value = value;
				}
				else
				{
					prop = new StringProperty( "UIProfile", value );
					Properties.Add( "UIProfile", prop );
				}
			}
		}

		#region IOLAPObjectMetaData Members


		public object GetValue( string propertyName )
		{
			ObjectProperty propertyValue;
			if (!Properties.TryGetValue(propertyName, out propertyValue))
				return null;
			return propertyValue.Value;
		}

		public T GetValue<T>( string propertyName )
		{
			return (T)GetValue( propertyName );
		}

		#endregion
	}


	[Serializable]
	public class FieldKey : IIndexer<FieldMetaData, string>
	{
		#region IIndexer Members

		string IIndexer<FieldMetaData, string>.GetIndexKey(FieldMetaData o)
		{
			return GetIndexKey( o );
		}

	    public IEqualityComparer<string> GetKeyComparer()
	    {
            return StringComparer.OrdinalIgnoreCase;
	    }

	    #endregion

		public static string GetIndexKey( FieldMetaData o )
		{
			if( o == null )
				throw new ArgumentNullException( "o" );
			return o.Name;
		}

	}

	/// <summary>
	/// Измерение
	/// </summary>
    [HasSelfValidation]
	[Serializable]
	public class DimensionMetaData : OLAPObjectMetaData, IDimensionMetaData//, ICloneable
	{
		[CSAttribute( "LevelCount" )]
		private int _levelCount = 1;


        [CSAttribute( "IsAccessControlled" )]
        private bool _isAccessControlled = false;


        [CSAttribute( "HasReferenceRoot" )]
	    private bool _hasReferenceRoot;

        [CSAttribute("NeedSearchIndexing")]
	    private bool _needSearchIndexing = true;

        [CSReference( "Levels", true, "Owner" )]
        private CSMultiLink<DimensionMetaData, DimensionLevelMetaData> _levels;



		public DimensionMetaData()
		{
			CSPropMgr.Instance.setupObject( this, false );
		}

		public DimensionMetaData Clone()
		{
			DimensionMetaData result =(DimensionMetaData)base.CloneBase();
			//Уровни
			foreach( var level in Levels )
			{
				var newLevel = level.Clone();
				result.Levels.Add( newLevel );
			}

			return result;
		}
	    
        public IEnumerable<IReferenceMetaData> GetReferencedByListWithBackRefs()
        {
            var list = from dim in Model.GetAllDimensionsMetaData() 
                       from aRef in dim.GetReferences() 
                       where aRef.Target.Name == Name && aRef.HasBackRef
                       select aRef;
            return list;
        }

	    [RangeValidator( 1, RangeBoundaryType.Inclusive, 100, RangeBoundaryType.Inclusive )]
		public int LevelCount
		{
			get { return _levelCount; }
			set
			{
                if( value < 1 )
                    _levelCount = 1;
                else 
                    _levelCount = value > 100 ? 100 : value;
//			    UpdateDimensionLevels();
			}
		}

        [MetaDataPropertyAttribute]
	    public bool IsAccessControlled
	    {
	        get { return _isAccessControlled; }
	        set { _isAccessControlled = value; }
	    }

	    public ICollection<IDataFieldMetaData> GetAnalysisFields()
	    {
	        ICollection<IDataFieldMetaData> dataFields = GetDataFields();
            List<IDataFieldMetaData> result = new List<IDataFieldMetaData>(dataFields.Count);
	        foreach (IDataFieldMetaData field in dataFields)
            {
                if (field.IsAnalysis)
                    result.Add(field);
            }

            return result;
	    }

        public ICollection<IDataFieldMetaData> GetIndexedFields()
        {
            ICollection<IDataFieldMetaData> dataFields = GetDataFields();
            List<IDataFieldMetaData> result = new List<IDataFieldMetaData>(dataFields.Count);
            foreach (IDataFieldMetaData field in dataFields)
            {
                if (field.IsIndexed)
                    result.Add(field);
            }

            return result;
        }


		public override IList<IFieldMetaData> GetSystemFields( DatabaseTable dbTable )
		{
			List<IFieldMetaData> result = new List<IFieldMetaData>();
			if( dbTable == DatabaseTable.MainTable )
			{
				result.Add( new SystemIDFieldMetaData( this ) );
				result.Add( GetNewSystemField( ShartMappingUtils.LevelName, DataType.Integer, 0 ) );
				result.Add( GetNewSystemField( ShartMappingUtils.ParentIDName, DataType.Integer, 0 ) );
				for( int i = 0; i <= LevelCount; i++ )
				{
					SystemParentLevelFieldMetaData parentLevelField = new SystemParentLevelFieldMetaData( i );
					result.Add( parentLevelField );
				}

			}
			else if( dbTable == DatabaseTable.ViewTable )
			{
				result.Add( GetNewSystemField( ShartMappingUtils.VersionIDName, DataType.Integer, 0 ) );

				result.Add( new SystemIDFieldMetaData( this ) );

				result.Add( GetNewSystemField( ShartMappingUtils.GuidName, DataType.String, 36 ) );
				result.Add( GetNewSystemField( ShartMappingUtils.DateCreateName, DataType.DateTime, 0  ) );
				result.Add( GetNewSystemField( ShartMappingUtils.DateChangeName, DataType.DateTime, 0 ) );
				result.Add( GetNewSystemField( ShartMappingUtils.DateCloseName, DataType.DateTime, 0 ) );
				result.Add( GetNewSystemField( ShartMappingUtils.UserIDCreateName, DataType.String, 128 ) );
				result.Add( GetNewSystemField( ShartMappingUtils.UserIDChangeName, DataType.String, 128 ) );

				foreach( DataFieldMetaData field in GetDataFields().Where( f => f.IsAnalysis ) )
				{
					for( int i = 1; i <= LevelCount; i++ )
					{
						SystemAnalysisFieldMetaData analysisField = new SystemAnalysisFieldMetaData( field, i );
						result.Add( analysisField );
					}
				}
			}
			return result;

		}

		public static IFieldMetaData GetNewSystemField( string fieldName, DataType dataType, int dataSize )
		{
			SystemFieldMetaData result = new SystemFieldMetaData();
			result.Name = fieldName;
			result.DataType = dataType;
			result.DataSize = dataSize;
			return result;
		}

	    public override string IDName
        {
            get
            {
                return ShartMappingUtils.GetDimIDName(this);
            }
        }

        public bool HasReferenceRoot
        {
            get { return _hasReferenceRoot; }
            set { _hasReferenceRoot = value; }
        }

        public bool NeedSearchIndexing
        {
            get { return _needSearchIndexing; }
            set { _needSearchIndexing = value; }
        }

        public IList<DimensionLevelMetaData> Levels
        {
            get { return _levels; }
        }

        public string[] GetLevelNames()
        {
            string[] result = new string[_levels.Count];
            for( int i = 0; i < _levels.Count; i++ )
            {
                result[i] = _levels[i].Name;
            }
            return result;
        }

        public string GetLevelName( int levelNumber )
        {
            string result = null;
            DimensionLevelMetaData dimLevel = GetLevel( levelNumber );
            if( dimLevel != null )
                result = dimLevel.Name;
            return result;
        }

        public DimensionLevelMetaData GetLevel( int levelNumber )
        {
            DimensionLevelMetaData result = null;
            for( int i = 0; i < _levels.Count; i++ )
            {
                if( _levels[i].Number == levelNumber )
                {
                    result = _levels[i];
                    break;
                }
            }
            return result;
        }

        public void UpdateDimensionLevels()
        {
            //if( _levels.Count != LevelCount )
            {
                //Удалить лишние уровни
                for( int i = _levels.Count - 1; i >= 0; i-- )
                {
                    DimensionLevelMetaData dimLevel = _levels[i];
                    if( dimLevel.Number < 1
                        || dimLevel.Number > LevelCount )
                    {
                        _levels.Remove( dimLevel );
                    }
                }
                // Добавить новые уровни
                for( int i = 1; i <= LevelCount; i++ )
                {
                    DimensionLevelMetaData level = GetLevel( i );
                    if( level == null )
                    {
                        DimensionLevelMetaData newLevel = new DimensionLevelMetaData
                                                              {
                                                                  Number = i,
                                                                  Name = ("Level" + i)
                                                              };
                        _levels.Add( newLevel );
                    }
                }
            }
        }


	    public SqlAction[] GetTriggerSQL(SQLTemplateType type)
	    {
            var res = from t in TriggerTemplates
                      where t.TemplateType == type
                      select GetSqlAction(t);
            return res.ToArray();
	    }

		#region ICloneable Members

		//object ICloneable.Clone()
		//{
		//    return Clone();
		//}

		#endregion
	}

	/// <summary>
	/// Куб
	/// </summary>
    [HasSelfValidation]
	[Serializable]
	public class CubeMetaData : OLAPObjectMetaData, ICubeMetaData//, ICloneable
	{
		public CubeMetaData()
		{
			CSPropMgr.Instance.setupObject( this, false );
		}

		public override DatabaseDataSpace MainTableFilegroup
		{
			get
			{
				return null;
			}
			set
			{
	
			}
		}

	    public IList<IReferenceMetaData> GetIdentifyFields()
	    {
            List<IReferenceMetaData> idList = new List<IReferenceMetaData>(Fields.Count);
            foreach (FieldMetaData field in Fields.Values)
            {
                if (field is IReferenceMetaData)
                {
                    if (((IReferenceMetaData)field).IsIdentifying)
                        idList.Add((IReferenceMetaData) field);
                }
            }

            return idList;
	    }

        public override string IDName
        {
            get
            {
                return ShartMappingUtils.GetCubeIDName(this);
            }
        }


		public override IList<IFieldMetaData> GetSystemFields( DatabaseTable dbTable )
		{
			List<IFieldMetaData> result = new List<IFieldMetaData>();
			if( dbTable == DatabaseTable.MainTable )
			{

			}
			else if( dbTable == DatabaseTable.ViewTable )
			{
				//CellID
				result.Add( new SystemIDFieldMetaData( this ) );

				SystemFieldMetaData dateCreateFld = new SystemFieldMetaData();
				dateCreateFld.Name = ShartMappingUtils.DateCreateName;
				dateCreateFld.DataType = DataType.DateTime;
				result.Add( dateCreateFld );
			}
			return result;
		}

		public CubeMetaData Clone()
		{
			CubeMetaData result = (CubeMetaData)base.CloneBase();
			return result;
		}

		#region ICloneable Members

		//object ICloneable.Clone()
		//{
		//    return Clone();
		//}

		#endregion
	}

 
   
}
