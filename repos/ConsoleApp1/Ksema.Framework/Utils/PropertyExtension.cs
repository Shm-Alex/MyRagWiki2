using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ksema.Framework.Utils
{
	public static class PropertyExtension
	{
		public static void SetPropertyValue(this object obj, string propName, object value)
		{
			obj.GetType().GetProperty(propName)?.SetValue(obj, value, null);
		}
	}
}
