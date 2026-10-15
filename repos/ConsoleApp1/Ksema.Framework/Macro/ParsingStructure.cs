using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using System.Text;
using System.Runtime.Remoting.Proxies;
using System.Runtime.Remoting;
using Framework.Collections;
using Ksema.Framework.Macro.ContextCache;
using Framework.Utils;

namespace Framework.Macro.Parsing
{
	interface IMacroProvider
	{
		IDictionary MacroLibrary{get;}
		IDictionary CollectionMacro{get;}
		object GetMacro(string alias);
	}

	interface IMacroItem
	{
		object Eval(IMacroProvider macroProvider);
		void Test(IMacroProvider macroProvider);
	}

	#region Interfaces Implementation

	interface IMacroItemImpl : IMacroItem
	{
		void EvalImpl(DataContainer result, IMacroProvider macroProvider);
		void TestImpl(IMacroProvider macroProvider);
	}

	interface ICompositeMacro : IMacroItemImpl
	{
		void Add(IMacroItemImpl item);
		void Add(IEnumerable items);
		void Remove(IMacroItemImpl item);
		bool isOnceMacro{get;}
		IMacroItemImpl FirstMacro{get;}
	}

	interface IConst : IMacroItemImpl
	{
		string DataString{get;set;}
	}

	interface IForeach : IMacroItemImpl
	{
		IAlias Collection{get;set;}
		IMacroItemImpl Delimiter{get;set;}
		string CollectionAlias{get;set;}
		IMacroItemImpl Command{get;set;}
	}

	interface IAlias : IMacroItemImpl
	{
		string Def{get;set;}
		IFunction Method{get;set;}
	}

	interface IFunction
	{
		string Name{get;set;}
		BindingFlags Flags{get;set;}
		IFunction Next{get;set;}
		object[] Arguments(IMacroProvider macroProvider);
		object Invoke(object source, IMacroProvider macroProvider);
		void Test(IMacroProvider macroProvider);
	}

	#endregion

	#region Implementation

	class DataContainer
	{
		StringBuilder _stringBuilder = null;
		object _returnObject = null;
		
		public object Value
		{
			get
			{
				return _stringBuilder == null ? _returnObject : _stringBuilder.ToString();
			}
		}

		public void Add(object item, bool isObject)
		{
			if(_stringBuilder != null)
				_stringBuilder.Append(item);
			else if(isObject && _returnObject == null)
                _returnObject = item;
            else
            {
				_stringBuilder = new StringBuilder();
				_stringBuilder.Append(_returnObject);
				_stringBuilder.Append(item);
			}
		}
	}

	class MacroProvider : IMacroProvider
	{
		IMacroManager _manager;
		IDictionary _macroLibrary;
		IDictionary _collectionMacro;

		public MacroProvider(IMacroManager manger, IDictionary macroLibrary)
		{
			_manager = manger;
			_macroLibrary = macroLibrary;
			_collectionMacro = null;
		}

		IMacroManager Manager
		{
			get
			{
				return _manager;
			}
		}

		#region IMacroProvider Members

		public IDictionary MacroLibrary
		{
			get
			{
				return _macroLibrary;
			}
		}

		public IDictionary CollectionMacro
		{
			get
			{
				if(_collectionMacro == null)
					_collectionMacro = new ListDictionary();
				return _collectionMacro;
			}
		}

		public object GetMacro(string alias)
		{
			if(_collectionMacro != null && CollectionMacro.Contains(alias))
				return CollectionMacro[alias];
			if(MacroLibrary != null && MacroLibrary.Contains(alias))
				return MacroLibrary[alias];
			return Manager.GetMacro(alias);
		}

		#endregion
	}

	class BaseMacroItem : IMacroItemImpl
	{
		#region IMacroItemImpl Members

		public virtual void EvalImpl(DataContainer result, IMacroProvider macroProvider)
		{
		}
		public virtual void TestImpl(IMacroProvider macroProvider)
		{
		}

		#endregion

		#region IMacroItem Members

		public virtual object Eval(IMacroProvider macroProvider)
		{
			DataContainer result = new DataContainer();
			this.EvalImpl(result,macroProvider);
			return result.Value;
		}

		public virtual void Test(IMacroProvider macroProvider)
		{
			this.TestImpl(macroProvider);
		}
		#endregion
	}

	class CompositeMacro : BaseMacroItem, ICompositeMacro
	{
		IList _items = new ArrayList();

		public CompositeMacro()
		{}

		public CompositeMacro(IMacroItemImpl item)
		{
			Add(item);
		}

		public CompositeMacro(IEnumerable items)
		{
			Add(items);
		}

