using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Framework.Caching;
using Framework.Collections;
using Framework.Macro.Parsing;
using Framework.Utils;
using Ksema.Framework;
using Ksema.Framework.Documentation;
using Ksema.Framework.Macro.ContextCache;
using Ksema.Framework.Caching;

namespace Framework.Macro
{
	#region Interfaces

	public interface IMacroManager
	{
		bool RegisterMacro(string alias, object macro);
		void SetMacro(string alias, object macro);
		void DropMacro(string alias);
		object GetMacro(string alias);
		object GetMacro(string alias, out MacroType type);
		bool ContainsMacro(string alias);

		void CheckString(string s, params string[] macroLibrary);

		object ParseString(string s);
		object ParseString(string s, string alias, object library);
		object ParseString(string s, object[] alias, object[] library);
		object ParseString(string s, IDictionary macroLibrary);

		string ConvertString(string s);
		string Prefix{get;set;}
	}

	interface IMacroManagerImpl
	{
		IDictionary InternalMacroList{get;}
	}

	interface IParser
	{
		IMacroItem ParseMacro();
		IParser NewParser(string dataString);
	}

	#endregion

	#region Implementation

	[Documentation("Framework.Macro.Name", null, "Framework.Macro.Usage")]
	public class MacroManager : IMacroManager, IMacroManagerImpl, IDocumentationProvider, ISelfDocumentationProvider, ILinksDocumentationProvider
	{
		IDictionary _macroList = new Map();
		IDictionary _macroLibraryList = new Map();
		IDictionary _internalMacroList = new Map();
		string _macroPrefix = DefaultMacroPrefix;
		const string _parseTable = "_ParseTable_";
		public const string DefaultMacroPrefix = "&";

		public MacroManager()
		{
			Initialize();
		}

		public MacroManager(string prefix)
		{
			Prefix = prefix;
			Initialize();
		}

