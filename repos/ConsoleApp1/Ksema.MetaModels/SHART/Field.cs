using System;
using System.Collections.Generic;
using Framework.Macro;
using Ksema.Framework;
using Ksema.ObjectModel;
using Ksema.Shart;
using Microsoft.Practices.EnterpriseLibrary.Validation.Validators;

namespace Shart.ModelDef
{
	[Serializable]
	public abstract class FieldMetaData : BaseMetaData, IFieldMetaData, IObjectURIProvider
	{
		[CSReference( "Owner", false, "Fields" )]
		private CSSingleLink<FieldMetaData, OLAPObjectMetaData> _owner = null;

		[CSReference( "Contexts", false, "AttachedObjects" )]
		private CSMultiLink<BaseMetaData, ContextMetaData> _contexts;

		[CSAttribute( "IsNull" )]
		private bool _isNull = true;

		public FieldMetaData()
		{
		    CSPropMgr.Instance.setupObject( this, false );
		}


        [NotNullValidator]
        [StringLengthValidator( 1, 64, MessageTemplate = "Длина строки должна быть от 1 до 64 символов" )]
        [RegexValidator( @"[0-9a-zA-Z\s\-\.']*" )]
        public override string Name
        {
            get { return _name; }
            set
            {
                if( Owner != null && _name != value )
                {
                    string newName = value;
                    IFieldMetaData olapObj = Owner.GetFieldByName( newName, false );
                    if( olapObj != null && !Object.ReferenceEquals( this, olapObj ) )
                    {
                        throw new Exception( string.Format( "Field with the same key, already exists. Key: {0}", newName ) );
                    }
                    string oldName = _name;
                    _name = newName;
                    Owner.UpdateFieldMetaDataKey( oldName );
                }
                else
                {
                    _name = value;
                }
            }
        }

		public IList<ContextMetaData> Contexts
		{
			get
			{
				return _contexts;
			}
		}

	    IOLAPObjectMetaData IFieldMetaData.Owner
		{
			get { return _owner.LinkedObject; }
		}

        public OLAPObjectMetaData Owner
        {
            get { return _owner.LinkedObject; }
            set { _owner.LinkedObject = value; }
        }

        public string RuntimeDataTypeName
        {
            get
            {
                return GetRuntimeDataType(this.DataType);
            }
        }

		public object DefaultValue
		{
			get
			{
				return FieldMetaData.GetDefaultValue( this.DataType );
			}
		}

        public static string GetRuntimeDataType(DataType type)
	    {
	        switch (type)
	        {
				case DataType.Text:
	            case DataType.String:
	                return typeof(string).FullName;
	            case DataType.Number:
	                return typeof(Double).FullName;
	            case DataType.Integer:
	                return typeof(int).FullName;
				case DataType.LongInteger:
					return typeof( Int64 ).FullName;
				case DataType.DateTime:
	                return typeof(DateTime).FullName;
                case DataType.Boolean:
                    return typeof(Boolean).FullName;
				case DataType.LongBinary:
				case DataType.Image:
					return typeof( byte[] ).FullName;
				default:
	                throw new NotSupportedException("not supported data type: " + type);
	        }
	    }

		public static object GetDefaultValue( DataType type )
		{
			switch( type )
			{
				case DataType.Text:
				case DataType.String:
					return "";
				case DataType.Number:
					return (Double)0;
				case DataType.Integer:
					return (int)0;
				case DataType.LongInteger:
					return (Int64)0;
				case DataType.DateTime:
					return (DateTime)DateTime.MinValue;
				case DataType.Boolean:
					return false;
				case DataType.LongBinary:
				case DataType.Image:
					return null;
				default:
					throw new NotSupportedException( "not supported data type: " + type );
			}
		}

	    public virtual DataType DataType
        {
            get { throw new NotSupportedException(); }
            set { }
        }
        public virtual int DataSize
        {
            get { throw new NotSupportedException(); }
            set { }
        }

		public bool IsComputed
		{
			get 
			{
				return Properties.ContainsKey("Formula");
			}
		}

		public string Formula
		{
			get 
			{
				ObjectProperty prop;
				if (Properties.TryGetValue("Formula", out prop))
					return prop.StringValue;
				return null;
			}
		}

		#region IObjectURIProvider Members

        public ObjectURI CreateURI()
        {
            ObjectURI result = new ObjectURI( string.Format( "fieldmetadata/{0}", GUID ) );
            return result;
        }
        #endregion

		[MetaDataPropertyAttribute]
		public virtual bool IsNull
		{
			get
			{
				return _isNull;
			}
			set
			{
				_isNull = value;
			}
		}
	}

	[Serializable]
	public class FieldMetaDataLink
	{
		[CSReference( "Link", false )]
		private CSSingleLink<FieldMetaDataLink, FieldMetaData> _link;

		public FieldMetaDataLink()
		{
			CSPropMgr.Instance.setupObject( this, false );
		}

		public FieldMetaData Link
		{
			get
			{
				return _link.LinkedObject;
			}
			set
			{
				_link.LinkedObject = value;
			}
		}
	}

}
