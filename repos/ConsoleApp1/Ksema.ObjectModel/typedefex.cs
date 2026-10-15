using System;
using System.ComponentModel;
using Framework.Utils;

namespace Ksema.ObjectModel
{

	public interface ITypeDef
	{
		object ToObject(string strValue);
		string ToString(object aValue);
		object ConvertFrom(object aValue);
		Type SystemType{get;}
		object createDefaultValue();
	}

    [Serializable]
	public class SystemTypeDef : ITypeDef
	{
		Type _type;
		string _typename;

		public SystemTypeDef(Type type)
		{
			if(type == null)
				throw new ArgumentNullException("type");
			_type = type;
			_typename = _type.AssemblyQualifiedName;
		}

		public SystemTypeDef(string typename)
		{
			if(Utils.isEmpty(typename))
				throw new ArgumentNullException("typename");
			_typename = typename;
		}

		public virtual Type SystemType
		{
			get
			{
				if(_type == null)
					_type = Type.GetType(_typename, true);
				return _type;
			}
		}

		public virtual object createDefaultValue()
		{
			if(SystemType.IsValueType)
			{
				if(SystemType == typeof(DateTime))
					return new DateTime(1900, 1, 1);
				else
					return Activator.CreateInstance(SystemType);
			}
			else
				return null;
		}

		public virtual object ToObject(string aValue)
		{
			if(SystemType == typeof(String))
				return aValue;
			if (SystemType == typeof(Byte[]))
				return Utils.parseBlob(aValue);
//			if (SystemType == typeof(Framework.Blob.IBlob))
//				return new Framework.Blob.Blob(Utils.parseBlob(aValue));
			if( Utils.isEmpty(aValue) )
				return createDefaultValue();
			if(SystemType == typeof(Decimal))
				return Utils.parseDecimal(aValue);
			if(SystemType == typeof(uint))
				if(aValue == "-1")
					return uint.MaxValue;
			return getConverter().ConvertFromString(aValue);
		}
		public virtual string ToString(object aValue)
		{
			if(SystemType == typeof(uint) && aValue != null)
				if(aValue.GetType() != typeof(string))
					if(Convert.ToUInt32(aValue) == UInt32.MaxValue)
						return "-1";
			if (SystemType == typeof(Byte[]))
				return Utils.formatBlob((byte[])aValue);
//			if (SystemType == typeof(Framework.Blob.IBlob))
//				return Utils.formatBlob(((Framework.Blob.IBlob)aValue).Value.Data);
			if(aValue != null)
			{
				Type vType = aValue.GetType();
				if(vType == typeof(DateTime))
					return Utils.dateToString((DateTime)aValue);
				if(vType == typeof(Decimal))
					return Utils.decimalToString((Decimal)aValue);
			}

			return getConverter().ConvertToString(aValue);
		}
		public virtual object ConvertFrom(object aValue)
		{
			if(aValue != null)
			{
				if(this.SystemType == typeof(Object))
					return aValue;
				if(aValue.GetType() == this.SystemType)
					return aValue;
				if(aValue == DBNull.Value)
					aValue = null;
				if (SystemType.IsEnum)
				{
					if (aValue is string)
						return Enum.Parse(SystemType, (string)aValue, true);
					else
						return Enum.ToObject(SystemType, Convert.ToInt32(aValue));
				}
			}
			if(aValue == null)
				return createDefaultValue();
			return getConverter().ConvertFrom(aValue);
		}
		protected virtual TypeConverter getConverter()
		{
			return TypeDescriptor.GetConverter(SystemType);
		}
	}
}