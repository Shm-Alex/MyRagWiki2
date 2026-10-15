using System;
using System.Collections.Generic;
using Ksema.ObjectModel;
using Microsoft.Practices.EnterpriseLibrary.Validation.Validators;
using Framework.Collections;

namespace Shart.ModelDef
{
	[Serializable]
	public class BaseMetaData : IBaseMetaDataDef
	{
        [CSAttribute("NameTemplate")]
        protected string _nameTemplate = string.Empty;

		[CSAttribute( "Name" )]
		protected string _name = string.Empty;

		[CSAttribute( "Description" )]
		private string _description = string.Empty;

		[CSAttribute( "Guid" )]
		private string _guid = string.Empty;

		[CSAttribute( "Version" )]
		private int _version = 1;


		[CSReference( "Properties", true, "Owner" )]
		private CSKeyMultiLink<BaseMetaData, ObjectProperty, string, ObjectPropertyKey> _properties;

		public BaseMetaData()
		{
			CSPropMgr.Instance.setupObject( this, false );
		}

        [NotNullValidator]
        [StringLengthValidator( 1, 64, MessageTemplate = "Длина строки должна быть от 1 до 64 символов" )]
        [RegexValidator( @"[0-9a-zA-Z\s\-\.']*" )]
		public virtual string Name
		{
			get { return _name; }
			set { _name = value; }
		}

        [StringLengthValidator(0, 255, MessageTemplate = "Длина строки должна быть от 0 до 255 символов")]
        [MetaDataPropertyAttribute]
        public string NameTemplate
        {
            get { return _nameTemplate; }
            set 
            { 
                _nameTemplate = value;				
                OnNameTemplateChanged();
            }
        }

        protected virtual void OnNameTemplateChanged()
        {
            
        }

        [StringLengthValidator( 0, 255, MessageTemplate = "Длина строки должна быть от 0 до 255 символов" )]
		public string Description
		{
			get { return _description; }
			set { _description = value; }
		}

        [NotNullValidator]
        [StringLengthValidator(36, 36)]
		public string GUID
		{
			get { return _guid; }
			set { _guid = value; }
		}

		[MetaDataPropertyAttribute]
		[RangeValidator( 1, RangeBoundaryType.Inclusive, int.MaxValue, RangeBoundaryType.Inclusive )]
		public int Version
		{
			get { return _version; }
			set { _version = value; }
		}

		public IDictionary<string, ObjectProperty> Properties
		{
			get
			{
				return _properties;
			}
		}

		public void AddProperty( ObjectProperty property )
		{
			_properties.addLinkedObject( property );
		}

		public void RemoveProperty( ObjectProperty property )
		{
			_properties.removeLinkedObject( property );
		}

		internal void UpdatePropertyKey( string oldSysName )
		{
			ObjectProperty obj;
			if( _properties.TryGetValue( oldSysName, out obj ) )
			{
				_properties.removeObjectByKey( oldSysName );
				string newKey = ObjectPropertyKey.GetKey( obj );
				if( !_properties.ContainsKey( newKey ) )
					_properties.addLinkedObject( obj );
				else
					throw new Exception( string.Format( "Property with the same key, already exists. Key: {0}", newKey ) );
			}
		}
	}

    [AttributeUsage( AttributeTargets.Property, AllowMultiple = false )]
    public class MetaDataPropertyAttribute: Attribute
    {
        private string _name;
        
        public MetaDataPropertyAttribute( )
        {
        }

        public MetaDataPropertyAttribute( string name )
        {
            _name = name;
        }

        public string Name
        {
            get { return _name; }
        }
    }


	[Serializable]
	public class BaseMetaDataKey: IIndexer<BaseMetaData, string>
	{
		#region IIndexer Members
		string IIndexer<BaseMetaData, string>.GetIndexKey( BaseMetaData obj )
		{
			return GetKey( obj );
		}

		public IEqualityComparer<string> GetKeyComparer()
		{
			return StringComparer.OrdinalIgnoreCase;
		}

		#endregion

		public static string GetKey( IBaseMetaDataDef obj )
		{
			if( obj == null )
				throw new ArgumentNullException( "obj" );
			return obj.Name;
		}
	}

}
