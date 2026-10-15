using System;
using System.Collections;
using System.Text;
using System.Threading;
using Framework.Collections;

namespace Framework.Diagnostics
{
	/// <summary>
	/// Провайдеры доступа к БД
	/// </summary>
	public enum ProviderKind
	{
		OleDB,
		ODBC,
		ORACLE,
		SQLServer
	}
	/// <summary>
	/// Диагностика операций с БД
	/// </summary>
	public class DBDiagMessage : DiagMessage
	{
		public const string __name = "DBDiagMessage";
		/// <summary>
		/// Диагностика операция с БД
		/// </summary>
		/// <param name="provider">Провайдер</param>
		/// <param name="login">Логин</param>
		/// <param name="reason">???</param>
		/// <param name="database">Имя БД</param>
		public DBDiagMessage(ProviderKind provider, string login, string reason, string database) : base(__name)
		{
			Init(provider, login, reason, database, null, null);
		}

		/// <summary>
		/// Диагностика операций с БД
		/// </summary>
		/// <param name="provider">Провайдер</param>
		/// <param name="login">Логин</param>
		/// <param name="reason">???</param>
		/// <param name="database">Имя БД</param>
		/// <param name="dataSource">Источник данных (ODBC)</param>
		public DBDiagMessage(ProviderKind provider, string login, string reason, string database, string dataSource) : base(__name)
		{
			Init(provider, login, reason, database, dataSource, null);
		}

		/// <summary>
		/// Диагностика операций с БД
		/// </summary>
		/// <param name="provider">Провайдер</param>
		/// <param name="login">Логин</param>
		/// <param name="reason">???</param>
		/// <param name="database">Имя БД</param>
		/// <param name="dataSource">Источник данных (ODBC)</param>
		/// <param name="driver">Драйвер (ODBC)</param>
		public DBDiagMessage(ProviderKind provider, string login, string reason, string database, string dataSource, string driver) : base(__name)
		{
			Init(provider, login, reason, database, dataSource, driver);
		}

		private void Init(ProviderKind provider, string login, string reason, string database, string dataSource, string driver)
		{
			SetValue("Provider", provider.ToString());
			SetValue("Login", login);
			SetValue("Reason", reason);
			SetValue("Database", database);
			SetValue("DataSource", dataSource);
			SetValue("Driver", driver);
		}


		/// <summary>
		/// Возвращает тип провайдера
		/// </summary>
		public ProviderKind Provider
		{
			get
			{
				return (ProviderKind) Enum.Parse(typeof(ProviderKind), GetValue("Provider"));
			}
			set
			{
				SetValue("Provider", value.ToString());
			}
		}

		/// <summary>
		/// Возвращает логин
		/// </summary>
		public string Login
		{
			get
			{
				return GetValue("Login");
			}
			set
			{
				SetValue("Login", value);
			}
		}

		/// <summary>
		/// Возвращает ???
		/// </summary>
		public string Reason
		{
			get
			{
				return GetValue("Reason");
			}
			set
			{
				SetValue("Reason", value);
			}
		}

		/// <summary>
		/// Возвращает имя БД
		/// </summary>
		public string Database
		{
			get
			{
				return GetValue("Database");
			}
			set
			{
				SetValue("Database", value);
			}
		}

		/// <summary>
		/// Возвращает источник данных (ODBC)
		/// </summary>
		public string DataSource
		{
			get
			{
				return GetValue("DataSource");
			}
			set
			{
				SetValue("DataSource", value);
			}
		}

		/// <summary>
		/// Возвращает драйвер (ODBC)
		/// </summary>
		public string Driver
		{
			get
			{
				return GetValue("Driver");
			}
			set
			{
				SetValue("Driver", value);
			}
		}


		/// <summary>
		/// Проверка правильности загружаемых из конфигурационного файла описаний атрибутов
		/// </summary>
		/// <param name="attList">Список атрибутов</param>
		protected override void Validate(ICollection attList)
		{
			foreach (string name in attList)
				switch (name)
				{
					case "Login" :
					case "Provider" :
					case "Reason" :
					case "Database" :
					case "DataSource" :
					case "Driver" :
						break;
					default :
						throw new Exception("Сообщение DBDiagMessage не содержит поле " + name + "\nВозможно, ошибка в конфигурационном файле");
				}
		}

	}
	public class TimeTraceMessage : DiagMessage
	{
		public const string __name = "TimeTraceMessage";

		protected TimeTraceMessage(string msg, DateTime time, string typeName) : base(typeName)
		{
			init(msg, time);
		}

		public TimeTraceMessage(string msg, DateTime time) : base(__name)
		{
			init(msg, time);
		}
		public TimeTraceMessage(string msg) : base(__name)
		{
			init(msg, DateTime.Now);
		}
		void init(string msg, DateTime time)
		{
			this.SetValue("Message", msg);
//			this.SetValue("Time", time.ToShortDateString() + " # " + time.ToLongTimeString() + "." + time.Millisecond.ToString());
//			this.SetValue("Time", time.ToString("yyyy.MM.dd HH:mm:ss.fff") );
			this.SetValue("Time", time.ToString("HH:mm:ss.fff") );
		}

		public override string ToString()
		{
			string result = String.Format("[{0}][0x{1}] {2}", GetValue("Time"), Thread.CurrentThread.GetHashCode().ToString("x"),  GetValue("Message") );
			return result;
		}
	}
	public class SimpleMessage : DiagMessage
	{
		public const string __name = "SimpleMessage";
		public SimpleMessage(string msg) : base(__name)
		{
			SetValue("Message", msg);
		}
	}
	public class ErrorReportMessage : DiagMessage
	{
		public const string __name = "ErrorReportMessage";
		public ErrorReportMessage(Exception error, string user, string host) : base(__name)
		{
			Init(error, user, host);
		}
		protected ErrorReportMessage() : base(__name)
		{
		}
		private string GetErrorString(Exception error)
		{
			string text = error.GetType().FullName + ": " + error.Message;
			text += Environment.NewLine + error.StackTrace;
			return text;
		}
		protected void Init(Exception error, string user, string host)
		{
//			SetValue("AppUser", user);
//			SetValue("Host", host);
			SetValue("DateTime", DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss.fff"));
			string text = error.ToString();
//			SetValue("Code", text.GetHashCode().ToString());
			SetValue("Message", text);
//			SetValue("Uuid", Guid.NewGuid().ToString());
		}

		public override string ToString()
		{
			string result = String.Format("ERROR[{0}][0x{1}] {2}", GetValue("DateTime"), Thread.CurrentThread.GetHashCode().ToString("x"), GetValue("Message") );
			return result;
		}
	}
	public class AppTraceMessage : DiagMessage
	{
		public const string __name = "AppTraceMessage";
		public AppTraceMessage(string uri, string user, string instance, DateTime timeStamp, string type, string action) : base(__name)
		{
			SetValue("Uri", uri);
			SetValue("User", user);
			SetValue("Instance", instance);
			SetValue("DateTime", timeStamp.ToString("yyyy-MM-dd HH:mm:ss"));
			SetValue("Type", type);
			SetValue("Action", action);
		}


		public override string ToString()
		{
			string delimiter = " ";
			StringBuilder text = new StringBuilder();
			text.Append(GetValue("DateTime")).Append(delimiter);
			text.Append(GetValue("User")).Append(delimiter);
			text.Append(GetValue("Instance")).Append(delimiter);
			text.Append(GetValue("Uri")).Append(delimiter);
			text.Append(GetValue("Type")).Append(delimiter);
			text.Append(GetValue("Action"));
			return text.ToString();
		}
	}
}
