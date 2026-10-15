using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Ksema.Framework;
using Ksema.Framework.Documentation;

namespace Framework.Macro
{
	public interface IStandardMacroMethods
	{
		bool IsHolidayDay(object date);
	}

	[Documentation("Framework.StandardMacro.Doc")]
	public class StandardMacro
	{
		public static string MacroArrayDelemiter = "</value><value>";

		[Documentation("Framework.StandardMacro.Now")]
		public object Now
		{
			get { return System.DateTime.Now; }
		}


		[Documentation("Framework.StandardMacro.Not")]
		public object Not(object arg)
		{
			if (arg == null)
				return true;
			if (arg is string)
			{
				string val = ((string) arg).Trim().ToLower();
				switch (val)
				{
					case "true":
					case "yes":
						return false;
					case "false":
					case "no":
						return true;
					default:
						throw new NotSupportedException("not supported string for boolean: " + val);
				}
			}
			else
				return !(bool) arg;
		}

		[Documentation("Framework.StandardMacro.And")]
		public object And(object arg1, object arg2)
		{
			return (bool) arg1 && (bool) arg2;
		}

		[Documentation("Framework.StandardMacro.Or")]
		public object Or(object arg1, object arg2)
		{
			return (bool) arg1 || (bool) arg2;
		}

		[Documentation("Framework.StandardMacro.Xor")]
		public object Xor(object arg1, object arg2)
		{
			return (bool) arg1 ^ (bool) arg2;
		}

		[Documentation("Framework.StandardMacro.Null")]
		public object NULL
		{
			get { return null; }
		}

		[Documentation("Framework.StandardMacro.True")]
		public object True
		{
			get { return true; }
		}

		[Documentation("Framework.StandardMacro.False")]
		public object False
		{
			get { return false; }
		}

		[Documentation("Framework.StandardMacro.IsNull")]
		public object IsNull(object arg)
		{
			return arg == null;
		}

		[Documentation("Framework.StandardMacro.IsNullOrEmpty")]
		public object IsNullOrEmpty(object arg)
		{
			if (arg == null)
				return true;
			return String.IsNullOrEmpty(arg.ToString());
		}

		[Documentation("Framework.StandardMacro.IsNotNull")]
		public object IsNotNull(object arg)
		{
			return arg != null;
		}

		[Documentation("Framework.StandardMacro.IsNotNullOrEmpty")]
		public object IsNotNullOrEmpty(object arg)
		{
			if (arg == null)
				return false;
			return !String.IsNullOrEmpty(arg.ToString());
		}

		[Documentation("Framework.StandardMacro.If2")]
		public object If(object arg1, object arg2)
		{
			return If(arg1, arg2, true, false);
		}

		[Documentation("Framework.StandardMacro.IfNot")]
		public object IfNot(object arg1, object arg2)
		{
			return If(arg1, arg2, false, true);
		}

		[Documentation("Framework.StandardMacro.If3")]
		public object If(bool condition, object trueValue, object falseValue)
		{
			return condition ? trueValue : falseValue;
		}

		[Documentation("Framework.StandardMacro.If4")]
		public object If(object arg1, object arg2, object trueValue, object falseValue)
		{
			bool condition;
			if (arg1 == null)
				arg1 = String.Empty;
			if (arg2 == null)
				arg2 = String.Empty;
			condition = arg1.Equals(arg2);
			return condition ? trueValue : falseValue;
		}

		[Documentation("Framework.StandardMacro.IfExact2")]
		public object IfExact(object arg1, object arg2)
		{
			return IfExact(arg1, arg2, true, false);
		}

		[Documentation("Framework.StandardMacro.IfNotExact")]
		public object IfExactNot(object arg1, object arg2)
		{
			return IfExact(arg1, arg2, false, true);
		}

