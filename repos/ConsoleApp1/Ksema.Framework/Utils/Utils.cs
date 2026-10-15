using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using Ksema.Framework.Diagnostics;
using Ksema.Framework.Tasks;
using Ksema.Framework.Utils;
using System.Net;
using Ksema.Framework;
using Ksema.Framework.Serializers;
using Shart.Service.Shared.Templates;

namespace Framework.Utils
{
	public static class Helper
	{
		public static void Times(this int count, Action action)
		{
			for (int i = 1; i < count; i++)
				action.Invoke();
		}

		public static void TryExecute(Action action)
		{
			TryExecute(action, 5, 1000);
		}

		public static void TryExecute(Action action, int maxTries, int initialTimeout)
		{
			for (int i = 0; i <= maxTries; i++)
			{
				try
				{
					action();
					break;
				}
				catch
				{
					if (i == maxTries)
						throw;
				}
				Thread.Sleep(initialTimeout);
				initialTimeout *= 2;
			}
		}

	    public static MethodResult TryAction<T>(Action<T> action, T param1, string errorText, int maxTries = 5)
	    {
	        var mr = new MethodResult();
	        for (int i = 0; i < maxTries; i++)
	        {
	            try
	            {
	                action(param1);
	                break;
                }
                catch (Exception ex)
	            {
	                if (i == maxTries - 1)
	                {
                        Tracer.Error(ex);

	                    mr = new MethodResult
	                    {
	                        Severity = Severity.Error,
	                        DetailedMessage = ex.ToString(),
	                        Message = $"{errorText}: {ex.Message}"
	                    };
	                }
	                else
	                {
	                    Tracer.Warning($"{errorText}: {ex.Message}");
	                }
	            }
	        }

	        return mr;
	    }

	    public static MethodResult TryAction(Action action, string errorText, int maxTries = 5)
	    {
	        var mr = new MethodResult();
	        for (int i = 0; i < maxTries; i++)
	        {
	            try
	            {
	                action();
	                break;
                }
                catch (Exception ex)
	            {
	                if (i == maxTries - 1)
	                {
	                    Tracer.Error(ex);

                        mr = new MethodResult
	                    {
	                        Severity = Severity.Error,
	                        DetailedMessage = ex.ToString(),
	                        Message = $"{errorText}: {ex.Message}"
	                    };
	                }
	            }
	        }

	        return mr;
	    }

        public static bool IsNullOrEmpty(this string s)
		{
			return string.IsNullOrEmpty(s);
		}

		public static string TrimLine(this string s)
		{
			return s != null ? s.Trim() : String.Empty;
		}
	}

	public interface IServicesStarter
	{
		void StartServices();
		void ShutdownServices();
	}

	public interface ITestableServicesStarter
	{
		string[] TestableServices();
	}





	public class TempFileToTargetFileStruct
	{
		public string cdt { get; set; }
		public string TempTickIdPath { get; set; } = Path.GetTempPath();
		public string TargetTickPath { get; set; }
		public string period { get; set; }
		public string targetfile { get; set; }
		public string statusFile { get; set; }

		public TempFileToTargetFileStruct(string savePathUNC, string userName, string taskName, string tickId,
			string fromDate, string toDate)
		{
		    cdt = $"{DateTime.Now:yyyy-MM-dd}";

		    if (!string.IsNullOrEmpty(savePathUNC))
		    {
		        if (!Directory.Exists(savePathUNC))
		            throw new Exception($"Путь [{savePathUNC}] не доступен для сохранения.");

		        Utils.CreateNonexistsDirectory(Path.Combine(savePathUNC, userName));

		        TargetTickPath = Path.Combine(savePathUNC, userName, taskName, cdt);
		        Utils.CreateNonexistsDirectory(TargetTickPath);
            }

			period = fromDate.Substring(0, 8) != toDate.Substring(0, 8)
				? $"с_{Utils.StringToDateTime(fromDate):yyyy.MM.dd}_по_{Utils.StringToDateTime(toDate):yyyy.MM.dd}"
				: $"за_{Utils.StringToDateTime(toDate):yyyy.MM.dd}";
			targetfile = Path.Combine(TempTickIdPath, $"{taskName}_{tickId}{period}.tmp");
			statusFile = Path.Combine(savePathUNC, tickId);
		}
	}


	public class FileGenerateTemplatePath
	{
		public event PropertyChangedEventHandler InnerFieldChanged;

		private string Template { get; set; }

		private string generatedPath { get; set; } = "";

		public void Apply() => generatedPath = string.Format(Template, Objs);

		public string GetFilePath() => generatedPath;
		

		private object[] Objs { get; set; }


		public FileGenerateTemplatePath(string templatePath, params object[] initArray)
		{
			Template = templatePath;
			Objs = initArray;
		}

		public void updateProps(int[] indexes, params object[] objs)
		{
			for (var i = 0; i <= indexes.Length - 1; i++)
			{
				Objs[indexes[i]] = objs[i];
			}
			Apply();
			InnerFieldChanged?.Invoke(null, null);
		}

