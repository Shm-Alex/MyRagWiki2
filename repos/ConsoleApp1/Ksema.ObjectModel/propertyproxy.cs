using System.Reflection;

namespace Ksema.ObjectModel
{
	public abstract class CSPropertyProxyBase : ICSPropertyInfo
	{
		object _owner;
		string _name;
		ITypeDef _typeDef;
		private bool _required;

		public CSPropertyProxyBase(object _owner, string _name, bool _required)
		{
			this._owner = _owner;
			this._name = _name;
			this._required = _required;
		}

		public object Owner
		{
			get { return _owner; }
		}

		public ITypeDef TypeDef
		{
			get { return _typeDef; }
			set { _typeDef = value; }
		}

		public string Name
		{
			get { return _name; }
		}

		public bool Required
		{
			get { return _required; }
			set { _required = value; }
		}
	}

    public class CSAttValuePropertyProxy : CSPropertyProxyBase, ICSValue
	{
		PropertyInfo _pi;

		public CSAttValuePropertyProxy(object _owner, PropertyInfoEntry entry)
            : base(_owner, entry.PropertyInfo.Name, entry.PropertyInfo.Required)
		{
			_pi = (PropertyInfo) entry.MemberInfo;
			TypeDef = new SystemTypeDef(_pi.PropertyType);
		}


        public void setValue(object val)
        {
            _pi.SetValue(Owner, val, null);
            
        }

        public object getValue()
        {
            return _pi.GetValue(Owner, null);
        }
	}

    public class CSAttValueFieldProxy : CSPropertyProxyBase, ICSValue
	{
		FieldInfo _field;

		public CSAttValueFieldProxy(object owner, PropertyInfoEntry entry)
            : base(owner, entry.PropertyInfo.Name, entry.PropertyInfo.Required)
		{
			_field = (FieldInfo) entry.MemberInfo;
			TypeDef = new SystemTypeDef(_field.FieldType);
		}


        public void setValue(object val)
        {
            _field.SetValue(Owner, val);            
        }

        public object getValue()
        {
            return _field.GetValue(Owner);
        }
	}

}