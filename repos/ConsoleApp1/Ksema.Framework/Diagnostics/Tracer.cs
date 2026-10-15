using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Framework.Utils;
using System.Runtime.CompilerServices;
using System.Text;
using Ksema.Framework.Caching;
using System.ComponentModel.DataAnnotations;
using Ksema.Framework.Utils;


namespace Ksema.Framework.Diagnostics
{
    public enum LoginStatus
    {
        None, IsEWebException
    }

    public enum LogRowType
    {
        INFO, WARN, ERROR
    }

    public static class EnumExtensions
    {
        public static string nm(this Enum enumValue)
        {
            return enumValue.GetType()
                    .GetMember(enumValue.ToString())
                    .First()
                    .GetCustomAttributes(typeof(DisplayAttribute), false)
                    .Select(x => (DisplayAttribute)x).FirstOrDefault()
                    ?.GetName()
                ;
        }
    }


    public static class Tracer
    {
        const string ROOT_NAME = "Application";
        static readonly DictionaryDataCache<string, TraceSourceBase> s_traceSources;

        static Tracer()
        {
            s_traceSources = new DictionaryDataCache<string, TraceSourceBase>(
                "Trace.Sources", name =>
                 {
                     TraceSourceBase parentSource = GetParentSource(name);
                     return new TraceSourceBase(name, parentSource);
                 }, 
                 StringComparer.OrdinalIgnoreCase);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ITraceSource GetTraceSource()
        {
            StackTrace st = new StackTrace(2);
            if (st.FrameCount == 0)
                return RootTraceSource;
            return GetTraceSource(st.GetFrame(0).GetMethod().DeclaringType);
        }

        static TraceSourceBase GetParentSource(string name)
        {
            if (String.Compare(name, ROOT_NAME, true) == 0)
                return null;

            int pos = name.LastIndexOf('.');
            if (pos < 0)
                return s_traceSources[ROOT_NAME];

            string parentName = name.Substring(0, pos);
            TraceSourceBase ts = s_traceSources[parentName];
            return ts;
        }

        public static ITraceSource RootTraceSource
        {
            get 
            {
                return GetTraceSource(ROOT_NAME);
            }
        }

        public static ITraceSource GetTraceSource(string name)
        {
            AssertUtils.IsNotEmpty(name, "name");
            return s_traceSources[name];
        }

        public static ITraceSource GetTraceSource(Type type)
        {
            string name = TraceSourceAttribute.GetTraceSourceName(type);
            return GetTraceSource(name);
        }

        [MethodImpl(MethodImplOptions.NoInlining), Conditional("TRACE")]
        public static void Error(Exception ex)
        {
            ITraceSource ts = GetTraceSource();
			if (EssentialAppMgr.App != null)
				ex = EssentialAppMgr.App.ProcessError(ex);
            ts.Error(ex);
        }

        [MethodImpl(MethodImplOptions.NoInlining), Conditional("TRACE")]
        public static void Error(string format, params object[] args)
        {
            ITraceSource ts = GetTraceSource();
            ts.Error(format, args);
        }

        [MethodImpl(MethodImplOptions.NoInlining), Conditional("TRACE")]
        public static void Warning(string message)
        {
            ITraceSource ts = GetTraceSource();
            ts.Warning(message);
        }

        [MethodImpl(MethodImplOptions.NoInlining), Conditional("TRACE")]
        public static void Info(string message)
        {
            ITraceSource ts = GetTraceSource();
            ts.Info(message);
        }

        [MethodImpl(MethodImplOptions.NoInlining), Conditional("TRACE")]
        public static void Info(string format, params object[] args)
        {
            ITraceSource ts = GetTraceSource();
            ts.Info(format, args);
        }

        [MethodImpl(MethodImplOptions.NoInlining), Conditional("TRACE")]
        public static void Info(TraceData data)
        {
            ITraceSource ts = GetTraceSource();
            ts.Trace(TraceEventType.Information, data);
        }

        [MethodImpl(MethodImplOptions.NoInlining), Conditional("TRACE")]
        public static void StartOp(string format, params object[] args)
        {
            ITraceSource ts = GetTraceSource();
            ts.StartOp(format, args);
        }

        [MethodImpl(MethodImplOptions.NoInlining), Conditional("TRACE")]
        public static void StopOp(string format, params object[] args)
        {
            ITraceSource ts = GetTraceSource();
            ts.StopOp(format, args);
        }

        [Conditional("TRACE")]
        public static void Error(this ITraceSource ts, Exception ex)
        {
            ts.Trace(TraceEventType.Error, new TraceData(ex.ToString()));
        }

        [Conditional("TRACE")]
        public static void Error(this ITraceSource ts, string format, params object[] args)
        {
            ts.Trace(TraceEventType.Error, new TraceData(format, args));
        }

        [Conditional("TRACE")]
        public static void Warning(this ITraceSource ts, string message)
        {
            ts.Trace(TraceEventType.Warning, new TraceData(message));
        }

        [Conditional("TRACE")]
        public static void Info(this ITraceSource ts, string message)
        {
            ts.Trace(TraceEventType.Information, new TraceData(message));
        }

        [Conditional("TRACE")]
        public static void Info(this ITraceSource ts, string format, params object[] args)
        {
            ts.Trace(TraceEventType.Information, new TraceData(format, args));
        }

        [Conditional("TRACE")]
        public static void StartOp(this ITraceSource ts, string format, params object[] args)
        {
            ts.Trace(TraceEventType.Start, new TraceData(format, args));
        }

        [Conditional("TRACE")]
        public static void StopOp(this ITraceSource ts, string format, params object[] args)
        {
            ts.Trace(TraceEventType.Stop, new TraceData(format, args));
        }

    }

