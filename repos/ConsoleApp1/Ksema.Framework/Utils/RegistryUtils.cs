using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace Ksema.Framework.Utils
{
    public class RegistryUtils
    {
		public static object GetValue( string keyPath, string valueName, object defaultValue)
		{
			return GetValue(keyPath, valueName, defaultValue, RegistryView.Default);
		}

        public static object GetValue( string keyPath, string valueName, object defaultValue , RegistryView view)
        {
            object result = defaultValue;
        	var rootKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
            RegistryKey key = rootKey.OpenSubKey( keyPath );
            if( key != null )
            {
                result = key.GetValue( valueName, defaultValue );
            }
            return result;
        }
    }
}
