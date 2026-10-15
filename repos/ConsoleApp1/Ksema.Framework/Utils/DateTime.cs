using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlTypes;

namespace Framework.Utils
{
	public static class DateTimeHelper
	{
		public static DateTime MinDate(this DateTime a, DateTime b)
		{
			return a < b ? a : b;
		}

		public static DateTime MaxDate(this DateTime a, DateTime b)
		{
			return a > b ? a : b;
		}

		public static DateTime ValueOrNow(this DateTime? date)
		{
			return date.HasValue ? date.Value : DateTime.Now;
		}

		public static DateTime ValueOrDefault(this DateTime? date, DateTime defaultValue)
		{
			return date.HasValue ? date.Value : defaultValue;
		}

		public static bool IsCorrectSqlDateTime(this DateTime date)
		{
			return SqlDateTime.MinValue.Value <= date && date <= SqlDateTime.MaxValue.Value;
		}

		public static string LifePeriod(DateTime bg, DateTime end)
		{
			return "[" + bg.ToShortDateString() + ";" + end.AddSeconds(-1).ToShortDateString() + "]";
		}

		public static string ShartFormatDate(this DateTime dt)
		{
			return dt.ToString("yyyy.MM.dd");
		}
	}
}