	public class TraceResultToFile
	{
		private StreamWriter _wr;
		private bool _write2File = true;
		public bool NumMsg { get; set; }
		public TraceResultToFile (string file, bool append = false, string write2Files = "true", bool isSynch = true)
		{
			_write2File = CanWrite2File(write2Files);
			_isSynch = isSynch;
			InitTraceFile(file, append);
		}

		private static Dictionary<string, DateTime> _prevRunKey;
		public void CheckDifBetrveenPrevRun(string key, int maxLen)
		{
			if (!_write2File)
				return;
			if(_prevRunKey==null)
				_prevRunKey = new Dictionary<string, DateTime>();
			if (_prevRunKey.ContainsKey(key))
			{
				if ( ( DateTime.Now - _prevRunKey[key] ).TotalSeconds > maxLen )
					Write( string.Format("Внимание !. Интервал между прошлым и текущим запуском {0} секунд" , (DateTime.Now - _prevRunKey[key]).TotalSeconds),true);
			}
			_prevRunKey[key] = DateTime.Now;
		}
		public void SetDifBetrveenPrevRun(string key)
		{
			if (!_write2File)
				return;
			if (_prevRunKey==null)
				_prevRunKey = new Dictionary<string, DateTime>();
			_prevRunKey[key] = DateTime.Now;
		}
		private void InitTraceFile (string file, bool append,bool err=false)
		{
			try
			{
				if (_write2File)
				{
					Directory.CreateDirectory(Path.GetDirectoryName(file));
					_wr = new StreamWriter(file, append);
					_wr.WriteLine("");
				}
			}
			catch (Exception ex)
			{
				Trace.TraceError("Невозможно открыть файл " + file + " " + ex.Message);
				if (!err)
				{
					string newP = Path.GetDirectoryName(file) + "\\" + Path.GetFileNameWithoutExtension(file) + string.Format("_{0:000}_{1}", DateTime.Now.Millisecond, DateTime.Now.Ticks ) + Path.GetExtension(file);
					InitTraceFile(newP, append, true);
					Write("Error " + ex.Message );
				}
				else
				{
					_wr = null;
				}
			}
		}

		public TraceResultToFile (string file, bool append = false, bool write2Files = true, bool isSynch = true)
		{
			_write2File = write2Files;
			_isSynch = isSynch;
			InitTraceFile(file, append);
		}

		private static bool CanWrite2File (string write2Files)
		{
			try
			{
				return Convert.ToBoolean(write2Files);
			}
			catch
			{
				return false;
			}
		}

		public void MarkLine ()
		{
			if (_wr != null)
				_wr.WriteLine("===========================================================");
		}

		private bool _isSynch;

