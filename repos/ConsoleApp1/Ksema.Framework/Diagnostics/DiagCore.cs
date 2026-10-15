using System;
using System.Collections;
using System.Diagnostics;
using System.Configuration;
using Framework.Collections;
using Framework.Configuration;

namespace Framework.Diagnostics
{
	public class DiagMessage : IEnumerable
	{
		static IDictionary types = null;
		string type;
		IList attList;
		IDictionary map = new Map();

		public DiagMessage(string aType)
		{
			SetTypeList(aType);
		}

		public DiagMessage(string aType, IList names, IList values)
		{
			if (names.Count != values.Count)
				throw new ArgumentException("Количество имен и значений параметров несоответствует");
			SetTypeList(aType);
			for (int i = 0; i < names.Count; i++)
				SetValue(names[i] as string, values[i] as string);
		}

		public DiagMessage(string aType, IDictionary values)
		{
			SetTypeList(aType);
			foreach (DictionaryEntry entry in values)
				SetValue(entry.Key as string, entry.Value as string);
		}

		void SetTypeList(string aType)
		{
			type = aType;
			if (types == null)
				types = ConfigurationSettings.GetConfig(@"Diagnostics/Types") as IDictionary;
			attList = (types == null) || (!types.Contains(aType)) ? null : types[aType] as IList;
			Validate(attList);
		}

		protected virtual void Validate(ICollection attList)
		{
		}


		public string Type
		{
			get
			{
				return type;
			}
		}

		public string GetValue(string aName)
		{
			return map.Contains(aName) ? map[aName] as string : null;
		}

		public void SetValue(string aName, string aValue)
		{
			if ((aName != null) && (aValue != null) && (attList != null) && attList.Contains(aName))
				map[aName] = aValue;
		}


		public IEnumerator GetEnumerator()
		{
			return map.GetEnumerator();
		}

		public string this[string name]
		{
			get
			{
				return GetValue(name);
			}
			set
			{
				SetValue(name, value);
			}
		}

		public ICollection Keys
		{
			get
			{
				return map.Keys;
			}
		}

		public ICollection Values
		{
			get
			{
				return map.Values;
			}
		}


		public override string ToString()
		{
			string result = type + ": ";
			foreach(DictionaryEntry entry in map)
				result += entry.Key + "=" + entry.Value +"; ";
			return result;
		}

	}

	public abstract class DiagStream : TraceListener
	{
		static IDictionary map = null;
		public DiagStream() : base()
		{
		}


		protected bool NeedWriteMessage(object message)
		{
			if(message == null)
				return false;

			if (map == null)
			{
				map = ConfigurationSettings.GetConfig(@"Diagnostics/TypeStreams") as IDictionary;
				if (map == null) return false;
			}

			if(! (message is DiagMessage))
				message = createDefaultMessage(message.ToString());

			string typeName = (message as DiagMessage).Type;

			bool contains = map.Contains(typeName) ? (map[typeName] as IList).Contains(this.Name) : false;

			return contains;
		}

		public override void Write(object message)
		{
			if (NeedWriteMessage(message))
				WriteMessage(message);
		}

		public override void WriteLine(object message)
		{
			if (NeedWriteMessage(message))
				WriteMessage(message);
		}

		public override void Write(string message)
		{
			//throw new Exception("Method not supported");
			DiagMessage msg = createDefaultMessage(message);
			this.Write(msg);
		}

		private DiagMessage createDefaultMessage(string message)
		{
			return new TimeTraceMessage(message);
		}

		public override void WriteLine(string message)
		{
			//throw new Exception("Method not supported");
			DiagMessage msg = createDefaultMessage(message);
			this.WriteLine(msg);
		}

		public abstract void WriteMessage(object message);
	}

	public class TypeStreamCollectionHandler : CollectionHandler
	{
		private const string nodeName = "TypeStream";
		private const string keyName = "type";
		private const string valueName = "streams";
		public override string NodeName
		{
			get
			{
				return nodeName;
			}
		}

		public override string KeyName
		{
			get
			{
				return keyName;
			}
		}

		public override string ValueName
		{
			get
			{
				return valueName;
			}
		}

	}

	public class TypeCollectionHandler : CollectionHandler
	{
		private const string nodeName = "Type";
		private const string keyName = "name";
		private const string valueName = "attributes";
		public override string NodeName
		{
			get
			{
				return nodeName;
			}
		}

		public override string KeyName
		{
			get
			{
				return keyName;
			}
		}

		public override string ValueName
		{
			get
			{
				return valueName;
			}
		}

	}

}