		[Documentation("Framework.StandardMacro.IfExact4")]
		public object IfExact(object arg1, object arg2, object trueValue, object falseValue)
		{
			bool condition;
			if (arg1 == null)
				condition = arg2 == null;
			else
				condition = arg1.Equals(arg2);
			return condition ? trueValue : falseValue;
		}

		[Documentation("Framework.StandardMacro.List")]
		public object List(object start, object stop)
		{
			int startValue = start is string ? Int32.Parse(start as string) : (int) start;
			int stopValue = stop== null ? 0 : ( stop is string ? Int32.Parse(stop as string) : (int) stop) ;
			int[] list = new int[stopValue - startValue + 1];
			int j = 0;
			for (int i = startValue; i <= stopValue; i++)
				list[j++] = i;
			return list;
		}

		[Documentation("Framework.StandardMacro.Array")]
		public object Array(params object[] values)
		{
			return values;
		}

		[Documentation("Framework.StandardMacro.ToBoFormat")]
		public string ToBoFormat(object value)
		{
			string sendDataFormats = ConfigurationManager.AppSettings["SendDataFormats"];
			var boargs = GetDateFormat(sendDataFormats);
			if (value is DateTime)
			{
				DateTime dt = (DateTime) value;
				return string.Format(boargs, dt).Trim();
			}

			if (value is string)
			{
				DateTime dt = Convert.ToDateTime(value);
				return string.Format(boargs, dt).Trim();
			}

			return value.ToString();
		}

		private static string GetDateFormat(string sendDataFormats)
		{
			string boargs;
			if (string.IsNullOrEmpty(sendDataFormats))
				boargs = "{0:yyyy.MM.dd}";
			else if (sendDataFormats == "yyyy/MM/dd")
				boargs = "{0:yyyy}/{0:MM}/{0:dd}";
			else
				boargs = "{0:" + sendDataFormats + "}";
			return boargs;
		}

		[Documentation("Framework.StandardMacro.ToBoFormat")]
		public string ToBoFormat(object value, string format)
		{
			var boargs = GetDateFormat(format);
			if (value is DateTime)
			{
				DateTime dt = (DateTime) value;
				return string.Format(boargs, dt).Trim();
			}

			if (value is string)
			{
				DateTime dt = Convert.ToDateTime(value);
				return string.Format(boargs, dt).Trim();
			}

			return value.ToString();
		}

		public object CSV(string text)
		{
			return (text ?? String.Empty).Split(',');
		}

		[Documentation("Framework.StandardMacro.InList")]
		public object InList(object _value, params object[] valueList)
		{
			for (int i = 0; i < valueList.Length; i++)
			{
				if (Object.Equals(_value, valueList[i]))
					return true;
			}

			return false;
		}

		[Documentation("Framework.StandardMacro.NotInList")]
		public object NotInList(object _value, params object[] valueList)
		{
			for (int i = 0; i < valueList.Length; i++)
			{
				if (Object.Equals(_value, valueList[i]))
					return false;
			}

			return true;
		}

		[Documentation("Framework.StandardMacro.JoinList")]
		public IList JoinList(IList first, IList second)
		{
			ArrayList result = new ArrayList(first.Count + second.Count);
			result.AddRange(first);
			result.AddRange(second);
			return result;
		}
		[Documentation("Framework.StandardMacro.JoinList")]
		public IList JoinList(IList first, IList second, IList third)
		{
			ArrayList result = new ArrayList(first.Count + second.Count);
			result.AddRange(first);
			result.AddRange(second);
			result.AddRange(third);
			return result;
		}

		[Documentation("Framework.StandardMacro.Switch")]
		public object Switch(object _value, params object[] choice)
		{
			if (choice.Length % 2 != 0)
				throw new ArgumentException("Params count");
			for (int i = 0; i < choice.Length; i += 2)
				if (Object.Equals(_value, choice[i]))
					return choice[i + 1];
			return null;
		}

		[Documentation("Framework.StandardMacro.TypeOfClass")]
		public object TypeOf(string className)
		{
			return Type.GetType(className, true, true);
		}