		private int _LnCnt = 0;
		private string BoolMark
		{
			get
			{
				if (NumMsg)
				{
					if (_LnCnt++ == 0)
						return "";
					return string.Format("[{0}]", _LnCnt);
				}

				return "";
			}
		}
		public void Write (string line, bool mark = false)
		{
			if (_wr == null)
				return;
			try
			{
				DateTime now = DateTime.Now;
				StringBuilder forWr = new StringBuilder();
				if (mark)
					forWr.AppendLine("===========================================================");
				forWr.AppendLine(now.ToShortDateString() + " " + now.ToLongTimeString() + string.Format(":{0:000} :", now.Millisecond) + BoolMark + line);
				if (mark)
					forWr.AppendLine("===========================================================");
				if (_isSynch)
				{
					_wr.Write(forWr);
					_wr.Flush();
				}
				else
				{
					if (_lst == null)
						_lst = new List<StringBuilder>();
					_lst.Add(forWr);
				}
			}
			catch (Exception e)
			{
				;
			}
		}

		public void Finish ()
		{
			BackgroundWorker bg = new BackgroundWorker();
			bg.DoWork += (sender, args) =>
			{
				foreach (var forWr in _lst)
				{
					_wr.Write(forWr);
					_wr.Flush();
				}
			};
			bg.RunWorkerAsync();
		}
		List<StringBuilder> _lst = new List<StringBuilder>();
		public void Close ()
		{
			if (_wr != null)
				_wr.Close();
			_wr = null;
		}
	}

	public class TraceLogToFile
	{
		public static void CreateDirOnNonExist(string storePath)
		{
			if (!Directory.Exists(storePath))
				Directory.CreateDirectory(storePath);
		}

		private static readonly object Lock = new object();
		private static string lastMsg = "";
		public static DateTime lastWriDateTime { get; set; }

		static TraceLogToFile()
		{
			lastWriDateTime = DateTime.Now;
		}

		private static readonly Dictionary<int, int> posIndexDict = new Dictionary<int, int>();
		private static bool _onInit = false;
		private static bool _hasFails = false;
		private static void hasWriteLock()
		{
			lock (Lock)
			{
				_hasFails = true;
				posIndexDict[day]++;
			}
		}

		public static void initLogPath(string NewPath)
		{
			lock (Lock)
				LogRoot = NewPath;
		}

		public static string getLogRows()
		{
			lock (Lock)
			{
				using (StreamReader sr = new StreamReader(NowLogPath()))
				{
					return sr.ReadToEnd();
				}
			}
		}

		private static string _LogRoot = "";

		private static string LogRoot
		{
			get
			=> _LogRoot;
			set
			{
				_LogRoot = value;
				CreateDirOnNonExist(_LogRoot);
			}
		}

		private static int _day = int.Parse(DateTime.Now.ToString("yyyyMMdd"));
		private static int day
		{
			get
			{
				if (!posIndexDict.ContainsKey(_day))
				{
					posIndexDict.Add(_day, 0);
					_LogPath = NowLogPath();
				}
				return _day;
			}
			set
			{
				_day = value;

				if (!posIndexDict.ContainsKey(_day))
				{
					posIndexDict.Add(_day, 0);
					_LogPath = NowLogPath();
				}

				if (posIndexDict[_day] == 0)
				{
					var dir = Path.GetDirectoryName(LogPath);
					CreateDirOnNonExist(dir);
					var files = Directory.GetFiles(dir, "BOLog*.log");
					if (files.Any())
					{
						var HasFilesInDay = files.Select(x => x.Split('.')[0])
							.Select(y => y.Split('-').Count() > 1 ? int.Parse(y.Split('-')[1]) : 0).Where(x => x != 0);
						if (HasFilesInDay.Any())
						{
							_onInit = true;
						}
					}
				}
			}
		}

		private static string getLogPostfix() => "-" + posIndexDict[_day];
		private static string _LogPath;

		private static string LogPath
		{
			get
			{
				if (_onInit)
				{
					_LogPath = NowLogPath();
					_onInit = false;
				}

				if (_hasFails)
				{
					_LogPath = NowLogPath();
				}

				if (_LogPath == null)
					_LogPath = NowLogPath();

				return _LogPath;
			}
		}

		private static string NowLogPath() => Path.Combine(LogRoot, $"BOLogs{day}{getLogPostfix()}.log");

		private static void AppendMessageToFreeLog(string msg, LogRowType lrt)
		{
			if (lastMsg == msg) return;
			using (TextWriter tw = TextWriter.Synchronized(File.AppendText(LogPath)))
			{
				tw.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}" + lrt.ToString() + "-" + msg);
				lastMsg = msg;
			}
		}


