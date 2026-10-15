using System;
using System.IO;

namespace Ksema.Framework.Diagnostics
{
	public static class LogFileTasks
	{
		public const int MAX_USED_FILES = 12;
		public static void PrepareFileForLog(string fileName, int tolerantSize)
		{
			if (File.Exists(fileName))
			{
				FileInfo fInfo = new FileInfo(fileName);

				if (fInfo.Length > tolerantSize)
				{
					string tailed = Tail(fileName, tolerantSize);
					using (FileStream fs = File.Open(fileName, FileMode.Create))
					{
						using (StreamWriter sw = new StreamWriter(fs))
						{
							sw.WriteLine(tailed);
						}
					}
				}
			}
		}

		public static string GetLastUnusedFile(string fileName, int count, string firstPart)
		{
			try
			{
				using (var fs = new FileStream(fileName, FileMode.Append, FileAccess.Write))
				{
					fs.Close();
				}

				GC.WaitForPendingFinalizers();
				return fileName;
			}
			catch
			{
				string newFileName = null;
				if (count == MAX_USED_FILES)
					throw;
				if (count == 0)
				{
					newFileName = Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName) + "." + count.ToString("D3") + Path.GetExtension(fileName));
					return GetLastUnusedFile( newFileName, count + 1, Path.GetFileNameWithoutExtension(fileName));
				}

				newFileName = Path.Combine(Path.GetDirectoryName(fileName), firstPart + "." +  count.ToString("D3") +  Path.GetExtension(fileName));
				return GetLastUnusedFile(newFileName, count + 1, firstPart);

			}
		}

	    public static string Tail(string filename, int bytes)
	    {
	        using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
	        {
	            if (fs.Length < bytes)
	            {
	                bytes = (int)fs.Length;
	            }

	            byte[] buffer = new byte[bytes];
	            fs.Seek(-bytes, SeekOrigin.End);
	            fs.Read(buffer, 0, buffer.Length);

	            return System.Text.Encoding.UTF8.GetString(buffer);
	        }
	    }

        //public static string Tail(string filename, int bytes)
        //{
        //    using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
        //    {
        //        char[] buffer = (char[])Array.CreateInstance(typeof(char), bytes);
        //        string txt;
        //        using (StreamReader sr = new StreamReader(fs))
        //        {
        //            sr.BaseStream.Seek((-1 * bytes), SeekOrigin.End);
        //            sr.ReadBlock(buffer, 0, buffer.Length);
        //            txt = new string(buffer);
        //            sr.Close();
        //            fs.Close();
        //        }
        //        return txt;
        //    }
        //}
    }
}