		[Documentation("Framework.StandardMacro.TypeOf")]
		public object TypeOf(string className, string assemblyName)
		{
			return Type.GetType(className + ", " + assemblyName, true, true);
		}

		[Documentation("Framework.StandardMacro.NewClass")]
		public object New(string className)
		{
			return Activator.CreateInstance(TypeOf(className) as Type);
		}

		[Documentation("Framework.StandardMacro.New")]
		public object New(string className, string assemblyName)
		{
			return Activator.CreateInstance(TypeOf(className, assemblyName) as Type);
		}

		[Documentation("Framework.StandardMacro.New")]
		public object New(string className, string assemblyName, params object[] args)
		{
			return Activator.CreateInstance(TypeOf(className, assemblyName) as Type, args);
		}

		[Documentation("Framework.StandardMacro.StaticClass")]
		public object Static(string className, string methodName)
		{
			Type type = Type.GetType(className, true, true);
			return type.InvokeMember(methodName,
				BindingFlags.Static | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.GetField |
				BindingFlags.InvokeMethod, null, null, null);
		}

		[Documentation("Framework.StandardMacro.Static")]
		public object Static(string className, string assemblyName, string methodName)
		{
			Type type = Type.GetType(className + ", " + assemblyName, true, true);
			return type.InvokeMember(methodName,
				BindingFlags.Static | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.GetField |
				BindingFlags.InvokeMethod, null, null, null);
		}

		[Documentation("Framework.StandardMacro.Int")]
		public object Int(string _value)
		{
			return Int32.Parse(_value);
		}

		[Documentation("Framework.StandardMacro.Bool")]
		public object Bool(string _value)
		{
			return Boolean.Parse(_value);
		}

		[Documentation("Framework.StandardMacro.DateTime")]
		public object DateTime(string _value)
		{
			return System.DateTime.Parse(_value);
		}

		[Documentation("Framework.StandardMacro.Decimal")]
		public object Decimal(string _value)
		{
			return System.Decimal.Parse(_value);
		}

		[Documentation("Framework.StandardMacro.Double")]
		public object Double(string _value)
		{
			return System.Double.Parse(_value);
		}

		[Documentation("Framework.StandardMacro.GetEnum")]
		public object GetEnum(string _type, string _assembly, string _value)
		{

			/*
			int lastDot = _value.LastIndexOf('.');
			string elementName = _value.Substring(lastDot + 1);
			string className = _value.Substring(0, str.Length - elementName.Length - 1);
			Type type = Type.GetType(className, true, false);
			object obj = Enum.Parse(type, elementName);
			 //*/
			Type type = Type.GetType(_type + "," + _assembly);
			object obj = Enum.Parse(type, _value);
			return obj;
		}

		[Documentation("Framework.StandardMacro.Dbl")]
		public object Dbl(string _value)
		{
			if (string.IsNullOrEmpty(_value)) return null;
			return System.Double.Parse(_value.Replace('.', ','), numberFormat);
		}

		[Documentation("Framework.StandardMacro.Debug")]
		public object IsDebugMode
		{
			get
			{
#if DEBUG
				return true;
#else
                return false;
#endif
			}
		}

		NumberFormatInfo _numberFormat;

		NumberFormatInfo numberFormat
		{
			get
			{
				if (_numberFormat == null)
				{
					_numberFormat = (NumberFormatInfo) (new CultureInfo("ru-RU")).NumberFormat.Clone();
					_numberFormat.NumberDecimalSeparator = ",";
				}

				return _numberFormat;
			}
		}

		private char NextSymbol(string text, int start)
		{
			if (start + 1 < text.Length)
				return text[start + 1];
			return char.MinValue;
		}

		private int FindQuot(string text, int start)
		{
			while (start < text.Length)
			{
				char symbol = text[start];
				if (symbol == '"')
				{
					if (NextSymbol(text, start) == '"')
						start++;
					else
						break;
				}
				else if (symbol == '\\')
				{
					start++;
				}

				start++;
			}

			if (start >= text.Length)
				throw new ArgumentException("Closing quotation mark not found in string '" + text + "'");
			return start;
		}

