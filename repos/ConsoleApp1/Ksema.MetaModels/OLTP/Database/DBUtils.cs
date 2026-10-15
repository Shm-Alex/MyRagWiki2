using System;

namespace Ksema.Framework.Database
{
    public static class DBUtils
    {
        /// <summary>
        /// процедура преобразовывает значение из БД к инту.
        /// </summary>
        public static int ToInt(Object obj)
        {
            if (obj == null ||
                Equals(obj, DBNull.Value))
                return 0;

            return Convert.ToInt32(obj);
        }

        /// <summary>
        /// процедура преобразовывает значение из БД к bool.
        /// </summary>
        public static bool ToBool(Object obj)
        {
            if (obj == null ||
                Equals(obj, DBNull.Value))
                return false;

            return Convert.ToBoolean(obj);
        }

        public static string RemoveKeyValue(string key, string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;
            int key_index = str.IndexOf(key, 0, StringComparison.OrdinalIgnoreCase);
            if (key_index >= 0)
            {
                int term_index = str.IndexOf(';', key_index);
                if (term_index < key_index)
                    term_index = str.Length - 1;
                return str.Remove(key_index, term_index - key_index + 1);
            }
            else
                return str;
        }
		public static string GetKeyValue(string key, string str)
		{
			if (string.IsNullOrEmpty(str))
				return String.Empty;
			int key_index = str.IndexOf(key, 0, StringComparison.OrdinalIgnoreCase);
			if (key_index >= 0)
			{
				key_index = str.IndexOf('=', key_index);
				int term_index = str.IndexOf(';', key_index);
				if (term_index < key_index)
					term_index = str.Length - 1;
				return str.Substring(key_index + 1, term_index - key_index - 1);
			}
			else
				return String.Empty;
		}
    }
}
