using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using ICSharpCode.SharpZipLib.GZip;
using Ksema.Framework.Diagnostics;

namespace Ksema.Framework.Caching
{
	public class FileCache<T> where T : class
	{
		public static T GetValue(string localFileName, DateTime dateTime, Func<T> initialize)
		{
			/*
			return initialize();
			/*/
			var ass = Assembly.GetCallingAssembly();
			string fileName = Path.Combine(Path.GetDirectoryName(ass.Location), localFileName);

			if (IsFileValid(fileName, dateTime))
			{
				try
				{
					return LoadFromFile(fileName);
				}
				catch (Exception error)
				{
					Tracer.Error(error);
				}
			}

			T data = initialize();
			try
			{
				SaveToFile(fileName, data);
			}
			catch (Exception error)
			{
				Tracer.Error(error);
			}
			return data;
			//*/
		}

		private static bool IsFileValid(string fileName, DateTime dateTime)
		{
			var fileInfo = new FileInfo(fileName);
			return fileInfo.Exists && fileInfo.LastWriteTime > dateTime;
		}

		private static void SaveToFile(string fileName, T data)
		{
			using (var file = File.Open(fileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
			{
				using (GZipOutputStream gZipOut = new GZipOutputStream(file))
				{
					new BinaryFormatter().Serialize(gZipOut, data);
				}
			}
		}

		private static T LoadFromFile(string fileName)
		{
			using (var file = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				using (GZipInputStream gZipIn = new GZipInputStream(file))
				{
					return new BinaryFormatter().Deserialize(gZipIn) as T;
				}
			}
		}
	}
}
