using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using Shart.Service.Shared;

namespace Ksema.Framework.Utils
{
	public class Export2Txt : IDisposable
	{
		string NormalName(string name)
		{
			name = name.Replace("\n", "").Replace("\r", "").Replace(",", "").Replace(";", "").Replace(":", "");
			if (name.Length > 140)
				name = name.Substring(0, 130);
			return name;
		}
        public Export2Txt(string name, string ext, Encoding en)
        {
            string dirPath = System.IO.Path.GetTempPath() + "\\" + Guid.NewGuid();
            if (!ext.StartsWith("."))
                ext = "." + ext;
            _filePath = dirPath + "\\" + NormalName(name) + "_" + ext;

            Directory.CreateDirectory(dirPath);
            string[] filelist = Directory.GetFiles(dirPath);

            if (filelist.Length != 0)
            _filePath = dirPath + "\\" + NormalName(name) + "__" + ext;
            
            _fs = new StreamWriter(_filePath, true, en);

		}

		//для возможности задать родительскую папку для сохранения временных файлов
		public Export2Txt(string name, string ext, Encoding en, string TargetPath)
		{
			string dirPath = TargetPath + "\\" + Guid.NewGuid();
			if (!ext.StartsWith("."))
				ext = "." + ext;
			_filePath = dirPath + "\\" + NormalName(name) + "_" + ext;

			Directory.CreateDirectory(dirPath);
			string[] filelist = Directory.GetFiles(dirPath);

			if (filelist.Length != 0)
				_filePath = dirPath + "\\" + NormalName(name) + "__" + ext;

			_fs = new StreamWriter(_filePath, true, en);

		}

		public string getFilePath() => _filePath;
		private string _filePath;
		private StreamWriter _fs;

		public static string PrepareCollumn(string s)
		{
			return "\"" + s + "\"";
		}

		public void WriteLine(string line)
		{
			_fs.WriteLine(line);
		}
		public void Write(string line)
		{
			_fs.Write(line);
		}

		public void Dispose()
		{
			if (_fs != null)
			{
				_fs.Dispose();
				try
				{
					if (!string.IsNullOrEmpty(_filePath))
						Process.Start(_filePath);
				}
				catch (Exception ex)
				{
					MessageBox.Show("Ошибка открытия файла " + _filePath + " - " + ex.Message);
				}
			}
		}
	}

	public class Export2CSV : Export2Txt
	{
		public Export2CSV(string name, string ext = ".csv")
			: base(name, ".csv", Encoding.UTF8)
		{

		}
		public Export2CSV(string name, string ext , string TargetPath)
			: base(name, ".csv", Encoding.UTF8, TargetPath)
		{

		}
		public Export2CSV(string name, string TargetPath, Encoding encoding)
				: base(name, ".csv", encoding, TargetPath)
		{

		}
	}
}