		void Initialize()
		{
			string[] defMacroList = new string[] {"apt", "quot", "tld", "mul", "pcnt", "dlr", "at", "shrp", "emk", "qmk", "amp", "sem", "com", "pt", "lbr", "rbr", "spc", "lt", "gt"};
			string[] defValueList = new string[] {"'", @"""", "~", "*", "%", "$", "@", "#", "!", "?", "&", ";", ",", ".", "(", ")", " ", "<", ">"};
			for(int i = 0; i < defMacroList.Length; i++)
				_internalMacroList.Add(defMacroList[i], defValueList[i]);
		}

		IDictionary ParseTable
		{
			get
			{
				IDictionary table = CacheMgr.AppCache[_parseTable] as IDictionary;
				if(table == null)
					lock(typeof(MacroManager))
						if((table = CacheMgr.AppCache[_parseTable] as IDictionary) == null)
						{
							table = Hashtable.Synchronized(new Hashtable());
							CacheMgr.AppCache[_parseTable] = table;
						}
				return table;
			}
		}

		#region IMacroManager Members

		public bool RegisterMacro(string alias, object macro)
		{
			try
			{
				if (ContainsMacro(alias))
					return false;
				if (macro is IMacroParser)
					_macroList.Add(alias, macro);
				else
					_macroLibraryList.Add(alias, macro);
				return true;
			}
			catch
			{
				return false;
			}
		}

		public void SetMacro(string alias, object macro)
		{
			if (ContainsMacro(alias)) 
				DropMacro(alias);
			RegisterMacro(alias, macro);
		}

		public void DropMacro(string alias)
		{
			_macroList.Remove(alias);
			_macroLibraryList.Remove(alias);
		}

		public object GetMacro(string alias)
		{
			MacroType type;
			return GetMacro(alias, out type);
		}

		public object GetMacro(string alias, out MacroType type)
		{
			if (_macroList.Contains(alias))
			{
				type = MacroType.single;
				return _macroList[alias];
			}
			else if (_macroLibraryList.Contains(alias))
			{
				type = MacroType.library;
				var val = _macroLibraryList[alias];
				return val is ILazyInitValue ? (val as ILazyInitValue).Value : val;
			}
			else
			{
				Type objectType = Type.GetType("Framework." + alias);
				if (objectType == null)
					throw new ArgumentException("Framework.Macro.NoClass".Localize(alias));
				object macro = objectType.GetConstructor(Type.EmptyTypes).Invoke(null);
				if (!RegisterMacro(alias, macro))
					throw new ArgumentException("Framework.Macro.Cantregister".Localize(alias));
				type = macro is IMacroParser ? MacroType.single : MacroType.library;
				return macro;
			}
		}

		public bool ContainsMacro(string alias)
		{
			return _macroList.Contains(alias) || _macroLibraryList.Contains(alias);
		}


		public string Prefix
		{
			get
			{
				return _macroPrefix;
			}
			set
			{
				if (value.IndexOfAny(new char[5] {'(', ')', ',', '.', ';'}) != -1)
					throw new ArgumentException("Framework.Macro.PrefixError".Localize(value));
				_macroPrefix = value;
			}
		}


		public void CheckString(string s, params string[] macroLibrary)
		{
			try
			{
				IMacroItem macro = ParseTable[s] as IMacroItem;
				if (macro == null)
					lock (s = String.Intern(s))
						if ((macro = ParseTable[s] as IMacroItem) == null)
						{
							IParser parser = new Parser(this, s);
							macro = parser.ParseMacro();
							ParseTable.Add(s, macro);
						}
				var dictionary = new Dictionary<string, object>();
				if (macroLibrary != null)
					macroLibrary.ForEach(i => dictionary[i] = null);
				macro.Test(new MacroProvider(this, dictionary));
			}
			catch (NotImplementedException ex)
			{
				CompositeException cEx = CreateCompositeException(s, ex);
				throw cEx;
			}
			catch (NotSupportedException ex)
			{
				CompositeException cEx = CreateCompositeException(s, ex);
				throw cEx;
			}
		}

		
		public object ParseString(string s)
		{
			return ParseString(s, null);
		}

		public object ParseString(string s, string alias, object library)
		{
			IDictionary macroLibrary = new Map();
			macroLibrary.Add(alias, library);
			return ParseString(s, macroLibrary);
		}

		public object ParseString(string s, object[] alias, object[] library)
		{
			if (alias.Length != library.Length)
				throw new ArgumentException("Framework.Macro.WrongParams".Localize());
			IDictionary macroLibrary = new Map();
			for (int i = 0; i < alias.Length; i++)
				macroLibrary.Add(alias[i], library[i]);
			return ParseString(s, macroLibrary);
		}

		public object ParseString(string s, IDictionary macroLibrary)
		{
			try
			{
				IMacroItem macro = ParseTable[s] as IMacroItem;
				if (macro == null)
				lock (s = String.Intern(s))
					if ((macro = ParseTable[s] as IMacroItem) == null)
					{
						IParser parser = new Parser(this, s);
						macro = parser.ParseMacro();
						ParseTable.Add(s, macro);
					}
			
				return macro.Eval(new MacroProvider(this, macroLibrary));
			}
			catch (NotImplementedException ex)
			{
				CompositeException cEx = CreateCompositeException(s, ex);
				throw cEx;
			}
			catch(NotSupportedException ex)
			{
				CompositeException cEx = CreateCompositeException(s, ex);
				throw cEx;
			}
		}

		private CompositeException CreateCompositeException(string s, Exception ex)
		{
			Exception mEx = new Exception("Framework.Macro.ParseError".Localize(s));
			return new CompositeException(mEx, ex);
		}

		public string ConvertString(string s)
		{
			// Replace all occurences of prefix and brackets
			string[] symbols = new String[4] {String.Empty, "(", ")", ","};
			string[] values = new String[4] {String.Empty, "&lbr;", "&rbr;", "&com;"};
			symbols[0] = _macroPrefix;
			values[0] = _macroPrefix + "const(" + _macroPrefix + ")";
			for (int i = 1; i < values.Length; i++)
				values[i] = values[i].Replace("&", _macroPrefix);
			string result = String.Empty;
			for (int i = 0; i < s.Length; i++)
			{
				bool isFound = false;
				for (int j = 0; j < symbols.Length; j++)
					if (s.Substring(i).StartsWith(symbols[j]))
					{
						result += values[j];
						i += symbols[j].Length - 1;
						isFound = true;
						break;
					}
				if (!isFound)
					result += s[i];
			}
			return result;
		}

		#endregion

		#region IMacroManagerImpl

		IDictionary IMacroManagerImpl.InternalMacroList
		{
			get
			{
				return _internalMacroList;
			}
		}

		#endregion

		#region Documentation

		DocumentationData IDocumentationProvider.GetObjectDocumentation(DocumentationManager manager, int depth)
		{
			var data = manager.DocumentSelf(this, typeof(MacroManager));

			{
				var category = new DocumentationData();
				category.ObjectName = "StandardMacro";
				category.SetDataFromAttribute(new DocumentationAttribute("Framework.Macro.Standard"));
				data.Categories.Add(category);
				foreach (DictionaryEntry macro in _internalMacroList)
				{
					var link = new DocumentationLinkData();
					link.LinkName = Prefix + macro.Key + ";";
					link.SetDataFromAttribute(new DocumentationAttribute("Framework.Macro.ReturnValue".Localize(macro.Value)));
					category.Links.Add(link);
				}
			}

			{
				var category = new DocumentationData();
				category.ObjectName = "UserMacro";
				category.SetDataFromAttribute(new DocumentationAttribute("Framework.Macro.User"));
				data.Categories.Add(category);

				manager.DocumentLinks(depth, this, typeof(MacroManager), category);
			}

			return data;
		}

		DocumentationBaseData ISelfDocumentationProvider.GetSelfDocumentation(DocumentationManager manager)
		{
			var attribute = DocumentationManager.GetAttribute(typeof(MacroManager));
			var data = new DocumentationBaseData(typeof(MacroManager));
			data.SetDataFromAttribute(attribute);
			data.Description = "Framework.Macro.Desc".Localize(Prefix);
			return data;
		}

		IDictionary<DocumentationLinkBaseData, object> ILinksDocumentationProvider.GetObjectsToDocument(DocumentationManager manager)
		{
			var links = new Dictionary<DocumentationLinkBaseData, object>();
			foreach (DictionaryEntry macro in _macroLibraryList)
				links.Add(new DocumentationLinkBaseData{LinkName = Prefix + macro.Key}, macro.Value);
			foreach (DictionaryEntry macro in _macroList)
				links.Add(new DocumentationLinkBaseData{LinkName = Prefix + macro.Key}, macro.Value);
			return links;
		}

		#endregion
	}

	public class MacroParser
	{
		public static string[] ParseArguments(string Argument)
		{
			// (c) by Master
			if (Argument == null)
				throw new ArgumentException("Framework.Macro.WrongParams".Localize());
			ArrayList array = new ArrayList();
			int iNest = 0, iCnt = 0, iB = -1, iE, iPar;
			bool bSPar;
			char c = ' ';
			for (int i = 0; i < Argument.Length; i++)
			{
				c = Argument[i];
				if (c == '(') iNest ++;
				if ((c == ')') && (iNest > 0)) iNest--;
				if ((iCnt == array.Count) && (iB == -1)) iB = i;
				if ((c == ',') && (iNest < 1)) iCnt++;
				if (((iCnt > array.Count) && (iB > -1)) || ((i == Argument.Length - 1) && (iB > -1)))
				{
					bSPar = false;
					while (((Argument[iB] == ' ') || ((Argument[iB] == '(') && !bSPar)) && (iB < i))
					{
						if (Argument[iB] == '(') bSPar = true;
						iB++;
					}
					if (c == ',')
						iE = i - 1;
					else
						iE = i;
					if (iNest >= 1)
						while ((Argument[iE] == ' ') && (iB <= iE)) iE--;
					else
					{
						iPar = 0;
						while ((iB <= iE) && ((Argument[iE] == ' ') || (((Argument[iE] == ')') && bSPar) && (iPar < 1))))
						{
							if (Argument[iE] == ')') iPar++;
							iE--;
						}
					}
					if (iB > iE)
						array.Add(null);
					else
						array.Add(Argument.Substring(iB, iE - iB + 1).Trim());
					iB = -1;
				}
			}
			if (c == ',') array.Add(null);
			string[] args = new String[array.Count];
			array.CopyTo(args);
			return args;
		}
	}
	class Parser : IParser
	{
		int _first;
		int _second;
		int _third;
		int _index;
		string _function;
		string _argument;
		bool _isDefaultFunction;
		bool _isEndAlias;
		string _macro;

		static char[] _markupSymbols = new char[]{'.', ';', '('};
		static BindingFlags _commonFlag = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
		static string _defaultFunction = "default";

		IMacroManager _manager;
		string _dataString;

		public Parser(IMacroManager manager, string dataString)
		{
			_manager = manager;
			_dataString = dataString;
		}

		#region Properties

		public int First
		{
			get
			{
				return _first;
			}
			set
			{
				_first = value;
			}
		}
		public int Second
		{
			get
			{
				return _second;
			}
			set
			{
				_second = value;
			}
		}
		public int Third
		{
			get
			{
				return _third;
			}
			set
			{
				_third = value;
			}
		}
		public int Index
		{
			get
			{
				return _index;
			}
			set
			{
				_index = value;
			}
		}

		public string Function
		{
			get
			{
				return _function;
			}
			set
			{
				_function = value;
			}
		}

		public string Argument
		{
			get
			{
				return _argument;
			}
			set
			{
				_argument = value;
			}
		}

		public bool IsDefaultFunction
		{
			get
			{
				return _isDefaultFunction;
			}
			set
			{
				_isDefaultFunction = value;
			}
		}

		public bool IsEndAlias
		{
			get
			{
				return _isEndAlias;
			}
			set
			{
				_isEndAlias = value;
			}
		}

		public string Macro
		{
			get
			{
				return _macro;
			}
			set
			{
				_macro = value;
			}
		}

		public IMacroManager Manager
		{
			get
			{
				return _manager;
			}
			set
			{
				_manager = value;
			}
		}

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

		#region Parse Implementation

		bool IsOnce
		{
			get
			{
				return (Index == 0) && (Third == DataString.Length - 1);
			}
		}

		bool IsExistDelegate
		{
			get
			{
				return DataString[Third] == '.' || (Third + 1 < DataString.Length && Third != Second && DataString[Third + 1] == '.');
			}
		}

		void GoNextMacro()
		{
			Index = DataString.IndexOf(Manager.Prefix, Third);
		}

		string ErrorMessage(int pos)
		{
			return "Framework.Macro.Message".Localize(DataString[pos], pos, DataString);
		}

		int FindClosingBracket(int index)
		{
			int counter = 1;
			while (counter > 0)
			{
				index++;
				if (index >= DataString.Length)
					throw new ArgumentException("Framework.Macro.NoBracket".Localize(DataString));
				if (DataString[index] == ')') counter--;
				if (DataString[index] == '(') counter++;
			}
			return index;
		}

		void BuildThird()
		{
			if (Second == -1)
				throw new ArgumentException(ErrorMessage(First) + Environment.NewLine + "Framework.Macro.NoDelimiter".Localize());
			Third = DataString[Second] == '(' ? FindClosingBracket(Second) : Second;
		}

		void BuildFunctionInfo(bool isDefaultFunction, bool isEndAlias)
		{
			IsDefaultFunction = isDefaultFunction;
			IsEndAlias = isEndAlias;
			_argument = (DataString[Second] == '(') ? DataString.Substring(Second + 1, Third - Second - 1) : String.Empty;
			_function = IsDefaultFunction ? _defaultFunction : DataString.Substring(First + 1, Second - First - 1);
		}
		
		void BuildMacroInfo()
		{
			First = DataString.IndexOfAny(_markupSymbols, Index + Manager.Prefix.Length);
			if (First == -1)
				throw new ArgumentException(ErrorMessage(Index) + Environment.NewLine + "Framework.Macro.NoDelimiter".Localize());
			Second = DataString[First] == '.' ? DataString.IndexOfAny(_markupSymbols, First + 1) : First;
			BuildThird();
            BuildFunctionInfo(DataString[First] != '.', DataString[First] == ';');
			Macro = DataString.Substring(Index + Manager.Prefix.Length, First - Index - Manager.Prefix.Length);
		}

		void BuildAliasInfo()
		{
			First = Third + (DataString[Third] == '.' ? 0 : 1);
			Second = DataString.IndexOfAny(_markupSymbols, First + 1);
			BuildThird();
			BuildFunctionInfo(false, false);
		}

		IConst ParseConst()
		{
			if (First != Second || Second == Third)
				throw new ArgumentException(ErrorMessage(First) + Environment.NewLine + "Framework.Macro.ConstSyntax".Localize());
			return new Const(Argument);
		}

		string[] ParseArguments()
		{
			return MacroParser.ParseArguments(Argument);
		}

		object[] ParseParams()
		{
			object[] inParams = ParseArguments();
			object[] outParams = new object[inParams.Length];
			for (int i = 0; i < inParams.Length; i++)
			{
				string s = inParams[i] as string;
				outParams[i] = s != null ? NewParser(s).ParseMacro() : inParams[i];
			}
			return outParams;
		}

		IForeach ParseForeach()
		{
			string[] args = ParseArguments();
			IAlias collection = NewParser((args[0])).ParseMacro() as IAlias;
			int i = args.Length == 3 ? 1 : 2;
			IMacroItemImpl delimiter = null;
			if(i==2)
				delimiter = NewParser(args[1]).ParseMacro() as IMacroItemImpl;
			string collectionAlias = args[i];
			IMacroItemImpl comand = NewParser(args[i+1]).ParseMacro() as IMacroItemImpl;
			return new Foreach(collection, delimiter, collectionAlias, comand);
		}

		IFunction GetFunction(IFunction prevMethod)
		{
			object[] arguments = ParseParams();
			BindingFlags methodFlag = Third == Second ? BindingFlags.GetField | BindingFlags.GetProperty : BindingFlags.InvokeMethod;
			return new Function(Function, methodFlag | _commonFlag, prevMethod, arguments);
		}

		IAlias ParseAlias()
		{
			IFunction method = null;
			if(!IsEndAlias)
				method = GetFunction(null);
			IAlias aliasMacro = new Alias(Macro, method);

			while (IsExistDelegate)
			{
				BuildAliasInfo();
				method = GetFunction(method);
			}
			return aliasMacro;
		}

		IConst ParseInternal()
		{
			if (First != Second || Second != Third)
				throw new ArgumentException(ErrorMessage(First) + Environment.NewLine + "Framework.Macro.StandardSyntax".Localize());
			return new Const(((IMacroManagerImpl)Manager).InternalMacroList[Macro] as string);
		}

		void AddConst(ICompositeMacro macro, bool isBeginConst)
		{
			string data;
			if(isBeginConst)
				data = Index < 0 ? DataString : DataString.Substring(0, Index);
			else
				data = Index >= 0 ? DataString.Substring(Third+ 1, Index - Third - 1) : DataString.Substring(Third + 1);
			if(!Utils.Utils.isEmpty(data))
				macro.Add(new Const(data));
		}

		bool IsInternalMacro
		{
			get
			{
				if(!(Manager is IMacroManagerImpl))
					return false;
				return ((IMacroManagerImpl)Manager).InternalMacroList.Contains(Macro);
			}
		}

		#endregion

		#region IParser Members

		public IMacroItem ParseMacro()
		{
			ICompositeMacro result = new CompositeMacro();
			Index = DataString.IndexOf(Manager.Prefix, 0);
			AddConst(result, true);
			while (Index >= 0)
			{
				BuildMacroInfo();
				IMacroItemImpl macro;

				if(Macro == "const")
					macro = ParseConst();
				else if(Macro == "foreach")
					macro = ParseForeach();
				else if(IsInternalMacro)
					macro = ParseInternal();
				else
					macro = ParseAlias();

				if(IsOnce)
					return macro;
				result.Add(macro);
				GoNextMacro();
				AddConst(result, false);
			}
			if(result.isOnceMacro)
				return result.FirstMacro;
			return result;
		}

		public IParser NewParser(string s)
		{
			return new Parser(Manager, s);
		}

		#endregion
	}

	#endregion
}