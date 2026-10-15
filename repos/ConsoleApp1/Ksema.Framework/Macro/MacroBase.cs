using System;
using System.Collections.Specialized;
using System.Reflection;
using System.Collections;
using Framework.Collections;
using Framework.Utils;

namespace Framework.Macro
{
	public enum MacroType
	{
		library,
		single
	}
	public interface IMacroParser
	{
		object Parse(string function, object[] args);
	}
}