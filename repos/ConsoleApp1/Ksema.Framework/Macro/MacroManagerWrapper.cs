using System;
using System.Collections;
using Framework.Collections;
using Framework.Utils;
using Ksema.Framework.Macro.ContextCache;

namespace Framework.Macro
{
    public class MacroManagerWrapper : IMacroManager
    {
        IMacroManager _mgr;
        IDictionary _macroLib = new Map(); 

        public MacroManagerWrapper(IMacroManager mgr, string alias, object macroLib)
        {
            _mgr = mgr;
            _macroLib.Add(alias, macroLib);
        }

        public MacroManagerWrapper(IMacroManager mgr)
        {
            _mgr = mgr;
        }

        public bool RegisterMacro(string alias, object macro)
        {
            if (!ContainsMacro(alias))
            {
                _macroLib[alias] = macro;
                return true;
            }
            else
                return false;
        }

        public void SetMacro(string alias, object macro)
        {
            _macroLib[alias] = macro;
        }

        public void DropMacro(string alias)
        {
            _macroLib.Remove(alias);
        }

        public object GetMacro(string alias)
        {
            object macro = _macroLib[alias];
            if (macro == null)
                macro = _mgr.GetMacro(alias);
            return macro;
        }

        public object GetMacro(string alias, out MacroType type)
        {
            object macro = _macroLib[alias];
            if (macro == null)
                return GetMacro(alias, out type);
            else
            {
                type = MacroType.library;
                return macro;
            }
        }

        public bool ContainsMacro(string alias)
        {
            if (_macroLib[alias] != null)
                return true;
            else
                return _mgr.ContainsMacro(alias);
        }

		public void CheckString(string s, params string[] macroLibrary)
		{
			_mgr.CheckString(s, macroLibrary);
		}

		public object ParseString(string s)
        {
            return _mgr.ParseString(s, _macroLib);
        }

        public object ParseString(string s, string alias, object library)
        {
            object prevMacro = _macroLib[alias];
            SetMacro(alias, library);
            try
            {
                return _mgr.ParseString(s, _macroLib);
            }
            finally
            {
                if (prevMacro != null)
                    SetMacro(alias, prevMacro);
                else
                    DropMacro(alias);
            }
        }

        public object ParseString(string s, object[] alias, object[] library)
        {
            if(alias == null || library == null)
                throw new ArgumentNullException("macro library");
            if(alias.Length != library.Length)
                throw new ArgumentException("alias & library lengths must match");

            Map newMacroLib = new Map();
            for (int i = 0; i < alias.Length; i++)
                newMacroLib[alias[i]] = library[i];
            return ParseString(s, newMacroLib);
        }

        public object ParseString(string s, System.Collections.IDictionary macroLibrary)
        {
            IDictionary newMacroLib = CUtils.Clone(macroLibrary);
            foreach (DictionaryEntry entry in this._macroLib)
                newMacroLib[entry.Key] = entry.Value;

            return _mgr.ParseString(s, newMacroLib);
        }

    	public string ConvertString(string s)
        {
            return _mgr.ConvertString(s);
        }

        public string Prefix
        {
            get
            {
                return _mgr.Prefix;
            }
            set
            {
                throw new NotImplementedException("Method is not implemented yet");
            }
        }
    }
}