		private int FindComma(string text, int start)
		{
			while (start < text.Length)
			{
				char symbol = text[start];
				if (symbol == ',')
					break;
				else if (symbol == '"')
					start = FindQuot(text, start + 1);
				start++;
			}

			return start;
		}

		private string Normalize(string text)
		{
			text = text.Trim().Trim('"');
			int i = 0;
			while (i < text.Length)
			{
				char symbol = text[i];
				if ((symbol == '\\') || (symbol == '"' && NextSymbol(text, i) == '"'))
				{
					text = text.Remove(i, 1);
				}

				i++;
			}

			return text;
		}

		[Documentation("Framework.StandardMacro.CommaSplit")]
		public IList<string> CommaSplit(string text)
		{
			List<string> split = new List<string>();
			int start = 0;
			while (start < text.Length)
			{
				int stop = FindComma(text, start);
				split.Add(Normalize(text.Substring(start, stop - start)));
				start = stop + 1;
			}

			return split;
		}
		[Documentation("Framework.StandardMacro.CommaSplitForWhere")]
		public string CommaSplitForWhere(string text)
		{
			return  string.Join(",", text.Split(',').ToList().Select(x => "'" + x + "'")) ;
		}

	    private string GetDateWithFormat(object value, string format)
	    {

	        DateTime date;
	        if (value == null)
	            date = System.DateTime.Now;
	        else if (value is string)
	            date = System.DateTime.Parse((string)value);
	        else if (value is System.DateTime)
	            date = (DateTime)value;
	        else
	            throw new Exception("Неизвестный тип параметра '" + value.ToString() + "' - " + value.GetType());
            return date.ToString(format);
        }

	    string GetDelim(string delim)
	    {
	        int n;
	        bool isNumeric = int.TryParse(delim, out n);
	        if (isNumeric && n > 0 && n < 255)
	        {
	            return "" + Convert.ToChar(n);
	        }

	        if ( delim.Length>2 && delim.StartsWith("\'") && delim.EndsWith("\'"))
	            delim = delim.Substring(1, delim.Length - 2);
	        return delim;
	    }
	    [Documentation("Framework.StandardMacro.GetTimeInterval")]
        public object GetTimeInterval( object value1, object value2 , string format, string delim)
	    {
	        return GetDateWithFormat(value1, format) + GetDelim(delim) + GetDateWithFormat(value2, format) ;
        }
	    [Documentation("Framework.StandardMacro.MonthLastDay")]
        public object MonthLastDay( object value  )
	    {
	        DateTime date;
	        if (value == null)
	            date = System.DateTime.Now;
	        else if (value is string)
	            date = System.DateTime.Parse((string)value);
	        else if (value is System.DateTime)
	            date = (DateTime)value;
	        else
	            throw new Exception("Неизвестный тип параметра '" + value.ToString() + "' - " + value.GetType());
	        return new System.DateTime(date.Year, date.Month, 1).AddDays(-1);
	    }
		public object Month(object value)
		{
			DateTime date;
			if (value == null)
				date = System.DateTime.Now;
			else if (value is string)
				date = System.DateTime.Parse((string) value);
			else if (value is System.DateTime)
				date = (DateTime) value;
			else
				throw new Exception("Неизвестный тип параметра '" + value.ToString() + "' - " + value.GetType());
			return new System.DateTime(date.Year, date.Month, 1);
		}

		public object Month(object value,int move)
		{
			DateTime date;
			if (value == null)
				date = System.DateTime.Now;
			else if (value is string)
				date = System.DateTime.Parse((string) value);
			else if (value is System.DateTime)
				date = (DateTime) value;
			else
				throw new Exception("Неизвестный тип параметра '" + value.ToString() + "' - " + value.GetType());
			return new System.DateTime(date.Year, date.Month, 1).AddMonths(move);
		}

