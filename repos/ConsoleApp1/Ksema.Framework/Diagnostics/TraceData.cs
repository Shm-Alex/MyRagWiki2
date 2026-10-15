using System;
using System.Collections.Generic;
using System.Text;

namespace Ksema.Framework.Diagnostics
{
	public class TraceData
	{
		//readonly Dictionary<string, string> _items = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		LinkedList<KeyValuePair<string, string>> _items = new LinkedList<KeyValuePair<string, string>>();

		public TraceData()
		{
		}

		public TraceData(string message)
		{
			if ( message.Length > 20000)
				Message = message.Substring(0,19000);
			else
				Message = message;
		}

		public TraceData(string format, params object[] args)
		{
			StringBuilder sb = new StringBuilder();

			if (string.IsNullOrEmpty(format))
				AppendArgs(args, sb);
			else if (args != null && args.Length > 0)
				sb.AppendFormat(format, args);
			else
				sb.Append(format);

			if ( sb.Length > 20000)
				Message = sb.ToString().Substring(0,19000);
			else
				Message = sb.ToString();
		}

		public int ID { get; set; }
		public string Message { get; set; }

		public void AddProperty(string name, string value)
		{
			_items.AddLast(new KeyValuePair<string, string>(name, value));
		}

		public ICollection<KeyValuePair<string, string>> Items
		{
			get { return _items; }
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine();

			foreach (KeyValuePair<string, string> entry in Items)
			{
				sb.Append(entry.Key).Append(": ").Append(entry.Value).AppendLine();
			}

			if (!string.IsNullOrEmpty(Message))
				sb.Append(Message).AppendLine();

			return sb.ToString();
		}

		private static void AppendArgs(object[] args, StringBuilder sb)
		{
			if (args == null || args.Length == 0)
				return;

			for (int i = 0; i < args.Length; i++)
			{
				if (i != 0)
					sb.Append(',');
				if (args[i] != null)
					sb.Append(args[i].ToString());
			}
		}
	}
}