		#region ICompositeMacro Members

		public void Add(IMacroItemImpl item)
		{
			_items.Add(item);
		}

		public void Add(IEnumerable items)
		{
			foreach(IMacroItemImpl item in items)
				_items.Add(item);
		}

		public void Remove(IMacroItemImpl item)
		{
			_items.Remove(item);
		}

		public bool isOnceMacro
		{
			get
			{
				return _items.Count == 0;
			}
		}

		public IMacroItemImpl FirstMacro
		{
			get
			{
				if(_items.Count !=0)
					return _items[0] as IMacroItemImpl;
				else
					return new Const("");
			}
		}

		#endregion

		#region IMacroItemImpl Members

		public override void EvalImpl(DataContainer result, IMacroProvider macroProvider)
		{
			foreach(IMacroItemImpl item in _items)
				item.EvalImpl(result, macroProvider);
		}

		public override void TestImpl(IMacroProvider macroProvider)
		{
			foreach (IMacroItemImpl item in _items)
				item.TestImpl(macroProvider);
		}

		#endregion
	}

	class Const : BaseMacroItem, IConst
	{
		string _dataString;

		public Const(string dataString)
		{
			_dataString = dataString;
		}

		#region IMacroItemImpl Members

		public override void EvalImpl(DataContainer result, IMacroProvider macroProvider)
		{
			result.Add(DataString, false);
		}

		public override void TestImpl(IMacroProvider macroProvider)
		{
		}

		#endregion

		#region IMacroItem Members

		public override object Eval(IMacroProvider macroProvider)
		{
			return DataString;
		}

		public override void Test(IMacroProvider macroProvider)
		{
		}

		#endregion

		#region IConst Members

		public string DataString
		{
			get
			{
				return _dataString;
			}
			set
			{
				_dataString = value;
			}
		}

		#endregion
	}

	class Foreach : BaseMacroItem, IForeach
	{
		IAlias _collection;
		IMacroItemImpl _delimiter;
		string _collectionAlias;
		IMacroItemImpl _command;

		public Foreach(IAlias collection, IMacroItemImpl delimiter, string collectionAlias, IMacroItemImpl command)
		{
			_collection = collection;
			_delimiter = delimiter;
			_collectionAlias = collectionAlias;
			_command = command;
		}

		public Foreach(IAlias collection, string collectionAlias, IMacroItemImpl command)
		{
			_collection = collection;
			_delimiter = null;
			_collectionAlias = collectionAlias;
			_command = command;
		}

		#region IMacroItemImpl Members

		IEnumerable GetCollection(IMacroProvider macroProvider)
		{
			if(Collection == null)
				return Framework.Utils.CUtils.EmptyList;
			IEnumerable collection = Collection.Eval(macroProvider) as IEnumerable;
            return (collection != null) ? collection : Framework.Utils.CUtils.EmptyList;
		}

		string GetDelimiter(IMacroProvider macroProvider)
		{
			if(Delimiter == null)
				return null;
			return Framework.Utils.Utils.toString(Delimiter.Eval(macroProvider));
		}

		void EvalWithDelimiter(DataContainer result, IMacroProvider macroProvider, string delimiter, IEnumerable collection)
		{
			bool NotFirstDrop = false;
			foreach(object item in collection)
			{
				if(NotFirstDrop)
					result.Add(delimiter, false);
				if(Command != null)
				{
					if(CollectionAlias != null)
						macroProvider.CollectionMacro[CollectionAlias] = item;
					Command.EvalImpl(result, macroProvider);
				}
				NotFirstDrop = true;
			}
		}

		void EvalWithoutDelimiter(DataContainer result, IMacroProvider macroProvider, IEnumerable collection)
		{
			IList list = new ArrayList();
			if(Command != null)
			{
				foreach(object item in collection)
				{
					if(CollectionAlias != null)
						macroProvider.CollectionMacro[CollectionAlias] = item;
					list.Add(Command.Eval(macroProvider));
				}
			}
			result.Add(list, true);
		}

		public override void EvalImpl(DataContainer result, IMacroProvider macroProvider)
		{
			IEnumerable collection = GetCollection(macroProvider);
			string delimiter = GetDelimiter(macroProvider);

			object oldObject = null;
			if(CollectionAlias != null)
				oldObject = macroProvider.CollectionMacro[CollectionAlias];

			if(delimiter == null)
				EvalWithoutDelimiter(result, macroProvider, collection);
			else
				EvalWithDelimiter(result, macroProvider, delimiter, collection);

			if(CollectionAlias != null)
				macroProvider.CollectionMacro[CollectionAlias] = oldObject;
		}