		public object Year(object value)
		{
			DateTime date;
			if (value == null)
				date = System.DateTime.Now;
			else if (value is string)
				date = System.DateTime.Parse((string) value);
			else if (value is System.DateTime)
				date = (DateTime) value;
			else
				throw new Exception("Неизвестный тип параметра '" + value.ToString() + "' - " + value.GetType());
			return new System.DateTime(date.Year, 1, 1);
		}
		public object Year(object value, int move)
		{
			DateTime date;
			if (value == null)
				date = System.DateTime.Now;
			else if (value is string)
				date = System.DateTime.Parse((string) value);
			else if (value is System.DateTime)
				date = (DateTime) value;
			else
				throw new Exception("Неизвестный тип параметра '" + value.ToString() + "' - " + value.GetType());
			return new System.DateTime(date.Year, 1, 1).AddYears(move);
		}

		[Documentation("Framework.StandardMacro.IsFirstDayOfYear")]
		public bool IsFirstDayOfYear(object Dt)
		{
			DateTime dateTime = (DateTime) Dt;
			if (dateTime.Month == 1 && dateTime.Day == 1)
				return true;
			return false;
		}

		[Documentation("Framework.StandardMacro.IsFirstDayOfQuarter")]
		public bool IsFirstDayOfQuarter(object Dt)
		{
			DateTime dateTime = (DateTime) Dt;
			if ((dateTime.Month - 1) % 3 == 0 && dateTime.Day == 1)
				return true;
			return false;
		}

		[Documentation("Framework.StandardMacro.IsFirstDayOfMonth")]
		public bool IsFirstDayOfMonth(object Dt)
		{
			DateTime dateTime = (DateTime) Dt;
			if (dateTime.Day == 1)
				return true;
			return false;
		}

		[Documentation("Framework.StandardMacro.IsMonday")]
		public bool IsMonday(object Dt)
		{
			DateTime dateTime = (DateTime) Dt;
			if (dateTime.DayOfWeek == DayOfWeek.Monday)
				return true;
			return false;
		}

		[Documentation("Framework.StandardMacro.IsTuesday")]
		public bool IsTuesday(object Dt)
		{
			DateTime dateTime = (DateTime) Dt;
			if (dateTime.DayOfWeek == DayOfWeek.Tuesday)
				return true;
			return false;
		}

		[Documentation("Framework.StandardMacro.IsWednesday")]
		public bool IsWednesday(object Dt)
		{
			DateTime dateTime = (DateTime) Dt;
			if (dateTime.DayOfWeek == DayOfWeek.Wednesday)
				return true;
			return false;
		}

		[Documentation("Framework.StandardMacro.IsThursday")]
		public bool IsThursday(object Dt)
		{
			DateTime dateTime = (DateTime) Dt;
			if (dateTime.DayOfWeek == DayOfWeek.Thursday)
				return true;
			return false;
		}

		[Documentation("Framework.StandardMacro.IsFriday")]
		public bool IsFriday(object Dt)
		{
			DateTime dateTime = (DateTime) Dt;
			if (dateTime.DayOfWeek == DayOfWeek.Friday)
				return true;
			return false;
		}

		[Documentation("Framework.StandardMacro.IsSaturday")]
		public bool IsSaturday(object Dt)
		{
			DateTime dateTime = (DateTime) Dt;
			if (dateTime.DayOfWeek == DayOfWeek.Saturday)
				return true;
			return false;
		}

		[Documentation("Framework.StandardMacro.IsSunday")]
		public bool IsSunday(object Dt)
		{
			DateTime dateTime = (DateTime) Dt;
			if (dateTime.DayOfWeek == DayOfWeek.Sunday)
				return true;
			return false;
		}

