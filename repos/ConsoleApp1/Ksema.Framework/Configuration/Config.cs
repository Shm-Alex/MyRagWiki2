using System;
using System.Xml;
using System.Collections;
using System.Configuration;
using Framework.Collections;
using Ksema.Framework.Utils;

namespace Framework.Configuration
{
	public class DictionaryListHandler : IConfigurationSectionHandler
	{
		private string _nodeName;
		public DictionaryListHandler()
		{
			_nodeName = "item";
		}

		public DictionaryListHandler(string nodeName)
		{
			_nodeName = nodeName;
		}


		public virtual object Create(object parent, object configContext, XmlNode section)
		{
			IList list = new List();
			foreach (XmlNode child in section)
			{
				if ((child.NodeType == XmlNodeType.Element) && (child.LocalName == NodeName))
				{
					IDictionary map = new Map();
					foreach (XmlAttribute attribute in child.Attributes)
						map.Add(attribute.LocalName, attribute.Value);
					string aVal = child.InnerText;
					if ((aVal != null) && (aVal.Trim() != String.Empty))
						map.Add("$text", aVal);
					list.Add(map);
				}
				else if (!ConfigurationUtils.SupportedType(child.NodeType))
				{
					throw new Exception("Invalid configuration file content: \r\n" + child.OuterXml);
				}
			}
			return list;
		}

		public virtual string NodeName
		{
			get
			{
				return _nodeName;
			}
		}

	}

	public class CollectionHandler : IConfigurationSectionHandler
	{
		private string _nodeName;
		private string _keyName;
		private string _valueName;
		public CollectionHandler()
		{
			_nodeName = "item";
			_keyName = "key";
			_valueName = "value";
		}

		public CollectionHandler(string nodeName, string keyName, string valueName)
		{
			_nodeName = nodeName;
			_keyName = keyName;
			_valueName = valueName;
		}


		private IList ParseList(string csv)
		{
			IList list = new OrderedList();
			if (csv != null)
			{
				string[] array = csv.Split(new char[1] {','});
				foreach (string aValue in array)
				{
					string name = aValue.Trim();
					if (!list.Contains(name)) 
						list.Add(name);
				}
			}
			return list;
		}

		public virtual object Create(object parent, object configContext, XmlNode section)
		{
			IDictionary map = new Map();
			foreach (XmlNode child in section)
			{
				if ((child.NodeType == XmlNodeType.Element) && (child.LocalName == NodeName))
				{
					string aKey = ((XmlElement)child).GetAttribute(KeyName);
					string aValue = ((XmlElement)child).GetAttribute(ValueName);
					IList list = ParseList(aValue);
					if (!map.Contains(aKey))
						map.Add(aKey, list);
					else
					{
						IList prevList = map[aKey] as IList;
						foreach(string name in list)
							if (!prevList.Contains(name))
								prevList.Add(name);
					}
				}
				else if (!ConfigurationUtils.SupportedType(child.NodeType))
				{
					throw new Exception("Invalid configuration file content: \r\n" + child.OuterXml);
				}
			}
			return map;
		}

		public virtual string NodeName
		{
			get
			{
				return _nodeName;
			}
		}

		public virtual string KeyName
		{
			get
			{
				return _keyName;
			}
		}

		public virtual string ValueName
		{
			get
			{
				return _valueName;
			}
		}

	}

}
