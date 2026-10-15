using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Ksema.Framework.Utils
{
	public static class NbrbReflectionUtils
	{
		public static Object GetPropValue(this Object obj, String name)
		{
			foreach (String part in name.Split('.'))
			{
				if (obj == null)
				{
					return null;
				}

				Type type = obj.GetType();
				PropertyInfo info = type.GetProperty(part);
				if (info == null)
				{
					return null;
				}

				obj = info.GetValue(obj, null);
			}

			return obj;
		}

		public static T GetPropValue<T>(this Object obj, String name)
		{
			Object retval = GetPropValue(obj, name);
			if (retval == null)
			{
				return default(T);
			}

			// throws InvalidCastException if types are incompatible
			return (T) retval;
		}

		public static Object GetFieldValue(this Object obj, String name)
		{
			foreach (String part in name.Split('.'))
			{
				if (obj == null)
				{
					return null;
				}

				Type type = obj.GetType();
				FieldInfo info = type.GetField(part);
				if (info == null)
				{
					return null;
				}

				obj = info.GetValue(obj);
			}

			return obj;
		}

		public static T GetFieldValue<T>(this Object obj, String name)
		{
			Object retval = GetFieldValue(obj, name);
			if (retval == null)
			{
				return default(T);
			}

			// throws InvalidCastException if types are incompatible
			return (T)retval;
		}

		public static void SetFieldValue<T>(this Object obj, String name, object value) //, BindingFlags bindingAttr
		{
			FieldInfo Field = obj.GetType().GetField(name);
			Field.SetValue(obj, value);
		}
	}
}