		[Documentation("Framework.StandardMacro.IsLastDayOfMonth")]
		public bool IsLastDayOfMonth(object Dt)
		{
			DateTime dateTime = (DateTime) Dt;
			int month = dateTime.Month;
			int monthNextDay = dateTime.AddDays(1).Month;
			if (month != monthNextDay)
				return true;
			return false;
		}

		[Documentation("Framework.StandardMacro.AddDays")]
		public object AddDays(object Dt, int day)
		{
			DateTime dateTime = (DateTime) Dt;
			return dateTime.AddDays(day);
		}

		[Documentation("Framework.StandardMacro.FirstDayOfNextMonth")]
		public object FirstDayOfNextMonth(object Dt)
		{
			DateTime dateTime = (DateTime) Dt;
			dateTime = dateTime.AddMonths(1);
			dateTime = new DateTime(dateTime.Year, dateTime.Month, 1);
			return dateTime;
		}

		private DateTime GetDate(object Dt)
		{
			try
			{
				if (Dt is DateTime)
					return (DateTime) Dt;
				if (Dt is string )
					return Convert.ToDateTime((string)Dt);
			}
			catch
			{
				;
			}

			return System.DateTime.Now;
		}
		private int GetInt(object Int)
		{
			try
			{
				if (Int is int)
					return (int) Int;
				if (Int is string)
					return Convert.ToInt32(Int);
			}
			catch
			{
				;
			}

			return -1;
		}

		[Documentation("Framework.StandardMacro.NextWorkDay")]
		public object NextWorkDay(object Dt, int day)
		{
			return DoNextWorkDay(Dt, day) ;
		}
		public static DateTime DoNextWorkDay (object Dt, int day)
		{
			DateTime dateTime = (DateTime)Dt;
			if (day < 0)
			{
				dateTime = dateTime.AddDays(-1);
				//Прибавить 3 года - что бы при кривых настройках не ушли в бесконечный цикл
				for (; dateTime <= System.DateTime.Now.AddYears(3); dateTime = dateTime.AddDays(-1))
				{
					if (IsWorkDay(dateTime))
					{
						day++;
						if (day >= 0)
							return dateTime;
					}
				}
			}
			else
			{
				//dateTime = dateTime.AddDays(1);
				//Прибавить 3 года - что бы при кривых настройках не ушли в бесконечный цикл
				for (; dateTime <= System.DateTime.Now.AddYears(3); dateTime = dateTime.AddDays(1))
				{
					if (IsWorkDay(dateTime))
					{
						day--;
						if (day < 0)
							return dateTime;
					}
				}
			}

			return dateTime;
		}
		public static bool IsWorkDay (object date)
		{
			IStandardMacroMethods inter = ObjectContainer.Instance.Resolve<IStandardMacroMethods>(); 
			return !inter.IsHolidayDay(date);
		}
		[Documentation("Framework.StandardMacro.ConvertForBO4Date")]
		public string ConvertForBO4Date(object Dt,object step)
		{
			DateTime dateTime = GetDate(Dt);
			if (step is string && ((string)step)!= "0")
			{
				int stepVal = 0;
				try
				{
					stepVal = Convert.ToInt16(step);
				}
				catch (Exception e)
				{
					;
				}
				return dateTime.AddDays(stepVal).ToString("yyyy-MM-dd") + "T00:00:00.000Z";
			}
			return dateTime.ToString("yyyy-MM-dd")+ "T00:00:00.000Z";
		}
		[Documentation("Framework.StandardMacro.ConvertForBO4Date")]
		public string ConvertForBO4DateAndNextWorkDay(object Dt,object step)
		{
			DateTime dateTime = GetDate(Dt);
			if (step is string && ((string)step)!= "0")
			{
				int stepVal = 0;
				try
				{
					stepVal = Convert.ToInt16(step);
				}
				catch (Exception e)
				{
					;
				}
				return DoNextWorkDay(dateTime ,stepVal).ToString("yyyy-MM-dd") + "T00:00:00.000Z" ;
			}
			return dateTime.ToString("yyyy-MM-dd")+ "T00:00:00.000Z";
		}

