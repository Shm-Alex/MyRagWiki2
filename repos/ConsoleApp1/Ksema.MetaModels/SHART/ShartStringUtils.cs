using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Ksema.Shart
{
	/// <summary>
	/// класс предназначен для различных преобразований строк
	/// </summary>
	public	class StringUtils
	{
		#region	Public Static Functions
			/// <summary>
			/// процедура преобразует текст в строку, видимую пользователю
			/// если нулл, то возваращает "",
			/// если DateTime, то возвращает строку в нужном формате
			/// </summary>
			static	public	string	ToString(Object obj)
			{
				if (obj == null)
					return "";

				if (obj is DateTime)
				{
					DateTime	date = (DateTime)obj;
					if (date == DateTime.MinValue)
						return "";

					return ((DateTime)obj).ToString("dd.MM.yyyy HH:mm", m_RussiaCulture);
				}

				return obj.ToString();
			}

			static	public	string	ToString(DateTime date, string format, int levelId )
			{
				if ( levelId == (int)TimePeriodType.Year )
				{
					if ( format.IndexOf( 'p' ) > -1 )
					{
						format = format.Replace( "ppppr", "" );
						format = format.Replace( "pppr", "" );
						format = format.Replace( "ppr", "" );
						format = format.Replace( "pr", "" );
						format = format.Replace( "p", "" );
					}
				}
				else if ( levelId == (int)TimePeriodType.HalfYear )
				{
					if ( format.IndexOf( 'p' ) > -1 )
					{
						format = format.Replace( "ppr", "bb" );
						format = format.Replace( 'p', 'b' );
					}
				}
				else if ( levelId == (int)TimePeriodType.Quarter )
				{
					format = format.Replace( 'p', 'q' );
				}
				else if ( levelId == (int)TimePeriodType.Month )
				{
					if ( format.IndexOf( 'p' ) > -1 )
					{
						format = format.Replace( "ppppr", "mmmm" );
						format = format.Replace( "pppr", "mmm" );
						format = format.Replace( "ppr", "mm" );
						format = format.Replace( "pr", "m" );
						format = format.Replace( "p", "m" );
					}
				}
				else if ( levelId == (int)TimePeriodType.Day )
				{
					if ( format.IndexOf( 'p' ) > -1 )
					{
						format = format.Replace( "r", "" );
						format = format.Replace( "pppp", "dd mmmm" );
						format = format.Replace( "ppp", "dd mmm" );
						format = format.Replace( "pp", "dd mm" );
						format = format.Replace( "p", "dd mm" );
					}
				}

				return ToString( date, format );
			}

			static	public	string	ToString(DateTime date, string format)
			{
				if (date == DateTime.MinValue)
					return "";

				// HACK: StringUtils::ToString(date, format). руками меняем буквы m на M для месяцев
				format = format.Replace('m', 'M');
				format = format.Replace('Y', 'y');
				format = format.Replace('D', 'd');

				// а вот для кварталов нам нужно самим парсить
				if (format.IndexOf( "qq" ) > -1)
				{
					int		month = date.Month;
					int		quarter = TimePeriodEx.GetQuarter(month);

					string	sQuarter = quarter.ToString();

					if (format.IndexOf("qqqqr") > -1)
					{
						sQuarter = m_ArabianToRoman[sQuarter] as string;
						format = format.Replace("qqqqr", sQuarter + " квартал");
					}
					else if (format.IndexOf("qqqq") > -1)
					{
						format = format.Replace("qqqq", sQuarter + " квартал");
					}
					else if (format.IndexOf("qqqr") > -1)
					{
						sQuarter = m_ArabianToRoman[sQuarter] as string;
						format = format.Replace("qqqr", sQuarter + " кв");
					}
					else if (format.IndexOf("qqq") > -1)
					{
						format = format.Replace("qqq", sQuarter + " кв");
					}
					else if (format.IndexOf("qqr") > -1)
					{
						sQuarter = m_ArabianToRoman[sQuarter] as string;
						format = format.Replace("qqr", sQuarter);
					}
					else if (format.IndexOf("qq") > -1)
					{
						format = format.Replace("qq", sQuarter);
					}
				}// для полугодия тоже отпарсим
				else if (format.IndexOf("bb") > -1)
				{
					int		month = date.Month;
					int		halfYear = TimePeriodEx.GetHalfYear(month);

					string	sHalfYear = halfYear.ToString();

					if (format.IndexOf("bbbb") > -1)
					{
						format = format.Replace("bbbb", sHalfYear + " полугодие");
					}
					else if (format.IndexOf("bbb") > -1)
					{
						format = format.Replace("bbb", sHalfYear + " пол");
					}
					else if (format.IndexOf("bb") > -1)
					{
						format = format.Replace("bb", sHalfYear);
					}
				}
				return date.ToString(format, m_RussiaCulture);
			}
		
			static	public	string	ToStringFullForm(Object obj)
			{
				if (obj == null)
					return "";

				if (obj is DateTime)
				{
					DateTime	date = (DateTime)obj;
					if (date == DateTime.MinValue)
						return "";

					return ((DateTime)obj).ToString(m_RussiaCulture);
				}

				return obj.ToString();
			}
			/// <summary>
			/// Процедура преобразует число в строку, видимую пользователю.
			/// При этом происходит разделение каждых трех порядков пробелами.
			/// </summary>
			static	public	string	GetNumberGroupSeparatedString( Object obj,
				int numberDecimalDigits )
			{
				Debug.Assert ( numberDecimalDigits > -1 );

				NumberFormatInfo info = (NumberFormatInfo)NumberFormatInfo.
					CurrentInfo.Clone();

				string result;

				if ( obj is double )
				{
					double d = (double)obj;

					info.NumberGroupSeparator = " ";
					info.NumberDecimalDigits = numberDecimalDigits;

					result = d.ToString( "N", info );

					result = result.TrimEnd( info.NumberDecimalSeparator[ 0 ] );

					return result;
				}
				else if ( obj is int )
				{
					int i = (int)obj;

					info.NumberGroupSeparator = " ";
					info.NumberDecimalDigits = 0;

					return i.ToString( "N", info );
				}
				else if ( obj == null )
				{
					return "";
				}
				else
				{
					Debug.Fail( "В процедуру нужно передавать число!!!" );
					return obj.ToString();
				}
			}
			/// <summary>
			/// возвращает фэлз, если строка равна нулл или ""
			/// </summary>
			static	public	bool	IsEmpty(string str)
			{
				if (str == null || str == "")
					return true;

				return false;
			}
			/// <summary>
			/// возвращает фэлз, если объект, в котором должен быть double, равен нулл или 0.0
			/// </summary>
			static	public	bool	IsZeroDouble(object obj)
			{
				if (obj == null)
					return true;

				double	testObj = (double)obj;

				if (Math.Abs(testObj) < 0.0000000000001)
					return true;

				return false;
			}
			/// <summary>
			/// если цифра оканчивается на .000,
			/// то данная процедура отсекает и нули и точечки
			/// </summary>
			static	private	string	TrimZeroAtEnd(string str)
			{
				// найдем точку в конце...
				// если она есть, то отсечем все нолики в конце
				// но не забудем про знаки экспоненты
				int	startIndex = str.IndexOfAny(m_ExponentSymbols);

				if (startIndex < 0)
					startIndex = 0;

				int	decimalIndex = str.IndexOfAny(m_AllDecimalSeparators, startIndex);
				if (decimalIndex < 0)
					return str;

				// убьем нолики
				str = str.TrimEnd(m_Zero);
				// отсечем ненужные нолики справа после точки
				str = str.TrimEnd(m_AllDecimalSeparators);

				return str;
			}
			/// <summary>
			/// конвертируем в double
			/// если кроме числа есть другие символы, то кидается исключение
			/// </summary>
			static	public	double	ToDouble(string str)
			{
				try
				{
					if (IsEmpty(str))
						return 0;

					str = str.Trim();
					str = str.Replace(m_NotRequiredDecimalSeparator, m_RequiredDecimalSeparator);
					if (str.IndexOfAny(m_ExponentSymbols) < 0)
						return double.Parse(str);

					str = TrimZeroAtEnd(str);

					return double.Parse(str,	System.Globalization.NumberStyles.AllowLeadingSign |
												System.Globalization.NumberStyles.AllowDecimalPoint |
												System.Globalization.NumberStyles.AllowExponent);
				}
				catch(Exception exc)
				{
					throw new FormatException("StringUtils::ToDouble. Error while parsing <" + str + ">.", exc);
				}
			}
			/// <summary>
			/// конвертируем в int
			/// если кроме числа есть другие символы, то кидается исключение
			/// </summary>
			static	public	int		ToInt(string str)
			{
				try
				{
					if (IsEmpty(str))
						return 0;

					// убьем пробельчики
					str = str.Trim();
					str = TrimZeroAtEnd(str);

					return int.Parse(str);
				}
				catch(Exception exc)
				{
					throw new FormatException("StringUtils::ToInt. Error while parsing <" + str + ">.", exc);
				}
			}
			/// <summary>
			/// Конвертирует строку в 1, 0, либо в -1
			/// </summary>
			static public int ToTripleInt( string str )
			{
				if (IsEmpty(str))
					return 0;

				str = str.Trim();

				if ( str.ToUpper() == "ДА" || str.ToUpper() == "YES" ||
					str.ToUpper() == "TRUE" )
				{
					return 1;
				}
				else if ( str.ToUpper() == "НЕТ" || str.ToUpper() == "NO" ||
					str.ToUpper() == "FALSE" )
				{
					return 0;
				}

				try
				{
					int i = ToInt( str );
					if ( i == 0 )
					{
						return 0;
					}
					else if ( i == 1 )
					{
						return 1;
					}
					else
					{
						return -1;
					}
				}
				catch
				{
					return -1;
				}
			}
			/// <summary>
			/// конвертируем в дату
			/// дата может быть в формате:
			///  - int (т.е. OLE Automation Date)
			///  - dd.mm.yy		(вместо '.' м.б {_-,\/})
			///  - dd.mm.yyyy
			///  месяц можно задавать буквами.
			/// </summary>
			static public DateTime ToDateTime( string str )
			{
				return ToDateTime( str, true );
			}
			/// <summary>
			/// конвертируем в дату
			/// дата может быть в формате:
			///  - int (т.е. OLE Automation Date)
			///  ( если, что параметр allowOleAutomationDate установлен в true ).
			///  - dd.mm.yy		(вместо '.' м.б {_-,\/})
			///  - dd.mm.yyyy
			///  месяц можно задавать буквами.
			/// </summary>
			/// <param name="str">Входная строка.</param>
			/// <param name="allowOleAutomationDate">Флаг, говорящий о
			/// том, может ли строка содержать дату в формате OLE
			/// Automation или нет.</param>
			static	public	DateTime	ToDateTime( string str,
				bool allowOleAutomationDate )
			{
				if (IsEmpty(str))
					return DateTime.MinValue;

				str = str.Trim().ToLower();

				// From Ole Automation date.
				if (IsInteger(str))
				{
					if ( allowOleAutomationDate )
					{
						return DateTime.FromOADate(ToInt(str));
					}
					else
					{
						throw new FormatException( "Cannot convert string <" + str + "> to date!" );
					}
				}

				str = str.Replace('_', '.');
				str = str.Replace('-', '.');
				str = str.Replace(',', '.');
				str = str.Replace('/', '.');
				str = str.Replace('\\', '.');

				// посмотрим, может быть, там в конце стоит буковка г или г.
				int		lastIndex = str.Length - 1;
				if (str[lastIndex] == 'г')
					str = str.Substring(0, lastIndex).Trim();
				else if (str[lastIndex-1] == 'г')
					str = str.Substring(0, lastIndex-1).Trim();

				// посмотрим, может, там месяц буквами указали
				foreach(DictionaryEntry entry in m_Months)
				{
					if ( str.IndexOf( entry.Key as string ) > -1 )
					{
						str = str.Replace( entry.Key as string, entry.Value as string );
						break;
					}
				}
				// ну и, наконец, парсим
				try
				{
					return DateTime.Parse(str, m_RussiaCulture);
				}
				catch(Exception exc)
				{
					//throw new FormatException("Cannot convert string <" + str + "> to date!", exc);
					throw new FormatException("Невозможно преобразовать дату <" + str + ">!", exc);
				}
			}
			/// <summary>
			/// процедура убирает из строки символы:
			/// _, -, ,, ., \, /, .., 
			/// </summary>
			static	public	string	RemoveExtraSymbols( string str )
			{
				str = str.Replace('_', '.');
				str = str.Replace('-', '.');
				str = str.Replace(',', '.');
				str = str.Replace('/', '.');
				str = str.Replace('\\', '.');

				// избавимся от двойных точек
				str = str.Replace("..", ".");

				// уберем все пробелы
				str = str.Replace(" ", "");
				return str;
			}
			/// <summary>
			/// конвертируем в дату строку, заданную в формате:
			/// - dd.qq.yy
			/// - qq.yy
			/// вместо '.' может быть {_-,\/}
			/// квартал можно задавать:
			/// - арабской цифрой
			/// - арабской цифрой + кв
			/// - арабской цифрой + кв.
			/// - арабской цифрой + квартал
			/// - римской цифрой	(I, II, III, IV)
			/// - римской цифрой + кв
			/// - римской цифрой + кв.
			/// - римской цифрой + квартал
			/// </summary>
			/// <param name="str"></param>
			/// <returns></returns>
			static	public	DateTime	ToDateTimeFromQuarter( string str )
			{
				if (IsEmpty(str))
					return DateTime.MinValue;

				str = str.Trim().ToLower();

				str = RemoveExtraSymbols( str );

				// уберем все квартал
				str = str.Replace("квартал", "");

				// уберем все кв
				str = str.Replace("кв", "");

				// уберем все quarter
				str = str.Replace("quarter", "");

				// уберем все q
				str = str.Replace("q", "");

				// выделим квартальную часть.
				// эта часть находится на второй позиции справа после точки
				int		indexFirstDot = str.IndexOf('.');

				if (indexFirstDot < 0)
					throw new FormatException("Cannot convert string <" + str + "> to date!");

				int		indexSecondDot = str.IndexOf('.', indexFirstDot+1);

				// если второй точки нет, значит дня нет.
				// Поменяем эти переменные местами, чтобы не путаться
				if (indexSecondDot < 0)
				{
					indexSecondDot = indexFirstDot;
					indexFirstDot = -1;
				}
				string	quarter = str.Substring(indexFirstDot + 1, indexSecondDot - indexFirstDot - 1);
				int		day, year, month;

				if (indexFirstDot < 0)
					day = 1;
				else
					day = int.Parse(str.Substring(0, indexFirstDot));

				year = int.Parse(str.Substring(indexSecondDot + 1));

				// заменим номер квартала на номер первого месяца в квартале
				foreach(DictionaryEntry entry in m_QuarterToMonth)
				{
					if ( quarter.IndexOf( entry.Key as string ) > -1 )
					{
						quarter = quarter.Replace( entry.Key as string, entry.Value as string );
						break;
					}
				}
				month = int.Parse(quarter);
				// а теперь сформируем новую дату
				return new DateTime(year, month, day);
			}
			/// <summary>
			/// конвертируем в дату строку, заданную в формате:
			/// - b.yy
			/// вместо '.' может быть {_-,\/}
			/// полугодие можно задавать арабской цифрой
			/// </summary>
			static	public	DateTime	ToDateTimeFromHalfYear( string str )
			{
				if (IsEmpty(str))
					return DateTime.MinValue;

				str = str.Trim().ToLower();

				str = RemoveExtraSymbols( str );

				// выделим полугодовую часть часть.
				// эта часть находится на первой позиции
				int		indexFirstDot = str.IndexOf('.');

				if (indexFirstDot < 0)
					throw new FormatException("Cannot convert string <" + str + "> to date!");

				string	halfYear = str.Substring(0, indexFirstDot);
				int		day, year, month;

				day = 1;
				month = TimePeriodEx.GetBegMonthByHalfYear(int.Parse(halfYear));
				year = int.Parse(str.Substring(indexFirstDot + 1));
				// а теперь сформируем новую дату
				return new DateTime(year, month, day);
			}
			/// <summary>
			/// ф-ция аналогична ToDateTime(string str) за исключением того,
			/// что здесь разрешены ошибки в дате.
			/// в случае ошибки возвращается DateTime.MinValue
			/// </summary>
			static	public	DateTime	ToDateTimeWOException(string str)
			{
				DateTime	retValue = DateTime.MinValue;
				try
				{
					retValue = ToDateTime(str);
				}
				catch{}
				return retValue;
			}
			/// <summary>
			/// ф-ция проверяет, входит ли символ ch в charSet
			/// </summary>
			private	static	bool	IsIn(IEnumerable<char> charSet, char ch)
			{
				foreach(char chTest in charSet)
				{
					if (chTest == ch)
						return true;
				}
				return false;
			}
			/// <summary>
			/// смотрим, является ли число интом
			/// </summary>
			static	public	bool	IsInteger(string str)
			{
				if (IsEmpty(str))
					return false;
				str = str.Trim();

				foreach(char ch in str)
				{
					if (!IsIn(m_AllNumbers, ch))
						return false;
				}
				return true;
			}
			/// <summary>
			/// Конвертирует строку в объект.
			/// Обычно используется для того, чтобы получать данные по xml
			/// описаниям (там обычно есть какое-то поле, например fieldType,
			/// которое равно "string", или "double", ... ).
			/// </summary>
			/// <param name="str">стока для конвертации.</param>
			/// <param name="type">строка, которая равна "string", "double",
			/// "int", "date", или пустая строка (в этом случае возвращается строка).
			/// Регистр НЕ учитывается.
			///  </param>
			/// <returns></returns>
			public static object ToObject( string str, string type )
			{
				if ( StringUtils.IsEmpty( type ) )
				{
					return str;
				}
				
				type = type.ToUpper();

				if ( type == Types.STRING.ToString() || StringUtils.IsEmpty( type ) )
				{
					return str;
				}
				else if ( type == Types.DOUBLE.ToString() )
				{
					return ToDouble( str );
				}
				else if ( type == Types.INT.ToString() )
				{
					return ToInt( str );
				}
				else if ( type == Types.TRIPLEINT.ToString() )
				{
					return ToTripleInt( str );
				}
				else if ( type == Types.DATE.ToString() )
				{
					return ToDateTime( str );
				}
                else if( type == Types.DATE_AS_DBL.ToString() )
                {
                    if( string.IsNullOrEmpty(str) ) return DateTime.MinValue;
                    return DateTime.FromOADate(toDouble(str));
                }
                else
				{
					throw new InvalidOperationException( "Unknown fieldType <" + type + ">" );
				}
			}
        static NumberFormatInfo s_nFormat;
        static double toDouble(string val)
        {
            if( s_nFormat == null )
            {
                CultureInfo cltInfo = new CultureInfo("ru-RU");
                s_nFormat = (NumberFormatInfo) cltInfo.NumberFormat.Clone();
                s_nFormat.NumberDecimalSeparator = ",";
            }
            return Convert.ToDouble(val.Replace(".", ","), s_nFormat);
        }

		#endregion	Public Static Functions

		//---------------------------

		#region	private Data
			/// <summary>
			/// разделитель, который мы должны убрать из double и на который мы должны заменить...
			/// </summary>
			private	static	char	m_RequiredDecimalSeparator;
			private	static	char	m_NotRequiredDecimalSeparator;
			private	static	System.IFormatProvider	m_RussiaCulture;

			private	static	char[]	m_ExponentSymbols;
			private	static	char[]	m_AllDecimalSeparators;
			private	static	char[]	m_Zero;
			private	static	char[]	m_AllNumbers;

	    /// <summary>
			/// в массиве расположены DictionaryEntry различных вариантов написания месяцев
			/// в качестве значения - номера месяцев в виде строки
			/// </summary>
			private	static	readonly	ArrayList	m_Months;

			/// <summary>
			/// в массиве расположены DictionaryEntry различных вариантов написания кварталов
			/// в качестве значения - номера месяцев в виде строки
			/// </summary>
			private	static	readonly	ArrayList	m_QuarterToMonth;
			/// <summary>
			/// в массиве расположены соответствие арабских цифр - римским от 1 до 9
			/// </summary>
			private	static	readonly	Hashtable	m_ArabianToRoman;

		#endregion	private Data

		//---------------------------

		#region Public Properties

		public static Hashtable ArabianToRoman
		{
			get
			{
				return m_ArabianToRoman;
			}
		}

		#endregion

		//---------------------------

		#region	Initialisation
			static	StringUtils()
			{
				string	str = System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator;

				m_RequiredDecimalSeparator = str[0];
				m_NotRequiredDecimalSeparator = m_RequiredDecimalSeparator == '.' ? ',' : '.';

				m_RussiaCulture = new System.Globalization.CultureInfo("ru-RU");

				m_ExponentSymbols = new char[]{'E', 'e'};
				m_Zero = new char[]{'0'};
				m_AllDecimalSeparators = new char[]{'.', ','};
				m_AllNumbers = new char[]{'0', '1', '2', '3', '4', '5', '6', '7', '8', '9'};

			    m_Months = new ArrayList();
				m_Months.Add( new DictionaryEntry("январь", "01") );
				m_Months.Add( new DictionaryEntry("февраль", "02") );
				m_Months.Add( new DictionaryEntry("март", "03") );
				m_Months.Add( new DictionaryEntry("апрель", "04") );
				m_Months.Add( new DictionaryEntry("май", "05") );
				m_Months.Add( new DictionaryEntry("июнь", "06") );
				m_Months.Add( new DictionaryEntry("июль", "07") );
				m_Months.Add( new DictionaryEntry("август", "08") );
				m_Months.Add( new DictionaryEntry("сентябрь", "09") );
				m_Months.Add( new DictionaryEntry("октябрь", "10") );
				m_Months.Add( new DictionaryEntry("ноябрь", "11") );
				m_Months.Add( new DictionaryEntry("декабрь", "12") );
				m_Months.Add( new DictionaryEntry("january", "01") );
				m_Months.Add( new DictionaryEntry("february", "02") );
				m_Months.Add( new DictionaryEntry("march", "03") );
				m_Months.Add( new DictionaryEntry("april", "04") );
				m_Months.Add( new DictionaryEntry("may", "05") );
				m_Months.Add( new DictionaryEntry("june", "06") );
				m_Months.Add( new DictionaryEntry("july", "07") );
				m_Months.Add( new DictionaryEntry("august", "08") );
				m_Months.Add( new DictionaryEntry("september", "09") );
				m_Months.Add( new DictionaryEntry("october", "10") );
				m_Months.Add( new DictionaryEntry("november", "11") );
				m_Months.Add( new DictionaryEntry("december", "12") );

				m_QuarterToMonth = new ArrayList();
				// очень важен порядок!!!!
				// т.к. мы сможем заменить все "I" на "01"
				m_QuarterToMonth.Add( new DictionaryEntry("4", "10") );
				m_QuarterToMonth.Add( new DictionaryEntry("iv", "10") );

				m_QuarterToMonth.Add( new DictionaryEntry("3", "7") );
				m_QuarterToMonth.Add( new DictionaryEntry("iii", "7") );

				m_QuarterToMonth.Add( new DictionaryEntry("2", "4") );
				m_QuarterToMonth.Add( new DictionaryEntry("ii", "4") );

				m_QuarterToMonth.Add( new DictionaryEntry("1", "1") );
				m_QuarterToMonth.Add( new DictionaryEntry("i", "1") );

				m_ArabianToRoman = new Hashtable(10);
				m_ArabianToRoman.Add("1", "I");
				m_ArabianToRoman.Add("2", "II");
				m_ArabianToRoman.Add("3", "III");
				m_ArabianToRoman.Add("4", "IV");
				m_ArabianToRoman.Add("5", "V");
				m_ArabianToRoman.Add("6", "VI");
				m_ArabianToRoman.Add("7", "VII");
				m_ArabianToRoman.Add("8", "VIII");
				m_ArabianToRoman.Add("9", "IX");
			}
		#endregion	Initialisation

		//---------------------------

		#region Enums

		public enum Types
		{
			STRING,
			DATE,
			DOUBLE,
			INT,
			TRIPLEINT,
		    DATE_AS_DBL
		}

		#endregion
	}
}