		private static bool AppendMessagesToFreeLog(List<Tuple<string, DateTime, LogRowType>> msgs)
		{
			bool res = false;
			try
			{
				using (TextWriter tw = TextWriter.Synchronized(File.AppendText(LogPath)))
				{
					tw.WriteLine(string.Join(Environment.NewLine,
										msgs.Select(msg => $"{msg.Item2:yyyy-MM-dd HH:mm:ss.fff} " + msg.Item3.ToString() + "-" + msg.Item1)));
				}
				res = true;
			}
			catch
			{
				// ignored
			}

			return res;
		}



		#region Лист логов для многопоточного добавления с последующим встраиванием в файл лога.
		private static readonly ConcurrentHashSet<Tuple<string, DateTime, LogRowType>> _data = new ConcurrentHashSet<Tuple<string, DateTime, LogRowType>>();

		public static void Append(Exception ex)
		{
			_data.Add(new Tuple<string, DateTime, LogRowType>(ex.Message + "\r\nStackTrace:\r\n" + ex.StackTrace, DateTime.Now, LogRowType.ERROR));
		}

		public static void Append(Exception ex, string StackTrace)
		{
			_data.Add(new Tuple<string, DateTime, LogRowType>(ex.Message + "\r\nStackTrace:\r\n" + ex.StackTrace, DateTime.Now, LogRowType.ERROR));
		}

		public static void Append(string msg, LogRowType lrt = LogRowType.INFO)
		{
			_data.Add(new Tuple<string, DateTime, LogRowType>(msg, DateTime.Now, lrt));
		}
		#endregion


		public static void Write(Exception ex) => Write(ex.Message, LogRowType.ERROR);

		public static void Write(string msg, LogRowType lrt = LogRowType.INFO)
		{
			try
			{
				lock (Lock)
				{
					if (_data.Count > 0)
					{
						var keys = _data.Keys.OrderBy(X => X.Item2).ToList();
						AppendMessagesToFreeLog(keys);
						keys.ForEach(x => _data.Remove(x));
					}
					AppendMessageToFreeLog(msg, lrt);
					lastWriDateTime = DateTime.Now;
				}
			}
			catch (Exception e)
			{
				hasWriteLock();
				try
				{
					lock (Lock)
					{
						AppendMessageToFreeLog(msg, lrt);
					}
				}
				catch
				{
					Console.WriteLine($"Ошибка записи в BOLog {e.Message}");
				}
			}
		}
		#region Запись в не заблокированный файл
		public static void WriteToFile(string FilePath, string value)
		{
			try
			{
				using (StreamWriter sw = new StreamWriter(FilePath, true, Encoding.UTF8))
				{
					sw.WriteLine(value);
				}
			}
			catch (Exception ex)
			{
				Write(ex);
				throw new Exception("Exit");
			}
		}

		public static string ReadFromFile(string FilePath)
		{
			if (File.Exists(FilePath))
				using (StreamReader sw = new StreamReader(FilePath, Encoding.UTF8))
				{
					return sw.ReadToEnd();
				}

			return "";
		}
		#endregion
		#region tests
		public static void Tests()
		{
			DayChangeTest();
			LockTestOnDay();
		}

		private static void DayChangeTest()
		{
			day = int.Parse(DateTime.Now.AddDays(-2).ToString("yyyyMMdd"));
			Write(new Exception("Row 1 for -2 Day BOLog"));
			day = int.Parse(DateTime.Now.AddDays(-1).ToString("yyyyMMdd"));
			Write(new Exception("Row 2 for -1 Day BOLog"));
			day = int.Parse(DateTime.Now.ToString("yyyyMMdd"));
			Write(new Exception("Row 3 for Current Day BOLog"));
		}

		private static void LockTestOnDay()
		{
			Write($"Первое сообщение в лог.");
			//Заблокировали текущий файл лога на запись
			StreamWriter sw = new StreamWriter(LogPath, true, Encoding.UTF8);
			//Проробуем записать данные в заблокированный лог
			Write($"Проверочная строка 1 которая должна будет попасть в файл с новым дневным индексом {getLogPostfix()}");
			sw.Close();
			sw = new StreamWriter(LogPath, true, Encoding.UTF8);
			Write($"Проверочная строка 2 которая должна будет попасть в файл с таким же {getLogPostfix()}");
			sw.Close();
			Write(new Exception($"Проверочная строка 2 которая должна будет попасть в файл с таким же {getLogPostfix()}"));
		}

		#endregion
	}
}
