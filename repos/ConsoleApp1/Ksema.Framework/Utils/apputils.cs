using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using Microsoft.Win32;
using System.Text;
using Ksema.Framework.Diagnostics;

namespace Framework.Utils
{
	public class WinUtils
	{
		public static string FormatPath(string path)
		{
			if (Utils.isEmpty(path))
				return null;
			if (path.IndexOf("=") == -1)
				path = "emb=" + path + ";arc=False;";
			return path;
		}
	}

	public class AppUtils
	{
		public static bool UseLocalCatalog
		{
			get
			{
				string reg_path = ConfigurationSettings.AppSettings["AppRegPath"];
				if(reg_path == null || reg_path.Length == 0)
					reg_path = @"SOFTWARE\Ksema";
				RegistryKey key = Registry.LocalMachine.OpenSubKey(reg_path);
				if(key == null)
					return true;
				object v = key.GetValue("UseLocalCatalog");
				if(v == null)
					return true;
				return ((int)v == 0) ? false : true;
			}
		}
		public static string SplitResourceQualifiedName(string nsQualifiedResName, out Assembly assembly)
		{
			assembly = null;
			string resName = nsQualifiedResName;

			int index = nsQualifiedResName.IndexOf(",");
			if (index == -1)
			{
				try
				{
					foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
					{
						if (asm.IsDynamic)
							continue;
						ManifestResourceInfo manifestResourceInfo = asm.GetManifestResourceInfo(nsQualifiedResName);
						if (manifestResourceInfo != null)
						{
							assembly = asm;
							break;
						}
					}
				}
				catch (Exception error)
				{
					string errorText = TraceTypeLoadError(error);
					Tracer.Error(errorText);
					throw new Exception(errorText, error);
				}
				if(assembly == null)
					throw new ArgumentException("Resource '" + nsQualifiedResName + "' not found");
			}
			else
			{
				resName = nsQualifiedResName.Substring(0, index).Trim();
				string assemblyName = nsQualifiedResName.Substring(index + 1).Trim();
				assembly = Assembly.Load(assemblyName);
				if (assembly == null)
					throw new ArgumentException("Assembly '" + assemblyName + "' not found");
			}

			return resName;
		}

		public static string TraceTypeLoadError(Exception exc)
		{
			StringBuilder builder = new StringBuilder();
			TraceTypeLoadError(exc, builder);
			return builder.ToString();

			//Trace.WriteLine("Loaded assemblies:");
			//try
			//{
			//    foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
			//    {
			//        Trace.WriteLine(a.ToString() + " " + a.CodeBase);
			//        try
			//        {
			//            foreach (var t in a.GetTypes())
			//                Trace.WriteLine(" === " + t.FullName);
			//        }
			//        catch (Exception eType)
			//        {
			//            Trace.WriteLine("Error processing type list: " + eType.Message);
			//        }
			//    }
			//}
			//catch (Exception eAsm)
			//{
			//    Trace.WriteLine("Error processing assembly list: " + eAsm.Message);
			//}
		}
		public static void TraceTypeLoadError(Exception exc, StringBuilder str)
		{
			str.AppendLine(exc.Message);
			str.AppendLine(exc.ToString());
			if (exc is TypeLoadException)
				str.AppendLine(((TypeLoadException)exc).TypeName);
			if (exc is ReflectionTypeLoadException)
			{
				foreach (Exception innerError in (exc as ReflectionTypeLoadException).LoaderExceptions)
				{
					TraceTypeLoadError(innerError, str);
				}
			}
		}

		public static Stream GetResourceStream(string namespaceQualifiedResourceFileName)
		{
			Assembly assembly;
			string shortResName = SplitResourceQualifiedName(namespaceQualifiedResourceFileName, out assembly);

			Stream stream = assembly.GetManifestResourceStream(shortResName);
			return stream;
		}
		public static string GetResourceText(string namespaceQualifiedResourceFileName)
		{
			Stream stream = GetResourceStream(namespaceQualifiedResourceFileName);
			using (StreamReader reader = new StreamReader(stream))
			{
				return reader.ReadToEnd();
			}
		}
		public static byte[] GetResourceBytes(string namespaceQualifiedResourceFileName)
		{
			Stream source = GetResourceStream(namespaceQualifiedResourceFileName);
			return getBytes(source);
		}
		public static byte[] GetResourceBytes(Assembly assembly, string resourceName)
		{
			if (assembly == null)
				throw new ArgumentNullException("assembly");
			if (Utils.isEmpty(resourceName))
				throw new ArgumentNullException("resourceName");
			Stream source = assembly.GetManifestResourceStream(resourceName);
			if (source == null)
				throw new ArgumentException("Resource " + resourceName + " not found in assembly " + assembly.FullName);
			return getBytes(source);
		}
		private static byte[] getBytes(Stream source)
		{
			int length = (int)source.Length;
			byte[] buffer = new byte[length];
			source.Read(buffer, 0, length);
			return buffer;
		}
	}

    public static class IOUtils
    {
        public static string RootFileName(string aFileName)
        {
            if (Path.IsPathRooted(aFileName))
                return aFileName;
            else
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, aFileName);
        }

    }


}