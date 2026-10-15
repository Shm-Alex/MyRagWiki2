using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Data;
using System.Data.SqlClient;
using Ksema.Framework.Diagnostics;
using System.Diagnostics;

namespace Ksema.Framework.Database
{
	public static class SqlStatisticManager
	{
		private static int _connectionCount;
		private static int _bytesReceived;
		private static int _bytesSent;
		private static int _connectionTime;
		private static int _executionTime;

		public static void Clear()
		{
			_connectionCount = 0;
			_bytesReceived = 0;
			_bytesSent = 0;
			_executionTime = 0;
			_connectionTime = 0;
		}

		[Conditional("DEBUG")]
		public static void Enable(IDbConnection connection)
		{
			if (connection is SqlConnection)
				(connection as SqlConnection).StatisticsEnabled = true;
		}

		[Conditional("DEBUG")]
		public static void ProcessStatistics(IDbConnection connection)
		{
			if (connection is SqlConnection)
				ProcessStatistics((connection as SqlConnection).RetrieveStatistics());
		}

		[Conditional("DEBUG")]
		public static void ProcessStatistics(IDictionary stats)
		{
			++_connectionCount;
			int integerValue;
			object value = stats["BytesReceived"];
			if ((value != null) && (int.TryParse(value.ToString(), out integerValue)))
			{
				_bytesReceived += integerValue;
			}
			value = stats["BytesSent"];
			if ((value != null) && (int.TryParse(value.ToString(), out integerValue)))
			{
				_bytesSent += integerValue;
			}
			value = stats["ConnectionTime"];
			if ((value != null) && (int.TryParse(value.ToString(), out integerValue)))
			{
				_connectionTime += integerValue;
			}
			value = stats["ExecutionTime"];
			if ((value != null) && (int.TryParse(value.ToString(), out integerValue)))
			{
				_executionTime += integerValue;
			}
		}

		[Conditional("DEBUG")]
		public static void LogStatistics()
		{
			var result = new StringBuilder("SqlStatistics:").AppendLine();
			result.AppendFormat(" - connection Count = {0}", _connectionCount).AppendLine();
			result.AppendFormat(" - connection Time(milliseconds) = {0}", _connectionTime).AppendLine();
			result.AppendFormat(" - execution Time(milliseconds) = {0}", _executionTime).AppendLine();
			result.AppendFormat(" - bytes Sent = {0}", _bytesSent).AppendLine();
			result.AppendFormat(" - bytes Received = {0}", _bytesReceived).AppendLine();
			Tracer.Info(result.ToString());
		}
	}
}
