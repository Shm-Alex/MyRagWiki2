using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using Framework.Compress;

namespace Framework.Utils
{
	public class TypeUtils
	{
        public static T CreateInstance<T>(string className)
        {
            Type t = Type.GetType(className, true, true);
            return (T)Activator.CreateInstance(t);
        }

        public static T CreateInstance<T>(string className, params object[] prms)
        {
            Type t = Type.GetType(className, true, true);
            return (T)Activator.CreateInstance(t, prms);
        }

	    public static MethodInfo GetInheritedMethod(Type type, string name)
		{
			return GetInheritedMethod(type, name, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
		}
		public static MethodInfo GetInheritedMethod(Type type, string name, BindingFlags flags)
		{
			return GetInheritedMethod(type, name, flags, null);
		}
		public static MethodInfo GetInheritedMethod(Type type, string name, BindingFlags flags, Type[] types)
		{
			bool isEmpty = types == null;
			MethodInfo method = isEmpty ? type.GetMethod(name, flags) : type.GetMethod(name, flags, null, types, null);
			if (method == null)
			{
				do
				{
					foreach (Type innerType in type.GetInterfaces())
					{
						method = isEmpty ? innerType.GetMethod(name, flags) : innerType.GetMethod(name, flags, null, types, null);
						if (method != null)
							break;
					}
				}
				while ((type = type.BaseType) != null);
			}
			return method;
		}
		public static MethodInfo GetInheritedMethodEx(Type type, string name, BindingFlags flags, Type[] types)
		{
			MethodInfo method = null;
			if ((flags & BindingFlags.GetProperty) != 0)
				method = GetInheritedMethod(type, "get_" + name, flags, types);
			if (method == null)
				method = GetInheritedMethod(type, name, flags, types);
			return method;
		}

        public static string GetAssemblyVersion(Assembly asm)
        {
            return asm.GetName().Version.ToString();
        }


		public static object GetDefaultValue(Type t)
		{
			Type thisType = typeof(TypeUtils);
			MethodInfo mi = thisType.GetMethod("GetDefaultValueImpl", BindingFlags.NonPublic | BindingFlags.Static);
			mi = mi.MakeGenericMethod(t);
			object result = mi.Invoke(null, null);
			return result;
		}

		static object GetDefaultValueImpl<T>()
		{
			return default(T);			
		}
	}

	public static class TypeNameHelper
	{
		private const char genericSpecialChar = '`';
		private const string genericSeparator = ", ";

		static string GetCleanName(this Type t)
		{
			string name = t.Name;
			if (t.IsGenericType)
			{
				name = name.Remove(name.IndexOf(genericSpecialChar));
			}
			return name;
		}

		public static string GetCodeDefinition(this Type t)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendFormat("{0}.{1}", t.Namespace, t.GetCleanName());
			if (t.IsGenericType)
			{
				var names = from ga in t.GetGenericArguments()
							select GetCodeDefinition(ga);
				sb.Append("<");
				sb.Append(string.Join(genericSeparator, names.ToArray()));
				sb.Append(">");
			}
			return sb.ToString();
		}
	}

	public class MetaUtils
	{
		private MetaUtils()
		{
		}
		interface IMetaLoader
		{
			bool loadFromResources(string path, bool archive);
			bool loadFromFile(string path, bool archive);

			string ErrorMessage{get;}
		}
		class StreamLoader : IMetaLoader
		{
			private Stream _stream;
			public Stream Stream
			{
				get
				{
					return _stream;
				}
			}


			public bool loadFromResources(string path, bool archive)
			{
				_stream = AppUtils.GetResourceStream(path);
				if (archive)
					_stream = DecompressToStream(_stream);
				return true;
			}
			public bool loadFromFile(string path, bool archive)
			{
				_stream = new FileStream(path, FileMode.Open, FileAccess.Read);
				if (archive)
					_stream = DecompressToStream(_stream);
				return true;
			}


			public string ErrorMessage
			{
				get
				{
					throw new NotSupportedException();
				}
			}
		}



		public static XmlDocument LoadSystemXml(string connection)
		{
			XmlDocument document = new XmlDocument();
			LoadSystemXml(connection, document);
			return document;
		}
		public static void LoadSystemXml(string connection, XmlDocument document)
		{
			document.Load(CreateStream(connection));
		}
		public static Stream CreateStream(string connection)
		{
			StreamLoader loader = new StreamLoader();
			PerformLoading(connection, loader);
			return loader.Stream;
		}



		private static void PerformLoading(string connection, IMetaLoader loader)
		{
			bool archive;
			string arc = Utils.getKeyValue(connection, "arc", false);
			archive = Utils.isEmpty(arc) ? true : Boolean.Parse(arc);

			bool loaded;
			string path = Utils.getKeyValue(connection, "emb", false);
			if (!Utils.isEmpty(path))
			{
				loaded = loader.loadFromResources(path, archive);
			}
			else
			{
				path = Utils.getKeyValue(connection, "file", false);
				if (Utils.isEmpty(path))
				{
					path = Utils.getKeyValue(connection, "res", true);
					//path = Catalog.Instance.RequestResource(path) as string;
				}
				loaded = loader.loadFromFile(ConvertPath(path), archive);
			}
			if (!loaded)
				throw new ArgumentException("Ошибка загрузки файла модели '" + path + "':\r\n" + loader.ErrorMessage);
		}

		public static string ConvertPath(string helpstring)
		{
			int index = helpstring.IndexOf("$");			
			while (index >= 0)
			{
				if (helpstring[index + 1] == '(')
				{
					int ket = helpstring.IndexOf(")", index);
					if (ket == -1)
						throw new ArgumentException("Закрывающая скобка не найдена");
					string function = helpstring.Substring(index + 2, ket - index - 2);
					string path = GetPath(function);
					helpstring = helpstring.Remove(index, ket - index + 1).Insert(index, path);
					index += path.Length - 1;
				}
				index = helpstring.IndexOf("$", index + 1);
			}
			return helpstring;
		}

		private static string GetPath(string alias)
		{
			switch (alias.ToLower().Trim())
			{
				case "current" :
					return Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
			}
			Environment.SpecialFolder specialFolder = (Environment.SpecialFolder)Enum.Parse(typeof(Environment.SpecialFolder), alias, true);
			return Environment.GetFolderPath(specialFolder);
		}

		private static string DecompressToString(Stream source)
		{
			byte[] bytes = Compressor.Decompress(source);
			return Serializer.fromBytes(bytes);
		}
		private static Stream DecompressToStream(Stream source)
		{
			byte[] bytes = Compressor.Decompress(source);
			return new MemoryStream(bytes);
		}
	}

}