		private int[] QrtMonths = new int[] { 1, 4, 7, 10 };

		[Documentation("Framework.StandardMacro.QuarterSD")]
		public DateTime QuarterSD(object Dt, object offset)
		{
			DateTime dateTime = GetDate(Dt);
			int diff = GetInt(offset);
			dateTime = dateTime.AddDays(-dateTime.Day + 1);

			while (true)
			{
				if (QrtMonths.Contains(dateTime.Month))
					break;
				dateTime = dateTime.AddMonths(-1);
			}

			var curoffset = diff;
			while(true)
			{
				if (curoffset == 0) break;
				if (curoffset > 0)
				{
					dateTime = dateTime.AddMonths(3);
					curoffset--;
				}
				else
				{
					dateTime = dateTime.AddMonths(-3);
					curoffset++;
				}
			}
			return dateTime;
		}

		string DoJoinArray(IEnumerable<object> en)
		{
			StringBuilder sb = new StringBuilder();
			bool fst = true;
			foreach (var e in en)
			{
				if ( !fst )
					sb.Append(MacroArrayDelemiter);
				sb.Append( e.ToString().Trim() );
				fst = false;
			}

			return sb.ToString();
		}


		[Documentation("Framework.StandardMacro.JoinArray")]
		public object JoinArray (object o1, object o2)
		{
			return DoJoinArray ( new [] {o1, o2}) ;
		}
		[Documentation("Framework.StandardMacro.JoinArray")]
		public object JoinArray (object o1, object o2, object o3)
		{
			return DoJoinArray ( new [] {o1, o2, o3} ) ;
		}
		[Documentation("Framework.StandardMacro.JoinArray")]
		public object JoinArray (object o1, object o2, object o3, object o4)
		{
			return DoJoinArray ( new [] {o1, o2,o3,o4} ) ;
		}
		[Documentation("Framework.StandardMacro.JoinArray")]
		public object JoinArray (object o1, object o2, object o3, object o4, object o5 )
		{
			return DoJoinArray ( new object[] {o1, o2, o3, o4, o5 } ) ;
		}
		[Documentation("Framework.StandardMacro.JoinArray")]
		public object JoinArray (object o1, object o2, object o3, object o4, object o5, object o6)
		{
			return DoJoinArray ( new [] {o1, o2, o3, o4, o5, o6}) ;
		}
		[Documentation("Framework.StandardMacro.JoinArray")]
		public object JoinArray (object o1, object o2, object o3, object o4, object o5, object o6, object o7)
		{
			return DoJoinArray ( new [] {o1, o2, o3, o4, o5, o6, o7 } ) ;
		}

		string DoJsonJoinArray(IEnumerable<object> en)
		{
			return $"{string.Join(",", en)}"; /*Разделитель строчных значений "," */
		}
		[Documentation("Framework.StandardMacro.JsonJoinArray")]
		public object JsonJoinArray(object o1, object o2)
		{
			return DoJsonJoinArray(new[] { o1, o2 });
		}
		[Documentation("Framework.StandardMacro.JsonJoinArray")]
		public object JsonJoinArray(object o1, object o2, object o3)
		{
			return DoJsonJoinArray(new[] { o1, o2, o3 });
		}
		[Documentation("Framework.StandardMacro.JsonJoinArray")]
		public object JsonJoinArray(object o1, object o2, object o3, object o4)
		{
			return DoJsonJoinArray(new[] { o1, o2, o3, o4 });
		}
		[Documentation("Framework.StandardMacro.JsonJoinArray")]
		public object JsonJoinArray(object o1, object o2, object o3, object o4, object o5)
		{
			return DoJsonJoinArray(new object[] { o1, o2, o3, o4, o5 });
		}
		[Documentation("Framework.StandardMacro.JsonJoinArray")]
		public object JsonJoinArray(object o1, object o2, object o3, object o4, object o5, object o6)
		{
			return DoJsonJoinArray(new[] { o1, o2, o3, o4, o5, o6 });
		}
		[Documentation("Framework.StandardMacro.JsonJoinArray")]
		public object JsonJoinArray(object o1, object o2, object o3, object o4, object o5, object o6, object o7)
		{
			return DoJsonJoinArray(new[] { o1, o2, o3, o4, o5, o6, o7 });
		}