		public void updateProp(int i, string value)
		{
			Objs[i] = value;
			Apply();
			InnerFieldChanged?.Invoke(null, null);
		}
	}



	public static class Utils
	{
		/// <summary>
		/// Покажет из определенного поддерева конфигурации текущие настройки.
		/// </summary>
		/// <param name="path">Пример выражения "userSettings/Nbrb.Client.Properties.Settings"</param>
		/// <param name="propName">Пример выражения "startInDebugMode"</param>
		/// <returns></returns>
		public static string getPropertyFromConfigSection(string path, string propName)
		{
			System.Configuration.Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			ClientSettingsSection configClient =
				((ClientSettingsSection) (config.GetSection(path)));
			return configClient.Settings.Get(propName).Value.ValueXml.InnerText;
		}

	public static MemoryStream GenerateStreamFromString(string value)
		{
			return new MemoryStream(Encoding.UTF8.GetBytes(value ?? ""));
		}

		public static JSONConv jsonSvc = new JSONConv();

		public static string Base64Encode(string plainText)
		{
			var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
			return System.Convert.ToBase64String(plainTextBytes);
		}

		/// <summary>
		/// <see cref="string.Split(char[])"/> has no option to ignore casing.
		/// This functions mimics <see cref="string.Split(char[])"/> but also ignores casing.
		/// When called with <see cref="StringSplitOptions.RemoveEmptyEntries"/> <see cref="string.IsNullOrWhiteSpace(string)"/> is used to filter 'empty' entries.
		/// </summary>
		/// <param name="input">String to split</param>
		/// <param name="separators">Array of separators</param>
		/// <param name="options">Additional options</param>
		/// <returns></returns>
		public static IEnumerable<string> SplitInvariantIgnoreCase(this string input, string[] separators, StringSplitOptions options) // ref Dictionary<string,List<int>> insList
		{
			if (separators == null) throw new ArgumentNullException(nameof(separators));
			if (separators.Length <= 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(separators));
			if (string.IsNullOrWhiteSpace(input)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(input));

			// Build a regex pattern of all the separators this looks like aa|bb|cc
			// The Pipe character '|' means alternative.
			var regexPattern = string.Join("|", separators);

			var regexSplitResult = Regex.Split(input, regexPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

			// NOTE To be honest - i don't know the exact behaviour of Regex.Split when it comes to empty entries.
			//      Therefore i doubt that filtering null values even matters - however for consistency i decided to code it in anyways.
			return options.HasFlag(StringSplitOptions.RemoveEmptyEntries) ?
				regexSplitResult.Where(c => !string.IsNullOrWhiteSpace(c))
				: regexSplitResult;
		}

		public static List<string> SplitCI(string input, List<string> splt, ref List<string> _Result)
		{


			foreach (string _splt in splt)
			{
				if (splt.Count() == 1)
					_Result.AddRange(Regex.Split(input, _splt, RegexOptions.IgnoreCase).Select(x => x + _splt[0]).ToList());
				else
				{
					List<string> NewStr = Regex.Split(input, _splt, RegexOptions.IgnoreCase).ToList();
					foreach (string _NewStr in NewStr)
					{
						List<string> NewSplt = splt.ToList();

						NewSplt.Remove(_splt); //Убираем поиск проверенного символа
						var t_NewStr = _NewStr + _splt;

						return SplitCI(t_NewStr, NewSplt, ref _Result);
					}
				}
			}
			return _Result;
		}

		/// <summary>
		/// TimeSpanToText(24h 59m 59s)
		/// </summary>
		/// <param name="tspan"></param>
		/// <returns></returns>
		public static string IntervalString(TimeSpan tspan,string prevText = "") => 
							$"{prevText} {(tspan).Hours} h {(tspan).Minutes:00} m {(tspan).Seconds:00} s ";

		/// <summary>
		/// c 10.01.2024 00:00:00 по 01.02.2024 23:59:59 (24h 59m 59s)
		/// </summary>
		/// <param name="StartDate"></param>
		/// <param name="FinishDate"></param>
		/// <param name="sTime"></param>
		/// <returns></returns>
		public static string FromToString(DateTime StartDate, DateTime FinishDate, string prevText = "" , string sTime = null) => 
$"{prevText} с {StartDate.ToLongTimeString()} по {FinishDate.ToLongTimeString()} ( {sTime ?? IntervalString(FinishDate-StartDate)})";

		public static string GetTextByInputBool(bool val)
		{
			return val ? "Активно" : "";
		}

		public static Dictionary<string,string> TaskStateRus = new Dictionary<string, string>()
		{
			{"Created",		    "Создана"},
			{"Failed",			"Ошибки"},
			{"Running",		    "Запущена"},
			{"Done",			"Выполнена"},
			{"DoneWithWarning", "Выполнена\nс пред."},
			{"DoneNonHistoric", "Выполнена\nне исторически"},
			{"Canceled",		"Отменена"},
			{"WaitingLoading",  "Ожидает загрузки"}
		};

		public static string GenerateAditionalWhereStatement(string fieldName="Code", string queryRow= "A0001-A00009;A215%;A54024;(A54024,A54025,A54026)", string type = "string")
		{// and ( OR (code between 'A0001' and 'A00009') OR (code like ''A215%'') OR (code in ('A54024','A54025','A54026') OR (code = A54024)
			var q = type == "string" ? "'" : ""; // quote?
			var parts = queryRow.Split(new string[]{";"}, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length == 0)
				return null;
			var between = parts.Where(x => x.Contains("-")).ToList();
			var IN = parts.Where(x => x.Contains("(")).ToList();
			var LIKE = parts.Where(x => x.Contains("%")).ToList();
			var eq = parts.Except(between).Except(IN).Except(LIKE).ToList();
			Dictionary<string, IEnumerable<string>> tasks = new Dictionary<string, IEnumerable<string>>();

			if(between.Any())
				tasks.Add(" OR ({0} between {1})", between.Select(x=> $"{q}{x.Split('-')[0]}{q} and {q}{x.Split('-')[1]}{q}")); //'{1}','{2}'
			if (LIKE.Any())
				tasks.Add(" OR ({0} like {1})", LIKE.Select(x=>$"'{x}'"));
			if (IN.Any())
				tasks.Add(" OR ({0} in ({1}))", IN.Select(x=>$"{string.Join(",", (x.Replace("(", "").Replace(")", "")).Split(',').Select(y=> q+y+q))}"));//.Split(',')
			if (eq.Any())
				tasks.Add(" OR ({0} = '{1}')", eq);//.Split(',')

			StringBuilder sb = new StringBuilder(" and ("+ Environment.NewLine);

			/*генерируем условия по OR */
			foreach (var key in tasks.Keys)
			{
				sb.Append("\r\n");
				sb.Append($"{string.Join("\r\n",tasks[key].Select(x => string.Format(key, fieldName, x)))}");
			}
			sb.Append("\r\n)");
			var repl = @" and ((";
			return sb.ToString()
					.Replace("and ( OR", repl)
					.Replace(@" and (
 OR (", repl)
					.Replace(@" and (

 OR (", repl)
					.Replace(@" and (


 OR (", repl);
		}

		public static string GetDateInterval(TimeSpan totalTime) =>
							$"{totalTime.Hours}h {totalTime.Minutes:00} m {totalTime.Seconds:00} s ";

		public static List<int> RowToIntList(string res)
		{
			var colNoAutoSize = res.Split(new string[] { ",", ";", "|", "#" }, StringSplitOptions.RemoveEmptyEntries)?.Select(x =>
			{
				int.TryParse(x, out int vi);
				return vi;
			}).ToList();
			return colNoAutoSize;
		}


        public static void insertToLog(string path, string message)
		{
            
			var i = 0;
			while (i < 5)
			{
				try
				{
					using (StreamWriter sw = new StreamWriter(path, true, Encoding.UTF8))
					{                        
						sw.Write($"|{message}");
                        break;
					}
				}
				catch
				{
					Thread.Sleep(100);
				}

				i++;
			}
		}

		public static string getStatusesFromFile(TempFileToTargetFileStruct ttf)
		{
			return TextReader.Synchronized(File.OpenText(ttf.statusFile)).ReadToEnd();
		}

		public static Dictionary<string,MethodResult> JobState = new Dictionary<string,MethodResult>();
		static NumberFormatInfo _numFmt;

		static NumberStyles decimalStyle =
			NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.Integer;

		const string __dateTimeFormatSys = "yyyy-MM-dd HH:mm:ss";
		static readonly DateTime s_DateTimeMinValue = new DateTime(1800, 1, 1);

		static Utils()
		{
			lock (typeof(Utils))
			{
				CultureInfo info = new CultureInfo("ru-RU", true);
				_numFmt = info.NumberFormat;
				_numFmt.NumberDecimalSeparator = ".";
				_numFmt.CurrencyDecimalSeparator = ".";
			}
		}

		public static bool ValidatePath(string path)
		{
			Regex regex = new Regex(@"^(([a-zA-Z]:\\)|\\\\)(((?![<>:""/\\|? *]).)+((?<![ .])\\)?)*$");
			return regex.IsMatch(path);
		}

		public static Dictionary<string, bool> wwTickState = new Dictionary<string, bool>();

		public static bool CreateNonexistsDirectory(string bkpFilePath)
		{
			//var IsValidPath = ValidatePath(bkpFilePath);
			if (/*IsValidPath &&*/ !Directory.Exists(bkpFilePath))
				Directory.CreateDirectory(bkpFilePath);
			return true;
		}

		public static string numEnding(int perem, string oneName = "запись", string multyName = "записи",
			string multy2Name = "записей")
		{
			var tailEnd = int.Parse(perem.ToString().Substring(perem.ToString().Length - 1, 1));
			int.TryParse(perem.ToString().Substring(perem.ToString().Length - (perem.ToString().Length >1 ? 2 : 1), 1),out var tailPrev);
			return perem + " " + (
				       tailEnd == 1 ? oneName : 
				       tailPrev != 1 && (tailEnd == 2 || tailEnd == 3 || tailEnd == 4) ? multyName : multy2Name
			       );
		}

		public static void numEnding_Tests()
		{
			var lst = Enumerable.Range(1, 1000);
			var checks = lst.Select(x => new Tuple<int, string>(x, numEnding(x))).ToList();
		}

		/// <summary>
		/// Generate xmlElements KeyValue From Dictionary(string,string) <row><key>{0}</key><value>{1}</value></row>
		/// </summary>
		/// <param name="dict"></param>
		/// <returns></returns>
		public static string GetXml(Dictionary<string, string> dict)
		{
			const string xmlTemplate = @"<row><key>{0}</key><value>{1}</value></row>";
			return string.Join(Environment.NewLine, dict.Select(x => string.Format(xmlTemplate, x.Key, x.Value)));
		}

		public static string GetXml(List<string> InputValues)
		{
			const string xmlTemplate = @"<row><value>{0}</value></row>";
			return string.Join(Environment.NewLine, InputValues.Select(x => string.Format(xmlTemplate, x)));
		}

		public static string GenSelectStrByDimActualization(string targetDimName, string outxml, DateTime actDt)
		{
			return string.Format("declare @xml xml = '" + outxml + "'" + Environment.NewLine +
			                     @"select kv.[Code], kv.[FkCode], ISNULL(fkt.Code,'[NoLinkedDim]') from 
(SELECT  
       Tbl.Col.value('(*:key)[1]', 'varchar(100)') [Code],
       Tbl.Col.value('(*:value)[1]', 'varchar(4000)') [FkCode]
FROM   @xml.nodes('*') Tbl(Col)) kv
Left Outer join (Select Code from dbo.Dim_{1}_View where CONVERT(DateTime, '{0:yyyy-MM-dd}', 102) between DateCreate and DateClose) fkt on (fkt.Code = kv.FkCode )",
				actDt, targetDimName);
		}

		public static DateTime StringToDateTime(string inputVal)
		{
		    if (DateTime.TryParseExact(inputVal, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
		        return dt;

		    if (DateTime.TryParseExact(inputVal, "yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
		        return dt;

		    if (DateTime.TryParseExact(inputVal, "yyyyMMdd h:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
		        return dt;

		    if (DateTime.TryParseExact(inputVal, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
		        return dt;

		    if (DateTime.TryParseExact(inputVal, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
		        return dt;

		    if (DateTime.TryParseExact(inputVal, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
		        return dt;

		    if (DateTime.TryParseExact(inputVal, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
		        return dt;

		    if (DateTime.TryParseExact(inputVal, "dd.MM.yyyy h:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
		        return dt;

		    if (!DateTime.TryParse(inputVal, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
		        TraceLogToFile.Write($"Ошибка распознования даты {inputVal} {dt:yyyyMMdd hh:mm:ss}");

		    return dt;
		}

		public static string ConvertToString(object value)
		{
			return ConvertToString(value, false);
		}

		public static string ConvertToString(object value, bool treatNullAsEmptyString)
		{
			string retValue;
			if (value == null)
				retValue = treatNullAsEmptyString ? String.Empty : null;
			else
			{
				TypeCode typeCode;
				if (value is IConvertible)
					typeCode = ((IConvertible) value).GetTypeCode();
				else
					typeCode = Type.GetTypeCode(value.GetType());

				switch (typeCode)
				{
					case TypeCode.Decimal:
					case TypeCode.Double:
					case TypeCode.Single:
						retValue = (string) Convert.ChangeType(value, typeof(string), _numFmt);
						break;
					case TypeCode.DateTime:
						retValue = dateToString((DateTime) value);
						break;
					case TypeCode.Object:
						throw new NotSupportedException("Type not supported: " + value.GetType().FullName);
					default:
						retValue = value.ToString();
						break;
				}
			}

			return retValue;
		}

		public static T ConvertFromString<T>(string value)
		{
			return (T) ConvertFromString(value, typeof(T));
		}


		public static object ConvertFromString(string value, Type targetType)
		{
			AssertUtils.IsNotNull(targetType, "targetType");

			if (Equals(value, DBNull.Value))
				value = null;

			if (targetType == typeof(string))
				return value;

			//if (value == null && targetType.IsValueType && !allowNullForValueTypes)
			//    throw new ArgumentNullException("value");

			Type underlyingType;
			if (targetType.IsNullableType(out underlyingType))
			{
				if (string.IsNullOrEmpty(value))
					return null;
				else
					targetType = underlyingType;
			}

			if (targetType.IsEnum)
				return ConvertEnumValue(targetType, value);
			return ConvertValueFromTypeCode(targetType, value);
		}

		public static bool IsNullableType(this Type targetType, out Type underlyingType)
		{
			if (targetType.IsGenericType)
			{
				if (typeof(Nullable<>) == targetType.GetGenericTypeDefinition())
				{
					underlyingType = targetType.GetGenericArguments()[0];
					return true;
				}
			}

			underlyingType = null;
			return false;
		}

		private static object ConvertEnumValue(Type targetType, string value)
		{
			return string.IsNullOrEmpty(value) ? Enum.ToObject(targetType, 0) : Enum.Parse(targetType, value, true);
		}

		private static object ConvertValueFromTypeCode(Type targetType, string value)
		{
			object retValue;
			object src = value;
			if (string.IsNullOrEmpty(value) && targetType.IsValueType)
				src = 0;
			TypeCode typeCode = Type.GetTypeCode(targetType);
			switch (typeCode)
			{
				case TypeCode.Decimal:
				case TypeCode.Double:
				case TypeCode.Single:
					retValue = Convert.ChangeType(src, targetType, _numFmt);
					break;
				case TypeCode.DateTime:
					retValue = string.IsNullOrEmpty(value) ? s_DateTimeMinValue : parseDateTime(value);
					break;
				case TypeCode.Object:
					throw new NotSupportedException("target type not supported: " + targetType.FullName);
				default:
					retValue = Convert.ChangeType(src, targetType);
					break;
			}

			return retValue;
		}

		public static object cloneSerializableObject(object obj)
		{
			BinaryFormatter fmt = new BinaryFormatter();
			using (MemoryStream stream = new MemoryStream())
			{
				fmt.Serialize(stream, obj);
				stream.Seek(0, SeekOrigin.Begin);
				object clone = fmt.Deserialize(stream);
				return clone;
			}
		}

		public static bool tryParseInt(string s)
		{
			int result;
			return tryParseInt(s, out result);
		}

		public static bool tryParseInt(string s, out int result)
		{
			double _result;
			bool success = double.TryParse(s, NumberStyles.Integer, _numFmt, out _result);
			result = (int) _result;
			return success;
		}

		public static bool tryParseDecimal(string s)
		{
			decimal result;
			return tryParseDecimal(s, out result);
		}

		public static bool tryParseDecimal(string s, out decimal result)
		{
			double _result;
			bool success = double.TryParse(s, decimalStyle, _numFmt, out _result);
			result = (decimal) _result;
			return success;
		}

		public static Decimal parseDecimal(string s)
		{
			if (s == null || s.Length == 0)
				return 0;
			try
			{
				return Decimal.Parse(s, _numFmt);
			}
			catch (FormatException ex)
			{
				throw new FormatException("Input string was not in correct format: " + s);
			}

		}

		public static byte[] parseBlob(string s)
		{
			if (s == null || s.Length == 0)
				return new byte[0];
			if (s.Length % 2 != 0)
				throw new ArgumentException("This is not a correct blob value");
			byte[] result = new byte[s.Length / 2];
			for (int i = 0; i < result.Length; i++)
				result[i] = byte.Parse(s.Substring(i * 2, 2), NumberStyles.HexNumber);
			return result;
		}

		public static string formatBlob(Byte[] blob)
		{
			if (blob == null || blob.Length == 0)
				return String.Empty;
			System.Text.StringBuilder result = new System.Text.StringBuilder(blob.Length * 2);
			foreach (byte aByte in blob)
				result.Append(aByte.ToString("X2"));
			return result.ToString();
		}

		public static string formatDecimal(Decimal val)
		{
			return val.ToString("N", _numFmt);
		}

		/// <summary>
		/// Преобразование даты в строку для отображения в UI (формат dd.mm.yyyy)
		/// </summary>
		public static string formatDate(DateTime date)
		{
			return date.ToString("dd.MM.yyyy");
		}

		/// <summary>
		/// Преобразование даты в строку для отображения в UI (формат dd.mm.yyyy HH:mm:ss)
		/// </summary>
		public static string formatDateTime(DateTime date)
		{
			return date.ToString("dd.MM.yyyy HH:mm:ss");
		}

		public static string getShortName(string fqn)
		{
			if (isEmpty(fqn))
				return fqn;
			int pos = fqn.LastIndexOf('.');
			if (pos >= 0)
				return fqn.Substring(pos + 1);
			else
				return fqn;
		}

		public static string removeAssemblyName(string fullTypeName)
		{
			if (isEmpty(fullTypeName))
				return fullTypeName;
			int pos = fullTypeName.IndexOf(',');
			if (pos >= 0)
				return fullTypeName.Substring(0, pos);
			else
				return fullTypeName;
		}

		public static string firstLetter(string s)
		{
			if (s == null)
				return String.Empty;
			if (s.Length == 0)
				return String.Empty;
			return new string(s[0], 1);
		}

		public static string capitalizeFirst(string name)
		{
			if (isEmpty(name))
				return name;
			name = name.Substring(0, 1).ToUpper() + name.Substring(1).ToLower();
			return name;
		}

		public static Hashtable StringToHashTable(string data)
		{
			return StringToHashTable(data, "&", "=");
		}

		public static Hashtable StringToHashTable(string data, string pairSeparator, string keyvalueSeparator)
		{
			string[] namevalue = data.Split(pairSeparator.ToCharArray());
			Hashtable ht = new Hashtable();
			foreach (string pair in namevalue)
			{
				ht.Add(pair.Split(keyvalueSeparator.ToCharArray())[0], pair.Split(keyvalueSeparator.ToCharArray())[1]);
			}

			return ht;
		}

		public static string getKeyValue(string str, string keyName, bool raiseError)
		{
			Match m = Regex.Match(str, keyName + "=(?<value>[^;]*)(;|$)");
			if (m.Success)
				return m.Groups["value"].Value;
			else if (raiseError)
				throw new NotFoundException("Не найдено значение ключа: " + keyName + "\r\nв строке:\r\n" + str);
			else
				return null;
		}

		public static string _string(object obj)
		{
			return (obj == null) ? "_NULL_" : obj.ToString();
		}

		public static bool isEmpty(string sValue)
		{
			return string.IsNullOrEmpty(sValue);
		}

		public static string toString(object obj)
		{
			return obj != null ? obj.ToString() : String.Empty;
		}

		public static void checkName(string aName)
		{
			if (aName == null)
				throw new ArgumentException("Не задано значение переменной (null)");
			if (aName.Trim() == "")
				throw new ArgumentException("Не задано значение переменной (empty string)");
		}

		public static string generateOID()
		{
			return "_" + Guid.NewGuid().ToString();
		}

		public static string stripQuotes(string val)
		{
			if (isEmpty(val) || val.Length < 2)
				return val;
			char c = val[0];
			if (c == '\'' || c == '\"')
				return val.Substring(1, val.Length - 2);
			else
				return val.Trim();
		}

		public static DateTime parseDateTime(string sValue)
		{
			return DateTime.ParseExact(sValue, __dateTimeFormatSys, CultureInfo.CurrentCulture.DateTimeFormat);
		}

		/// <summary>
		/// Преобразование даты в строку для использования в SQL-запросах (формат yyyy-MM-dd HH:mm:ss)
		/// </summary>
		public static string dateToString(DateTime date)
		{
			return date.ToString(__dateTimeFormatSys);
		}

		public static string decimalToString(Decimal val)
		{
			return val.ToString(_numFmt);
		}

		public static string OCLConstantEncode(string origConst)
		{
			string codeConst = origConst.Replace(@"\", @"\\");
			codeConst = codeConst.Replace(@"""", @"\""");
			return codeConst;
		}

		public static string OCLConstantDecode(string codeConst)
		{
			string origConst = codeConst.Replace(@"\""", @"""");
			origConst = origConst.Replace(@"\\", @"\");
			return origConst;
		}

		public static int GetIntFromObject(object obj, int defaultValue)
		{
			return obj == null ? defaultValue : (int) obj;
		}

		public static DateTime GetDateTimeFromObject(object obj, DateTime defaultValue)
		{
			return obj == null ? defaultValue : (DateTime) obj;
		}

		public static decimal GetNumberFromObject(object obj, decimal defaultValue)
		{
			return obj == null ? defaultValue : (decimal) obj;
		}

		public static int GetIntFromObject(object obj)
		{
			return GetIntFromObject(obj, 0);
		}

		public static DateTime GetDateTimeFromObject(object obj)
		{
			return GetDateTimeFromObject(obj, DateTime.MinValue);
		}

		public static decimal GetNumberFromObject(object obj)
		{
			return GetNumberFromObject(obj, 0);
		}

		public static string Trim(string aValue)
		{
			return aValue == null ? aValue : aValue.Trim();
		}

		public static DateTime AdjustDateTimeValue(DateTime dt)
		{
			return new DateTime(dt.Ticks - 10000 * dt.Millisecond, dt.Kind);
		}

		public static object AdjustDateTimeValue(object obj)
		{
			if (obj is DateTime)
			{
				DateTime dt = (DateTime) obj;
				return new DateTime(dt.Ticks - 10000 * dt.Millisecond, dt.Kind);
			}
			else
				return obj;
		}

		private static bool IsCorrectXmlCode(int code)
		{
			// Это все допустимые двухбайтовые диапазоны.
			// Откуда я их взял - руками проверил!
			return code == 9 || code == 10 || code == 13 || (31 < code && code < 55296) || (57343 < code && code < 65534);
		}

		public static string ReplaceIncorrectXmlChars(this string text)
		{
			if (text == null)
				return String.Empty;
			bool hasIncorrectChars = false;
			foreach (var letter in text)
			{
				int code = (int) letter;
				if (!IsCorrectXmlCode(code))
				{
					hasIncorrectChars = true;
					break;
				}
			}

			if (hasIncorrectChars)
			{
				StringBuilder result = new StringBuilder(text.Length + 10);
				foreach (var letter in text)
				{
					int code = (int) letter;
					if (IsCorrectXmlCode(code))
						result.Append(letter);
					else
						result.Append('%').Append(code);
				}

				return result.ToString();
			}
			else
				return text;

			// А это первая версия...
			//return text != null ? text.Replace('\x0000', ' ') : String.Empty;
		}

		public static string NullIfEmpty(this string val)
		{
			if (val == String.Empty)
				return null;
			return val;
		}

		public static string GetDateIn126(DateTime dt)
		{
			return string.Format("{0}-{1:00}-{2:00}T{3:00}:{4:00}:{5:00}.{6:000}", dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute,
				dt.Second, dt.Millisecond);
		}

		public static DateTime GetDateTimeLastNow() => DateTime.Now.Date.AddDays(1).AddSeconds(-1);
		public static DateTime GetDateTimeFirstNow(int DayMinus) => DateTime.Now.Date.AddDays(DayMinus);
	}

	public static class ConvertDate2XML
	{
		private const string c_Type = "Type";
		private const string c_Items = "items";
		private const string c_main_prm = "<xml><Type>{0}</Type> <items>{1}</items> </xml>";

		static string GetDate(DateTime dt)
		{
			return string.Format("{0:0000}-{1:00}-{2:00}", dt.Year, dt.Month, dt.Day);
		}

		static DateTime GetDate(string prStr)
		{
			var pr = prStr.Split(new string[] {"-"}, StringSplitOptions.None);
			if (pr.Length != 3)
				throw new Exception("Bad Date format [" + prStr + "]");
			return GetDate(pr);
		}

		static DateTime GetDate(string[] pr)
		{
			return new DateTime(Convert.ToInt32(pr[0]), Convert.ToInt32(pr[1]), Convert.ToInt32(pr[2]));
		}

		public static string ConvertToString(this Dictionary<int, Tuple<DateTime, bool, bool>> actualization)
		{
			string res = string.Format(c_main_prm,
				c_Dictionary_Int2_DateTime_Bool,
				string.Join("|",
					actualization.Select(x => string.Format("{0};{1};{2}", x.Key, GetDate(x.Value.Item1), x.Value.Item2)))
			);
			return res;
		}

		public static string ConvertToString(this Dictionary<int, KeyValuePair<DateTime, bool>> actualization)
		{
			string res = string.Format(c_main_prm,
				c_Dictionary_Int2_DateTime_Bool,
				string.Join("|",
					actualization.Select(x => string.Format("{0};{1};{2}", x.Key, GetDate(x.Value.Key), x.Value.Value)))
			);
			return res;
		}

		public static string ConvertToString(this Dictionary<int, DateTime> actualization)
		{
			string res = string.Format(c_main_prm,
				"Dictionary int, DateTime",
				string.Join("|", actualization.Select(x => string.Format("{0};{1}", x.Key, GetDate(x.Value))))
			);
			return res;
		}

		private const string c_Dictionary_Int2_DateTime_Bool = "Dictionary int, DateTime,bool";

		public static Dictionary<int, DateTime> ConvertToContainer(this string str)
		{
			Dictionary<int, DateTime> res = new Dictionary<int, DateTime>();
			var xDoc = XDocument.Parse(str);
			if (xDoc.Root.HasElements)
			{
				string tp = xDoc.Root.Element(c_Type).Value;
				if (tp == "Dictionary int, DateTime")
				{
					string[] items = xDoc.Root.Element(c_Items).Value.Split(new string[] {"|"}, StringSplitOptions.None);
					foreach (var prItem in items)
					{
						string[] pr = prItem.Split(new[] {";", "-"}, StringSplitOptions.None);
						if (pr.Length != 4)
							continue;
						int id = Convert.ToInt32(pr[0]);
						DateTime dt = GetDate(new String[] {pr[1], pr[2], pr[3]});
						res[id] = dt;
					}

				}
			}

			return res;
		}

		public static Dictionary<int, KeyValuePair<DateTime, bool>> ConvertToContainerDateBool(this string str)
		{
			Dictionary<int, KeyValuePair<DateTime, bool>> res = new Dictionary<int, KeyValuePair<DateTime, bool>>();
			var xDoc = XDocument.Parse(str);
			if (xDoc.Root.HasElements)
			{
				string tp = xDoc.Root.Element(c_Type).Value;
				if (tp == c_Dictionary_Int2_DateTime_Bool)
				{
					string[] items = xDoc.Root.Element(c_Items).Value.Split(new string[] {"|"}, StringSplitOptions.None);
					foreach (var prItem in items)
					{
						string[] pr = prItem.Split(new[] {";", "-"}, StringSplitOptions.None);
						if (pr.Length != 5)
							continue;
						int id = Convert.ToInt32(pr[0]);
						DateTime dt = GetDate(new String[] {pr[1], pr[2], pr[3]});

						res[id] = new KeyValuePair<DateTime, bool>(dt, Convert.ToBoolean(pr[4]));
					}

				}
			}

			return res;
		}

		public static string ConvertToString(this Dictionary<int, List<int>> actualization)
		{
			string res = string.Format(c_main_prm,
				"Dictionary int, List int",
				string.Join("|",
					actualization.Select(x =>
						string.Format("{0};[{1}]", x.Key, string.Join(",", x.Value.Select(y => string.Format("{0}", y))))))
			);
			return res;
		}

		public static string ConvertToStringList(this Dictionary<int, List<System.Tuple<DateTime, int, int>>> actualization)
		{
			string add = string.Join("|",
				actualization.Select(x => string.Format("{0};[{1}]", x.Key,
					string.Join("_", x.Value.Select(y => string.Format("{0},{1},{2}", GetDate(y.Item1), y.Item2, y.Item3))))));

			string res = string.Format(c_main_prm,
				"Dictionary int, Date int int[]",
				add
			);
			return res;
		}

		public static string ConvertToString(this Dictionary<int, System.Tuple<DateTime, int, int>[]> actualization)
		{
			string add = string.Join("|",
				actualization.Select(x => string.Format("{0};[{1}]", x.Key,
					string.Join("_", x.Value.Select(y => string.Format("{0},{1},{2}", GetDate(y.Item1), y.Item2, y.Item3))))));

			string res = string.Format(c_main_prm,
				"Dictionary int, Date int int[]",
				add
			);
			return res;
		}

		public static Dictionary<int, System.Tuple<DateTime, int, int>[]> ConvertToContainer2(this string str)
		{
			Dictionary<int, System.Tuple<DateTime, int, int>[]> res = new Dictionary<int, System.Tuple<DateTime, int, int>[]>();
			var xDoc = XDocument.Parse(str);
			if (xDoc.Root.HasElements)
			{
				string tp = xDoc.Root.Element(c_Type).Value;
				if (tp == "Dictionary int, Date int int[]")
				{
					string[] items = xDoc.Root.Element(c_Items).Value.Split(new string[] {"|"}, StringSplitOptions.None);
					foreach (var prItem in items)
					{
						string[] pr = prItem.Split(new[] {";"}, StringSplitOptions.None);
						if (pr.Length != 2)
							continue;
						int id = Convert.ToInt32(pr[0]);
						if (pr[1].StartsWith("[") && pr[1].EndsWith("]"))
						{
							string lst = pr[1].Substring(1, pr[1].Length - 2);
							if (lst.Length > 1)
							{
								List<System.Tuple<DateTime, int, int>> lstArr = new List<System.Tuple<DateTime, int, int>>();
								foreach (var nm in lst.Split(new[] {"_"}, StringSplitOptions.None))
								{
									var nm1 = nm.Split(new[] {","}, StringSplitOptions.None);
									if (nm1[0].Length > 3)
										lstArr.Add(new Tuple<DateTime, int, int>(GetDate(nm1[0]), Convert.ToInt32(nm1[1]), Convert.ToInt32(nm1[2])));
								}

								res[id] = lstArr.ToArray();
							}
						}
					}

				}
			}

			return res;
		}
	}

	public class MethodDesc
	{
		public string methodName { get; set; }
		public string typeReference { get; set; }
		public Dictionary<string, string> paramList { get; set; }
		public Dictionary<string, string> customParamList { get; set; }

		public MethodDesc()
		{
			paramList = new Dictionary<string, string>();
			customParamList = new Dictionary<string, string>();
		}

		public MethodDesc(string MethodName, string TypeReference, Dictionary<string, string> ParamList,
			Dictionary<string, string> CustomParamList)
		{
			methodName = MethodName;
			typeReference = TypeReference;
			paramList = ParamList;
			customParamList = CustomParamList;
		}
	}

	public class JobDescDict
	{
		public JobDescDict(List<JobDesc> src)
		{
			var res = src.GroupBy(x => x.Summary, (y, a) => a.ToList());
		}
	}

	public class JobDesc
	{
		/*job.Summary , x.Name, x.Visualizer*/
		public int Index { get; set; }
		public string Summary { get; set; }
		public string ParamName { get; set; }
		public string ParamType { get; set; }
		public string Visualizer { get; set; }

		public JobDesc()
		{
		}

		public JobDesc(int index, string summary, string paramName, string paramType, string visualizer)
		{
			Index = index;
			Summary = summary;
			ParamName = paramName;
			ParamType = paramType;
			Visualizer = visualizer;
		}

		public override bool Equals(object obj)
		{
			JobDesc part = obj as JobDesc;

			return this.Index == part.Index;
		}
		public override int GetHashCode()
		{
			return this.Index.GetHashCode();
		}
	}
}
