using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Web;
using Framework.Utils;

namespace Ksema.ObjectModel
{
	public class StringTypeConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			if(sourceType == typeof(string))
				return true;
			return base.CanConvertFrom (context, sourceType);
		}
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			if(destinationType == typeof(string))
				return true;
			return base.CanConvertTo (context, destinationType);
		}
		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if(value is string || value == null)
				return convertFromString((string)value);
			return base.ConvertFrom (context, culture, value);
		}
		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if(destinationType == typeof(string))
				return convertToString(value);
			return base.ConvertTo (context, culture, value, destinationType);
		}

		protected virtual object convertFromString(string value)
		{
			throw new NotImplementedException();
		}
		protected virtual string convertToString(object value)
		{
			return (value == null) ? String.Empty : value.ToString();
		}
	}

	/// <summary>
	/// convert ObjectInfo to/from string representation in form: oid!className!{displayName}
	/// </summary>
	public class ObjectInfoConverter : StringTypeConverter
	{
		public const char separator = '!';
		public static string ToShortString(IObjectInfo info)
		{
			if(info == null || info == ObjectInfo.Empty)
				return String.Empty;
			return HttpUtility.HtmlEncode(info.oid + separator + info.className);
		}
		public static string ToString(IObjectInfo info)
		{
			return ToShortString(info) + HttpUtility.HtmlEncode(separator + info.displayName);
		}
		internal static ObjectInfo _FromString(string src)
		{
			if(Utils.isEmpty(src))
				return ObjectInfo.Empty;

			src = HttpUtility.HtmlDecode(src);

			int pos = src.IndexOf(separator);
			if(pos < 0)
				throw new FormatException("incorrect ObjectInfo format: " + src);
			string oid = src.Substring(0, pos).Trim();
			int pos2 = src.IndexOf(separator, pos + 1);
			if(pos2 < 0)
				pos2 = src.Length;
			string className = src.Substring(pos + 1, pos2 - pos - 1).Trim();
			string display = String.Empty;
			if(src.Length > (pos2 + 1))
				display = src.Substring(pos2 + 1).Trim();
			return new ObjectInfo(oid, className, display);
		}

		protected override string convertToString(object value)
		{
			return ToString((ObjectInfo)value);
		}
		protected override object convertFromString(string value)
		{
			return _FromString(value);
		}
	}

	/// <summary>
	/// convert ObjectSelectInfo to/from string representation in form: canselect,[objectinfo]
	/// </summary>
	public class ObjectSelectInfoConverter : StringTypeConverter
	{
		internal static string _ToString(ObjectSelectInfo info)
		{
			return (info.CanSelect ? "1" : "0") + ObjectInfoConverter.separator + info.ObjectData.ToString();
		}
		internal static ObjectSelectInfo _FromString(string src)
		{
			if(Utils.isEmpty(src))
				return null;
			char cs = src[0];
			bool canSelect = (cs == '0') ? false : true;
			if(src[1] != ObjectInfoConverter.separator)
				throw new FormatException("incorrect ObjectSelectInfo format: " + src);
			string oiSrc = src.Substring(2).Trim();
			return new ObjectSelectInfo(ObjectInfo.Parse(oiSrc), canSelect);
		}

		protected override string convertToString(object value)
		{
			return _ToString((ObjectSelectInfo)value);
		}
		protected override object convertFromString(string value)
		{
			return _FromString(value);
		}
	}


	public class ObjectInfoListConverter
	{	
		const char InfoSeparator = ';';
		const char DNSeparator = '!';
		const char ItemSeparator = '{';

		//Проверки на то, что ObjectInfo "корректный" не осуществляется!
		public static string ToString(ICollection objectsData)
		{
			Hashtable stringTable = new Hashtable();
			string sClassName = "";
			StringBuilder sDataInfo = new StringBuilder();
			StringBuilder result = new StringBuilder();
			
			foreach(ObjectInfo _objectData in objectsData)
			{			
				if(_objectData.className != sClassName)
				{					
					sClassName = _objectData.className;
					if(!stringTable.Contains(sClassName))
					{
						sDataInfo = new StringBuilder();						
						sDataInfo.Append(sClassName).Append(InfoSeparator);
						stringTable.Add(sClassName, sDataInfo);
					}
					else 
						sDataInfo = stringTable[sClassName] as StringBuilder;					
				}

				//добавление
				sDataInfo.Append(_objectData.oid);
				if(Utils.isEmpty(_objectData.displayName))
					sDataInfo.Append(InfoSeparator);
				else
				{
					sDataInfo.Append(DNSeparator);
					sDataInfo.Append(_objectData.displayName.Length);
					sDataInfo.Append(DNSeparator);
					sDataInfo.Append(_objectData.displayName);
				}
			}

			foreach(StringBuilder s in stringTable.Values)
				result.Append(s.ToString()).Append(ItemSeparator);
			
			return result.Remove(result.Length - 1, 1).ToString();
		}

		public static ICollection Parse(string sDataInfo)
		{
			ArrayList objectsData = new ArrayList();
			string sClassName = "";
			string sOid = "";
			string sDisplayName = "";

			char symbol;
			
			int lengthDisplayName;
			int curentIndex=0;
			int index, index2;
						
			index = sDataInfo.IndexOf(InfoSeparator);
			if(index == -1) throw new FormatException("Неправильный формат строки");
			sClassName = sDataInfo.Substring(0,index);
			curentIndex = index + 1;
			
			while(curentIndex < sDataInfo.Length)
			{
				if(sDataInfo[curentIndex] == ItemSeparator)
				{
					curentIndex++;
					index = sDataInfo.IndexOf(InfoSeparator,curentIndex);
					sClassName = sDataInfo.Substring(curentIndex,index - curentIndex);
					curentIndex = index + 1;
				}

				index = sDataInfo.IndexOf(InfoSeparator,curentIndex);				
				index2 = sDataInfo.IndexOf(DNSeparator,curentIndex);				
				symbol = InfoSeparator;

				if(index == -1 || (index >= index2 && index2 != -1)) 
				{
					index=index2; 
					symbol = DNSeparator;					
				}

				if(index == -1) throw new FormatException("Неправильный формат строки");

				sOid = sDataInfo.Substring(curentIndex,index-curentIndex);
				curentIndex = index+1;

				switch(symbol)
				{
					case InfoSeparator :	
						objectsData.Add(new ObjectInfo(sOid , sClassName));						
						break;

					case DNSeparator :	
						index = sDataInfo.IndexOf(DNSeparator, curentIndex);

						if(index == -1) throw new FormatException("Неправильный формат строки");
						
						lengthDisplayName = int.Parse(sDataInfo.Substring(curentIndex,index-curentIndex));
						curentIndex = index+1;
						sDisplayName = sDataInfo.Substring(curentIndex, lengthDisplayName);
						objectsData.Add(new ObjectInfo(sOid, sClassName, sDisplayName));
						
						curentIndex += lengthDisplayName;						
						break;				
				}				
			}

			return objectsData;
		}	
	}
}