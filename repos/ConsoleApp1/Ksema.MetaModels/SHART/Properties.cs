using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Framework.Collections;
using Framework.Macro;
using Framework.Utils;
using Ksema.Framework;
using Ksema.Framework.Caching;
using Ksema.MetaModels.SHART.Shart2OLTP;
using Ksema.ObjectModel;
using Microsoft.Practices.EnterpriseLibrary.Validation;
using Microsoft.Practices.EnterpriseLibrary.Validation.Validators;

namespace Shart.ModelDef
{
	[Serializable]
	public class ObjectProperty
	{
		[CSAttribute( "Name" )]
		private string _name;

		[CSReference( "Owner", false, "Properties" )]
		private CSSingleLink<ObjectProperty, BaseMetaData> _owner = null;


		public ObjectProperty()
		{
			CSPropMgr.Instance.setupObject( this, false );
		}

		[NotNullValidator]
		[StringLengthValidator( 1, 2000, MessageTemplate = "Длина строки должна быть от 1 до 2000 символов" )]
		[RegexValidator( @"[0-9a-zA-Z\s\-\.']*" )]
		public string Name
		{
			get
			{
				return _name;
			}
			set
			{
				if( Owner != null && _name != value )
				{
					string newName = value;
					ObjectProperty obj;
					Owner.Properties.TryGetValue( newName, out obj );
					if( obj != null && !Object.ReferenceEquals( this, obj ) )
					{
						throw new Exception( string.Format( "Property with the same key, already exists. Key: {0}", newName ) );
					}
					string oldName = _name;
					_name = newName;
					Owner.UpdatePropertyKey( oldName );
				}
				else
				{
					_name = value;
				}
			}
		}

		public virtual object Value
		{
			get;
			set;
		}

		public string StringValue
		{
			get
			{
				var val = Value;
				if( val != null )
					return val.ToString();
				return null;
			}
		}

		public BaseMetaData Owner
		{
			get
			{
				return _owner.LinkedObject;
			}
			set
			{
				_owner.LinkedObject = value;
			}
		}

		public virtual Type ValueType
		{
			get
			{
				return Value.GetType();
			}
		}

		public ObjectProperty Clone()
		{
			var result = (ObjectProperty)MemberwiseClone();
			CSPropMgr.Instance.setupObject(result, true);
			return result;
		}
	}

	[Serializable]
	public class ObjectPropertyKey: IIndexer<ObjectProperty, string>
	{
		#region IIndexer Members

		string IIndexer<ObjectProperty, string>.GetIndexKey( ObjectProperty o )
		{
			return GetKey( o );
		}

		public IEqualityComparer<string> GetKeyComparer()
		{
			return StringComparer.OrdinalIgnoreCase;
		}

		#endregion

		public static string GetKey( ObjectProperty o )
		{
			if( o == null )
				throw new ArgumentNullException( "o" );
			return o.Name;
		}
	}

	[Serializable]
	public class IntProperty : ObjectProperty
	{
		public IntProperty()
			: base()
		{
		}

		public IntProperty( string name, int value )
			: base()
		{
			Name = name;
			TypedValue = value;
		}

		public override object Value
		{
			get
			{
				return TypedValue;
			}
			set
			{
				TypedValue = Convert.ToInt32( value );
			}
		}

		[CSAttribute( "Value" )]
		public int TypedValue
		{
			get;
			set;
		}
	}

	[Serializable]
	public class StringProperty : ObjectProperty
	{
		public StringProperty()
			: base()
		{
		}

		public StringProperty( string name, string value )
			: base()
		{
			Name = name;
			TypedValue = value;
		}

		public override object Value
		{
			get
			{
				return TypedValue;
			}
			set
			{
				TypedValue = Convert.ToString( value );
			}
		}

		[CSAttribute( "Value" )]
		public string TypedValue
		{
			get;
			set;
		}

		public override Type ValueType
		{
			get
			{
				return typeof( string );
			}
		}
	}

	[Serializable]
	public class DoubleProperty : ObjectProperty
	{
		public DoubleProperty()
			: base()
		{
		}

		public DoubleProperty( string name, double value )
			: base()
		{
			Name = name;
			TypedValue = value;
		}

		public override object Value
		{
			get
			{
				return TypedValue;
			}
			set
			{
				TypedValue = Convert.ToDouble( value );
			}
		}

		[CSAttribute( "Value" )]
		public double TypedValue
		{
			get;
			set;
		}
	}

	[Serializable]
	public class BooleanProperty : ObjectProperty
	{
		public BooleanProperty()
			: base()
		{
		}

		public BooleanProperty( string name, bool value )
			: base()
		{
			Name = name;
			TypedValue = value;
		}

		public override object Value
		{
			get
			{
				return TypedValue;
			}
			set
			{
				TypedValue = Convert.ToBoolean( value );
			}
		}

		[CSAttribute( "Value" )]
		public bool TypedValue
		{
			get;
			set;
		}
	}

	[Serializable]
	public class DateTimeProperty : ObjectProperty
	{
		public DateTimeProperty()
			: base()
		{
		}

		public DateTimeProperty( string name, DateTime value )
			: base()
		{
			Name = name;
			TypedValue = value;
		}

		public override object Value
		{
			get
			{
				return TypedValue;
			}
			set
			{
				TypedValue = Convert.ToDateTime( value );
			}
		}

		[CSAttribute( "Value" )]
		public DateTime TypedValue
		{
			get;
			set;
		}
	}

	[Serializable]
	public class DecimalProperty : ObjectProperty
	{
		public DecimalProperty()
			: base()
		{
		}

		public DecimalProperty( string name, Decimal value )
			: base()
		{
			Name = name;
			TypedValue = value;
		}

		public override object Value
		{
			get
			{
				return TypedValue;
			}
			set
			{
				TypedValue = Convert.ToDecimal( value );
			}
		}

		[CSAttribute( "Value" )]
		public Decimal TypedValue
		{
			get;
			set;
		}
	}
}
