using System;
using System.Collections;
using System.ComponentModel;
using System.Xml.Serialization;
using Framework.Utils;

namespace Ksema.ObjectModel
{
	public interface IObjectInfo
	{
		string oid{get;}
		string className{get;}
		string displayName{get;}
		string infoString{get;}
		ISimpleObject CreateObject();
	}

	[TypeConverter(typeof(ObjectInfoConverter))]
	[Serializable]
	public class ObjectInfo : IComparable, IObjectInfo
	{
		[XmlIgnore]
		public static ObjectInfo Empty = new ObjectInfo();

		string _className;
		string _oid;
		string _displayName;

		[XmlAttribute]
		public string oid
		{
			get{return _oid;}
			set
			{
				if(Utils.isEmpty(value))
					throw new ArgumentNullException("oid");
				_oid = value;
			}
		}

		[XmlAttribute]
		public string className
		{
			get{return _className;}
			set
			{
				if(Utils.isEmpty(value))
					throw new ArgumentNullException("className");
				_className = value;
			}
		}

		[XmlAttribute]
		public string displayName
		{
			get{return _displayName;}
			set{_displayName = value;}
		}

		[XmlIgnore]
		public string infoString
		{
			get { return this.ToString(); }
		}

		public ISimpleObject CreateObject()
		{
			Type t = Type.GetType(_className, true, true);
			ISimpleObject obj = (ISimpleObject) Activator.CreateInstance(t, new object[]{_oid});
			return obj;
		}

		private ObjectInfo()
		{
			_className = String.Empty;
			_oid = String.Empty;
			_displayName = "<нет>";
		}

		void setInfo(string oid, string className)
		{
			this.className = className;
			this.oid = oid;
		}

		void setInfo(ISimpleObject obj)
		{
			setInfo(obj.Key.ToString(), obj.GetType().AssemblyQualifiedName);
		}

		public ObjectInfo(string oid, string className)
		{
			setInfo(oid, className);
		}
		public ObjectInfo(string oid, string className, string displayName)
		{
			setInfo(oid, className);
			this.displayName = displayName;
		}
		public ObjectInfo(ISimpleObject obj)
		{
			if(obj == null)
				throw new ArgumentNullException("Object");
			setInfo(obj);
		}
		public ObjectInfo(ISimpleObject obj, string displayName)
		{
			if(obj == null)
				throw new ArgumentNullException("Object");
			setInfo(obj);
			this.displayName = displayName;
		}

		public override string ToString()
		{
			return ObjectInfoConverter.ToShortString(this);
		}
		public string ToLongString()
		{
			return ObjectInfoConverter.ToString(this);
		}
		public static ObjectInfo Parse(string s)
		{
			return ObjectInfoConverter._FromString(s);
		}

		#region IComparable Members

		public int CompareTo(object obj)
		{
			if( obj == null )
				return 1;
			ObjectInfo info = (ObjectInfo)obj;
			if(info.oid == this.oid)
				return 0;
			int res = Comparer.Default.Compare(this.className, info.className);
			if(res == 0)
				res = Comparer.Default.Compare(this.displayName, info.displayName);
			return res;
		}

		#endregion
	}


	[TypeConverter(typeof(ObjectSelectInfoConverter))]
	public class ObjectSelectInfo
	{
		bool _canSelect = false;
		ObjectInfo _info;

		public ObjectSelectInfo(ObjectInfo info, bool canSelect)
		{
			if(info == null)
				throw new ArgumentNullException("ObjectInfo");
			_info = info;
			_canSelect = canSelect;
		}
		public ObjectSelectInfo(ISimpleObject obj, bool canSelect)
		{
			if(obj == null)
				throw new ArgumentNullException("object");
			_info = new ObjectInfo(obj);
			_canSelect = canSelect;
		}

		public ObjectInfo ObjectData
		{
			get
			{
				return _info;
			}
		}

		public bool CanSelect
		{
			get
			{
				return _canSelect;
			}
		}
		public override string ToString()
		{
			return ObjectSelectInfoConverter._ToString(this);
		}

		public static ObjectSelectInfo Parse(string src)
		{
			return ObjectSelectInfoConverter._FromString(src);
		}
	}


}