using System;
using System.Collections;
using System.Collections.Specialized;
using Framework.Utils;

namespace Framework.Macro
{
	public interface IMacroFunction
	{
		IMacroManager Parser{get;}
		object execute(object[] args);
		string DefaultName{get;}
	}

	public struct MacroValue
	{
		string _macroString;
		public MacroValue(string macro)
		{
			_macroString = macro;
		}

		public string MacroString
		{
			get { return _macroString; }
		}
	}

	public class MacroFunctionHost : IMacroParser
	{
		IDictionary _macroTable = new HybridDictionary(10);

		public IMacroFunction getFunction(string funcName)
		{
			IMacroFunction func = (IMacroFunction) _macroTable[funcName];
			if(func == null)
				throw new NotFoundException("macro function not registered: " + funcName);
			return func;
		}

		public void setFunction(IMacroFunction func)
		{
			setFunction(func, func.DefaultName);
		}
		public void setFunction(IMacroFunction func, string funcName)
		{
			if(func == null)
				throw new ArgumentNullException("function");
			if(Utils.Utils.isEmpty(funcName))
				throw new ArgumentNullException("function name");

			_macroTable[funcName] = func;
		}

		public object Parse(string function, object[] args)
		{
			return getFunction(function).execute(args);
		}
	}

	public abstract class MacroFunctionBase : IMacroFunction
	{
		IMacroManager _parser;

		public MacroFunctionBase(IMacroManager parser)
		{
			_parser = parser;
		}

		public IMacroManager Parser
		{
			get { return _parser; }
		}

		public abstract object execute(object[] args);
		public abstract string DefaultName { get; }
	}

}
