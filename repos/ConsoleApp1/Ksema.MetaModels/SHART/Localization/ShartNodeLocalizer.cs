using System;
using System.Collections;
using System.Globalization;
using Framework.Utils;

namespace Ksema.SHART
{
	public class ShartNodeLocalizer
	{
		IDictionary _registeredFields = Hashtable.Synchronized(new Hashtable());
		IDictionary _registeredLang = Hashtable.Synchronized(new Hashtable());

		public void SetLocalizableField(string dimName, string fieldName)
		{
			string key = createKey(dimName, fieldName);
			_registeredFields[key] = fieldName;
		}

		public void AddLanguagePostfix(string langName, string fieldPostfix)
		{
			if(langName == null)
				langName = String.Empty;
			if(fieldPostfix == null)
				fieldPostfix = String.Empty;
					
			_registeredLang[langName] = fieldPostfix;
		}

		public string GetLocalizedField(string dimName, string fieldName)
		{
            return GetLocalizedField(dimName, fieldName, CultureInfo.CurrentUICulture);
		}
        public string GetLocalizedField(string dimName, string fieldName, CultureInfo cInfo)
        {
            string postfix = "";
            string key = createKey(dimName, fieldName);

            if( _registeredFields.Contains(key) )
            {
                string langName = cInfo.Name;
                if( _registeredLang.Contains(langName) )
                    postfix = (string) _registeredLang[langName];
                else
                {
                    postfix = cInfo.ThreeLetterWindowsLanguageName;
                }
            }
            else
            {
                if( !Utils.isEmpty(dimName) )
                    return GetLocalizedField("", fieldName, cInfo);
            }

            return fieldName + postfix;
        }

		private static string createKey(string dimName, string fieldName)
		{
			if(Utils.isEmpty(fieldName))
				throw new ArgumentNullException("fieldName");

			return dimName + "." + fieldName;
		}

	}
}