		public override void TestImpl(IMacroProvider macroProvider)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region IForeach Members

		public IAlias Collection
		{
			get
			{
				return _collection;
			}
			set
			{
				_collection = value;
			}
		}

		public IMacroItemImpl Delimiter
		{
			get
			{
				return _delimiter;
			}
			set
			{
				_delimiter = value;
			}
		}

		public string CollectionAlias
		{
			get
			{
				return _collectionAlias;
			}
			set
			{
				_collectionAlias = value;
			}
		}

		public IMacroItemImpl Command
		{
			get
			{
				return _command;
			}
			set
			{
				_command = value;
			}
		}

		#endregion
	}

	class Alias : BaseMacroItem, IAlias
	{
		string _def;
		IFunction _method;

		public Alias(string def, IFunction method)
		{
			Def = def;
			_method = method;
		}

		#region IMacroItemImpl Members

		IFunction InvokeIMacroParser(ref object source, IMacroProvider macroProvider)
		{
			if(source is IMacroParser)
			{
				source = ((IMacroParser)source).Parse(Method.Name, Method.Arguments(macroProvider));
				return Method.Next;
			}
			return Method;
		}

		public override void EvalImpl(DataContainer result, IMacroProvider macroProvider)
		{
			object source = macroProvider.GetMacro(Def);

			IFunction method = InvokeIMacroParser(ref source, macroProvider);
			while(method != null)
			{
				source = method.Invoke(source, macroProvider);
				method = method.Next;
			}

			result.Add(source, true);
		}

		public override void TestImpl(IMacroProvider macroProvider)
		{
			object source = macroProvider.GetMacro(Def);

			IFunction method = Method;
			while (method != null)
			{
				method.Test(macroProvider);
				method = method.Next;
			}
		}
		#endregion

		#region IAlias Members

		public string Def
		{
			get
			{
				return _def;
			}
			set
			{
				_def = value != null ? value.Trim() : value;
			}
		}

		public IFunction Method
		{
			get
			{
				return _method;
			}
			set
			{
				_method = value;
			}
		}

		#endregion
	}

	class Function : IFunction
	{
		string _name;
		BindingFlags _flags;
		IFunction _next;
		object[] _arguments;

		public Function(string name, BindingFlags flags, IFunction prev, object[] arguments)
		{
			Name = name;
			_flags = flags;
			_next = null;
			_arguments = arguments;

			if(prev != null)
				prev.Next = this;
		}

		#region IFunction Members

		public string Name
		{
			get
			{
				return _name;
			}
			set
			{
				_name = value != null ? value.Trim() : value;
			}
		}

		public BindingFlags Flags
		{
			get
			{
				return _flags;
			}
			set
			{
				_flags = value;
			}
		}

		public IFunction Next
		{
			get
			{
				return _next;
			}
			set
			{
				_next = value;
			}
		}

		public object[] Arguments(IMacroProvider macroProvider)
		{
			object[] result = new object[_arguments.Length];
			for(int i=0; i<_arguments.Length; i++)
				result[i] = _arguments[i] == null ? null : ((IMacroItem)_arguments[i]).Eval(macroProvider);
			return result;
		}

		Type[] GetTypeArray(object[] arguments)
		{
			Type[] types = new Type[arguments.Length];
			for(int i=0; i<types.Length; i++)
				types[i] = arguments[i] != null ? arguments[i].GetType() : typeof(object);
			return types;
		}

		public object Invoke(object source, IMacroProvider macroProvider)
		{
			if (source == null)
				return null;
			Type type = null;
			if (RemotingServices.IsTransparentProxy(source))
			{
				throw new NotSupportedException("Transparent Proxies are not supported");
			}
			else
				type = source.GetType();

			object[] arguments = Arguments(macroProvider);
			MethodInfo method = Framework.Utils.TypeUtils.GetInheritedMethodEx(type, Name, Flags, GetTypeArray(arguments));

			try
			{
				if (method == null)
				{
					if (source is Type)
						return (source as Type).InvokeMember(Name, Flags | BindingFlags.Static | BindingFlags.FlattenHierarchy, null, null, arguments);
					else
						return type.InvokeMember(Name, Flags, null, source, arguments);
				}
				else
					return method.Invoke(source, arguments);
			}
			catch (TargetInvocationException error)
			{
				if (error.InnerException != null)
					throw new CompositeException(error.InnerException);
				else
					throw;
			}
		}

		public void Test(IMacroProvider macroProvider)
		{
			for (int i = 0; i < _arguments.Length; i++)
				if (_arguments[i] != null)
					((IMacroItem)_arguments[i]).Test(macroProvider);
		}

		#endregion
	}

	#endregion
}
