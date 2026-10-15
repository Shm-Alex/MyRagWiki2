using System;
using System.Text.RegularExpressions;

namespace Framework.Utils
{
	public sealed class AssertUtils
	{
		private static Regex patternText = new Regex(@"^[ a-zA-Zà-ÿÀ-ß¸¨\.\-()\""\';:,+_0-9]*$", RegexOptions.Compiled);
		private static Regex patternTextStrong = new Regex("^[a-zA-Z_0-9]*$", RegexOptions.Compiled);
		private AssertUtils()
		{
		}
		public static bool ValidateText(string source)
		{
			return patternText.IsMatch(source);
		}
		public static bool ValidateTextStrong(string source)
		{
			return patternTextStrong.IsMatch(source);
		}

        public static void IsNotNull(object val, string name)
        {
            if (val == null)
                throw new ArgumentNullException(name);
        }

        public static void IsNotEmpty(string val, string name)
        {
            if (Utils.isEmpty(val))
                throw new ArgumentException("string must not be empty", name);
        }

        public static void IsEqual(object val, object other, string msg)
        {
            if (!Equals(val, other))
                throw new InvalidOperationException("Objects are not equal: '" + val + "', '" + other + "'" + Environment.NewLine + msg);
        }

	    public static void IsNotEqual(object val, object other, string name)
        {
            if (Object.Equals(val, other))
                throw new ArgumentException(string.Format("Incorrect argument value: {0}", val), name);
        }
//		public static bool ValidateNumber(string source)
//		{
//			return false;
//		}
//		public static bool ValidateDateTime(string source)
//		{
//			return false;
//		}
	}
}
