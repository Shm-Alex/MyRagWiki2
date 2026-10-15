using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using ICSharpCode.SharpZipLib.GZip;

namespace Framework.Compress
{
	public class Compressor
	{
		public static string Compress(string source)
		{
			byte[] byteSource = Serializer.toBytes(source);
			return CompressBytes(byteSource);
		}

		public static string CompressBytes( byte[] byteSource )
		{
			return Convert.ToBase64String( Compress( byteSource ) );
		}

		public static byte[] Compress( byte[] byteSource )
		{
			if( byteSource == null )
				return null;

			using( MemoryStream destinationStream = new MemoryStream( byteSource.Length / 3 + 10 ) )
			{
				using( GZipOutputStream gZipOut = new GZipOutputStream( destinationStream ) )
				{
					gZipOut.Write( byteSource, 0, byteSource.Length );
				}
				return destinationStream.ToArray();
			}
		}

		public static string Decompress(string source)
		{
			byte[] bytes = DecompressBytes(source);
			return Serializer.fromBytes(bytes);
		}
		public static byte[] DecompressBytes(string source)
		{
			if (source == null)
				return null;

			byte[] byteSource = Convert.FromBase64String(source);
			using (MemoryStream sourceStream = new MemoryStream(byteSource))
			{
				return Decompress(sourceStream, byteSource.Length);
			}
		}

		public static byte[] Decompress( byte[] sourceData )
		{
			using( MemoryStream sourceStream = new MemoryStream( sourceData ) )
			{
				return Decompress( sourceStream );
			}
		}

		public static byte[] Decompress(Stream sourceStream)
		{
			return Decompress(sourceStream, (int)sourceStream.Length);
		}
		private static byte[] Decompress(Stream sourceStream, int sourceLength)
		{
			using (MemoryStream destinationStream = new MemoryStream(sourceLength * 3))
			{
				using (GZipInputStream gZipIn = new GZipInputStream(sourceStream))
				{
					int size = 2048;
					byte[] writeData = new byte[size];
					while (size > 0)
					{
						size = gZipIn.Read(writeData, 0, size);
						if (size > 0) 
							destinationStream.Write(writeData, 0, size);
					}
					gZipIn.Close();
				}
				return destinationStream.ToArray();
			}
		}
	}

	class SerializerSurrogate : ISerializationSurrogate
	{
		public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
		{
			if (obj == null)
				return;

			Type objType = obj.GetType();
			MemberInfo[] memberInfos = FormatterServices.GetSerializableMembers(objType, context);
			foreach (var itemInfo in memberInfos)
			{
				if (Attribute.IsDefined(itemInfo, typeof(NonSerializedAttribute)))
					continue;
				if (itemInfo is FieldInfo)
				{
					var fieldInfo = itemInfo as FieldInfo;
					info.AddValue(fieldInfo.Name, fieldInfo.GetValue(obj));
				}
				else if (itemInfo is PropertyInfo)
				{
					var propertyInfo = itemInfo as PropertyInfo;
					if (propertyInfo.CanRead && propertyInfo.GetIndexParameters().Length == 0)
						info.AddValue(propertyInfo.Name, propertyInfo.GetValue(obj, null));
				}
				else
				{
					if (System.Diagnostics.Debugger.IsAttached)
						System.Diagnostics.Debugger.Break();
				}
			}
		}

		public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
		{
			if (obj == null)
				return obj;

			var entries = new Dictionary<string, SerializationEntry>();
			foreach (var item in info)
			{
				entries.Add(item.Name, item);
			}

			Type objType = obj.GetType();
			MemberInfo[] memberInfos = FormatterServices.GetSerializableMembers(objType, context);
			foreach (var itemInfo in memberInfos)
			{
				if (Attribute.IsDefined(itemInfo, typeof(NonSerializedAttribute)))
					continue;
				if (!entries.ContainsKey(itemInfo.Name))
					continue;
				if (itemInfo is FieldInfo)
				{
					var fieldInfo = itemInfo as FieldInfo;
					fieldInfo.SetValue(obj, info.GetValue(fieldInfo.Name, fieldInfo.FieldType));
				}
				else if (itemInfo is PropertyInfo)
				{
					var propertyInfo = itemInfo as PropertyInfo;
					if (propertyInfo.CanWrite && propertyInfo.GetIndexParameters().Length == 0)
						propertyInfo.SetValue(obj, info.GetValue(propertyInfo.Name, propertyInfo.PropertyType), null);
				}
				else
				{
					if (System.Diagnostics.Debugger.IsAttached)
						System.Diagnostics.Debugger.Break();
				}
			}

			return obj;
		}
	}

	class SerializationSelector : ISurrogateSelector
	{
		private static Type exceptionType;
		private static Type[] types;
		private static BindingFlags flags;
		static SerializationSelector()
		{
			exceptionType = typeof(Exception);
			types = new Type[2] { typeof(SerializationInfo), typeof(StreamingContext) };
			flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance;
		}

		private ISurrogateSelector _next;

		public void ChainSelector(ISurrogateSelector selector)
		{
			if (_next == null)
				_next = selector;
			else
				_next.ChainSelector(selector);
		}

		public ISurrogateSelector GetNextSelector()
		{
			return _next;
		}

		public ISerializationSurrogate GetSurrogate(Type type, StreamingContext context, out ISurrogateSelector selector)
		{
			if (exceptionType != type && exceptionType.IsAssignableFrom(type) && type.GetConstructor(flags, null, types, null) == null)
			{
				selector = this;
				return new SerializerSurrogate();
			}

			if (_next != null)
				return _next.GetSurrogate(type, context, out selector);

			selector = null;
			return null;
		}
	}

	public class Serializer
	{
		public static object deserializeObject(byte[] data)
		{
			using (MemoryStream stream = new MemoryStream(data))
			{
				IFormatter fmt = new BinaryFormatter();
				fmt.SurrogateSelector = new SerializationSelector();
				object res = fmt.Deserialize(stream);
				stream.Close();
				return res;
			}
		}
		public static object deserializeFromString(string data)
		{
			return deserializeObject(Convert.FromBase64String(data));
		}
		public static byte[] serializeObject(object obj)
		{
			using (MemoryStream stream = new MemoryStream())
			{
				IFormatter fmt = new BinaryFormatter();
				fmt.SurrogateSelector = new SerializationSelector();
				fmt.Serialize(stream, obj);
				stream.Close();
				return stream.ToArray();
			}
		}	
		public static string serializeToString(object obj)
		{
			return Convert.ToBase64String(serializeObject(obj));
		}


		private const int __codePage = 1251;
		public static byte[] toBytes(string source)
		{
			Encoding encoding = Encoding.GetEncoding(__codePage);
			byte[] destination = source != null ? encoding.GetBytes(source) : null;
			return destination;
		}
		public static string fromBytes(byte[] source)
		{
			Encoding encoding = Encoding.GetEncoding(__codePage);
			string destination = source != null ? encoding.GetString(source) : null;
			return destination;
		}
	}
}