		public DateTime GetNextWorkDay (object date)
		{
			return DoNextWorkDay(date,1);
		}

		public DateTime GetNextWorkDay (object date, int n)
		{
			return DoNextWorkDay(date, n);
		}

		public DateTime GetNextWorkDay (object date, string n)
		{
			return DoNextWorkDay((DateTime)date, Int32.Parse(n) );
		}

		public DateTime GetPrevWorkDay (object date)
		{
			return DoNextWorkDay ( (DateTime)date, -1);
		}

		public DateTime GetPrevWorkDay (object date, int n)
		{
			return DoNextWorkDay((DateTime) date, -n);
		}

		public DateTime GetPrevWorkDay (object date, string n)
		{
			return DoNextWorkDay((DateTime)date, -1);
		}
	}

	public static class DateTimeExt
	{
		public static bool IsFirstDayOfMonth(this DateTime dateTime)
		{
			if (dateTime.Day == 1)
				return true;
			return false;
		}
		public static bool IsFirstDayOfQuarter(this DateTime dateTime)
		{
			if ((dateTime.Month - 1) % 3 == 0 && dateTime.Day == 1)
				return true;
			return false;
		}
		public static bool IsFirstDayOfHalfYear(this DateTime dateTime)
		{
			if ( (dateTime.Month == 1 || dateTime.Month == 7) && dateTime.Day == 1)
				return true;
			return false;
		}
		public static bool IsFirstDayOfYear(this DateTime dateTime)
		{
			if (dateTime.Month == 1 && dateTime.Day == 1)
				return true;
			return false;
		}
	}

	public class InitMacroManager : IDisposable
	{
		IMacroManager _macroMgr;
		IDictionary _libTable;
		IDictionary _prevMacros = new ListDictionary();

		public static IDictionary createMacroTable(string alias, object macroLib)
		{
			if (Utils.Utils.isEmpty(alias))
				throw new ArgumentNullException("alias");
			if (macroLib == null)
				throw new ArgumentNullException("macroLib");

			ListDictionary table = new ListDictionary();
			table.Add(alias, macroLib);
			return table;
		}


		public InitMacroManager(IMacroManager macroMgr, string alias, object macroLib) : this(macroMgr, createMacroTable(alias, macroLib), true, false)
		{
		}

		public InitMacroManager(IMacroManager macroMgr, IDictionary libTable, bool overwriteExisting, bool throwWhenExists)
		{
			_macroMgr = macroMgr;
			_libTable = libTable;

			foreach (DictionaryEntry entry in _libTable)
			{
				string alias = (string)entry.Key;
				if (Utils.Utils.isEmpty(alias))
					throw new ArgumentNullException("alias (entry key)");
				object prevMacro = null;
				object newMacro = entry.Value;
				if (_macroMgr.ContainsMacro(alias))
				{
					if (throwWhenExists)
						throw new InvalidOperationException("macro with alias '" + alias + "' already registered");
					prevMacro = _macroMgr.GetMacro(alias);
					if (!overwriteExisting)
						newMacro = prevMacro;

				}
				_prevMacros[alias] = prevMacro;
				_macroMgr.SetMacro(alias, newMacro);
			}
		}

		public void Dispose()
		{
			foreach (DictionaryEntry entry in _libTable)
			{
				string alias = (string)entry.Key;
				_macroMgr.DropMacro(alias);
				object prevMacro = _prevMacros[alias];
				if (prevMacro != null)
					_macroMgr.SetMacro(alias, prevMacro);
			}
		}
	}
}
