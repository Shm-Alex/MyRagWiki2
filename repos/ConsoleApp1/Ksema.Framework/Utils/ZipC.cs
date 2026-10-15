using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using Ksema.Framework.Diagnostics;

namespace Ksema.Framework.Utils
{
	public static class ZipC
	{
		public static void CompressFile(string SourceFile, string CompressedFile)
		{
			// поток для чтения исходного файла
			using (FileStream sourceStream = new FileStream(SourceFile, FileMode.OpenOrCreate))
			// поток для записи сжатого файла
			using (FileStream targetStream = File.Create(CompressedFile))
				// поток архивации
				using (GZipStream compressionStream = new GZipStream(targetStream, CompressionMode.Compress))
				{
					sourceStream.CopyTo(compressionStream); // копируем байты из одного потока в другой
				}
		}

		private static MemoryStream GenerateStreamFromString(string value)
		{
			return new MemoryStream(Encoding.UTF8.GetBytes(value ?? ""));
		}

		public static void CompressRowToFile(string src, string CompressedFile)
		{
			// поток для чтения исходного файла
			using (Stream sourceStream = GenerateStreamFromString(src))
			{
				// поток для записи сжатого файла
				using (FileStream targetStream = File.Create(CompressedFile))
				{
					// поток архивации
					using (GZipStream compressionStream = new GZipStream(targetStream, CompressionMode.Compress))
					{
						sourceStream.CopyTo(compressionStream); // копируем байты из одного потока в другой
					}
				}
			}
		}

		public static void CompressFileToTergetFile(string src, string CompressedFile)
		{
			// поток для чтения исходного файла
			using (Stream sourceStream = GenerateStreamFromString(src))
			{
				// поток для записи сжатого файла
				using (FileStream targetStream = File.Create(CompressedFile))
				{
					// поток архивации
					using (GZipStream compressionStream = new GZipStream(targetStream, CompressionMode.Compress))
					{
						sourceStream.CopyTo(compressionStream); // копируем байты из одного потока в другой
					}
				}
			}
		}

		public static void Decompress(string compressedFile, string targetFile)
		{
			// поток для чтения из сжатого файла
			using (FileStream sourceStream = new FileStream(compressedFile, FileMode.OpenOrCreate))
				// поток для записи восстановленного файла
				using (FileStream targetStream = File.Create(targetFile))
					// поток разархивации
					using (GZipStream decompressionStream = new GZipStream(sourceStream, CompressionMode.Decompress))
					{
						decompressionStream.CopyTo(targetStream);
						Console.WriteLine($"Восстановлен файл: {targetFile}");
					}
		}


		public static void UnzipFilesToDirectory(string zipFile, string TargetDir, List<string> fileNames = null)
		{
			ZipInputStream zipIn = null;
			FileStream streamWriter = null;
			try
			{
				if (!Directory.Exists(TargetDir))
					Directory.CreateDirectory(TargetDir);

				zipIn = new ZipInputStream(File.OpenRead(zipFile));
				ZipEntry entry;

				while ((entry = zipIn.GetNextEntry()) != null)
				{
					if (entry.IsDirectory)
					{
						string dirPath = Path.Combine(TargetDir, entry.Name);

						if (!Directory.Exists(dirPath))
						{
							Directory.CreateDirectory(dirPath);
						}
					}

					if (!entry.IsDirectory && 
					    (fileNames != null && fileNames.Contains(entry.Name) 
					     || fileNames == null))
					{
						streamWriter = File.Create(Path.Combine(TargetDir, entry.Name));
						int size = 2048;
						byte[] buffer = new byte[size];

						while ((size = zipIn.Read(buffer, 0, buffer.Length)) > 0)
						{
							streamWriter.Write(buffer, 0, size);
						}
					}

					streamWriter?.Close();
				}
			}
			catch (System.Threading.ThreadAbortException lException)
			{
				// do nothing
			}
			catch (Exception ex)
			{
				throw (ex);
			}
			finally
			{
				if (zipIn != null)
				{
					zipIn.Close();
				}

				if (streamWriter != null)
				{
					streamWriter.Close();
				}
			}

		}


		

		
	}
}
