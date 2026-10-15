using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Data;
using System.Data.SqlClient;
using System.Data.OracleClient;
using System.Threading;
using System.Collections;
using System.Diagnostics;

namespace Framework.Diagnostics
{
	public class TextDiagStream : DiagStream
	{
		StreamWriter writer;

		public TextDiagStream() : base()
		{
			writer = null;
		}


		public TextDiagStream(string name) : base()
		{
			try
			{
				resetWriter(name);
			}
			catch
			{
			}
		}

		protected void resetWriter(string name)
		{
			try
			{
				this.Flush();
				this.Close();
			}
			catch
			{
			}
			writer = new StreamWriter(name, true, Encoding.GetEncoding(1251));
		}

		public override void WriteMessage(object message)
		{
			if (writer == null)
				Console.WriteLine(message.ToString());
			else
				writer.WriteLine(message.ToString());
		}

		public override void Flush()
		{
			if (writer != null)
				writer.Flush();
		}

		public override void Close()
		{
			if (writer != null)
			{
				try
				{
					writer.Close();
				}
				catch
				{
				}
			}
		}

	}

	public class ConsoleDiagStream : DiagStream
	{
		public override void WriteMessage(object message)
		{
			Console.WriteLine(message.ToString());
		}
	}

	public class AppTraceStream : TextDiagStream
	{
		string m_catalog = "";
		string m_fileName = "";

		public AppTraceStream(string catalog)
		{
			m_catalog = createRealCatalog(catalog);
			m_fileName = createFileName();
			resetWriter(getFullName());
		}

		private string getFullName()
		{
			return m_catalog + m_fileName;
		}

		private static string createRealCatalog(string catalog)
		{
			//catalog = Framework.ResourceCatalog.PathResourceProvider.ConvertPath(catalog.Trim());
			if (!Directory.Exists(catalog))
				Directory.CreateDirectory(catalog);
			catalog = Path.GetFullPath(catalog);
			if (!catalog.EndsWith(Path.DirectorySeparatorChar.ToString()))
				catalog = catalog + Path.DirectorySeparatorChar;
			return catalog;
		}

		private static string createFileName()
		{
			string file = DateTime.Now.ToString("yyyy-MM-dd") + ".log";
			return file;
		}

		public override void WriteMessage(object message)
		{
			string newFileName = createFileName();
			if(newFileName != m_fileName)
			{
				m_fileName = newFileName;
				resetWriter(getFullName());
			}
			base.WriteMessage(message);
		}

	}

	public class XMLDiagStream : DiagStream
	{
		bool changed;
		string name;
		XmlDocument document = new System.Xml.XmlDocument();

		public XMLDiagStream(string name) : base()
		{
			try
			{
				this.name = name;
				if (File.Exists(name))
				{
					XmlTextReader reader = new XmlTextReader(name);
					document.Load(reader);
					reader.Close();
					changed = false;
				}
				else
				{
					document.AppendChild(document.CreateElement("XMLDiagStream"));
					changed = true;
				}
			}
			catch
			{
			}
		}

		public override void WriteMessage(object message)
		{
			DiagMessage diagMessage = message as DiagMessage;
			XmlElement xmlElement = document.CreateElement(diagMessage.Type);
			document.DocumentElement.AppendChild(xmlElement);
			foreach (DictionaryEntry entry in diagMessage)
			{
				XmlElement element = document.CreateElement(entry.Key as string);
				element.InnerXml = entry.Value as string;
				xmlElement.AppendChild(element);
			}
			changed = true;
		}

		public override void Flush()
		{
			if (changed)
			{
				document.Save(name);
				changed = false;
			}
		}

		public override void Close()
		{
			if (changed)
			{
				document.Save(name);
				changed = false;
			}
		}

	}

	public class EventDiagStream : DiagStream
	{
		EventLog log;
		public EventDiagStream() : this(null)
		{
		}
		public EventDiagStream(string source) : base()
		{
			try
			{
				string name = "Application";
				if (source == null)
					source = name;
				if (!EventLog.SourceExists(source))
					EventLog.CreateEventSource(source, name);
				log = new EventLog();
				log.Source = source;
				log.Log = name;
			}
			catch
			{
			}
		}


		public override void WriteMessage(object message)
		{
			if (message != null)
				log.WriteEntry(message.ToString());
		}
		public void WriteErrorMessage(object message)
		{
			if (message != null)
				log.WriteEntry(message.ToString(), EventLogEntryType.Error);
		}
		public override void Close()
		{
			log.Close();
		}
	}

	public class DBDiagStream : DiagStream
	{
		private IDbConnection connection = null;
		private string type = null;
		private string query = null;
		private string log = null;
		public DBDiagStream(string initData) : base()
		{
			try
			{
				string[] data = initData.Split('|');
				type = data[0].Trim().ToUpper();
				switch (type)
				{
					case "SQL" :
						connection = new SqlConnection(data[1]);
						query = "insert into " + data[2].Trim() + "(uuid,DateTime,Code,Message,AppUser,Host) values(@Uuid,@DateTime,@Code,@Message,@AppUser,@Host)";
						log = data[3].Trim();
						break;
					case "ORA" :
						connection = new OracleConnection(data[1]);
						query = "insert into " + data[2].Trim() + "(uuid,DateTime,Code,Message,AppUser,Host) values(:Uuid,:DateTime,:Code,:Message,:AppUser,:Host)";
						log = data[3].Trim();
						break;
				}
			}
			catch
			{
			}
		}
		private void CreateParam(IDbCommand command, ErrorReportMessage message, string param, int length)
		{
			IDbDataParameter parameter = command.CreateParameter();
			switch (type)
			{
				case "SQL" :
					parameter.ParameterName = "@" + param;
					break;
				case "ORA" :
					parameter.ParameterName = param;
					break;
			}
			string _value = message.GetValue(param);
			if (_value != null && _value != String.Empty)
			{
				length = Math.Min(length - 1, _value.Length);
				if (length != _value.Length)
					_value = _value.Substring(0, length);
			}
			else
				_value = "-";
			parameter.Value = _value;
			command.Parameters.Add(parameter);
		}
		private void DoWriteMessage(object _message)
		{
			lock(this)
			{
				try
				{
					connection.Open();
					IDbCommand command = connection.CreateCommand();
					command.CommandText = query;

					ErrorReportMessage message = (ErrorReportMessage)_message;
					CreateParam(command, message, "Message", 4000);
					CreateParam(command, message, "Code", 20);
					CreateParam(command, message, "DateTime", 20);
					CreateParam(command, message, "AppUser", 20);
					CreateParam(command, message, "Host", 50);
					CreateParam(command, message, "Uuid", 37);

					command.ExecuteNonQuery();
				}
				catch(Exception error)
				{
					EventDiagStream stream = null;
					try
					{
						stream = new EventDiagStream(log);
						stream.WriteMessage(_message);
						stream.WriteErrorMessage(error.ToString());
					}
					catch
					{
					}
					finally
					{
						if (stream != null)
							stream.Close();
					}
				}
				finally
				{
					if (connection.State == ConnectionState.Open)
						connection.Close();
				}
			}
		}
		public override void WriteMessage(object _message)
		{
			if (connection == null || _message == null)
				return;
			//DoWriteMessage(_message);
			ThreadPool.QueueUserWorkItem(new WaitCallback(this.DoWriteMessage), _message);
		}
	}
}
