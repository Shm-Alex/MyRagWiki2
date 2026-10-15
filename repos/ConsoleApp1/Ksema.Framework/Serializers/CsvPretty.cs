using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ksema.Framework
{
	public class CsvPretty : IDisposable
	{
		public List<string[]> OutCsvRows { get; set; }
		private string[] headers;
		private int[] Padding { get; set; }

		public CsvPretty(params string[] values)
		{
			OutCsvRows = new List<string[]>();
			headers = values;
			Padding = new int[headers.Length];
			OutCsvRows.Add(headers);
		}

		public void AppendMessage(string[] row, out bool hasFail)
		{
			OutCsvRows.Add(row);
			hasFail = true;
		}

		public void AppendMessage(params string[] values)
		{
			OutCsvRows.Add(values);
		}

		public void Pretty()
		{
			Padding = OutCsvRows.SelectMany(arr => arr.Select((x, index) => new KeyValuePair<int, int>(index, x.Length))).GroupBy(x => x.Key)
					.OrderBy(x => x.Key).Select(t => (int)t.Max(a => a.Value)).ToArray();
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine(string.Join(";", OutCsvRows[0].Select((col, index) => col.PadRight(Padding[index], ' '))));
			OutCsvRows.RemoveAt(0);
			foreach (var row in OutCsvRows.OrderBy(x=>x[0]))
			{
				sb.AppendLine(string.Join(";", row.Select((col, index) => col.PadRight(Padding[index], ' '))));
			}
			return sb.ToString();
		}

		public void Dispose()
		{
			
		}

		public string Test()
		{
			var csvp = new CsvPretty("ИД", "Фамилия", "Имя", "Отчество", "Email");

			csvp.AppendMessage("1000001", "Аваров","Брикет","Гургенович","gurgen@ami.ru");
			csvp.AppendMessage("1000002", "Простоквашинов", "Геннадий", "Петрович", "pgp@ami.ru");
			csvp.AppendMessage("1000003", "Пластеев", "Данил", "Александрович", "pda@ami.ru");
			csvp.Pretty();
			return csvp.ToString();
		}
	}
}
