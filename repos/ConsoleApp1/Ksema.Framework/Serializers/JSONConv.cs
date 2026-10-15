using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ksema.Framework.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Formatting = System.Xml.Formatting;

namespace Ksema.Framework.Serializers
{
	public class JSONConv
	{
		static Dictionary<string, JsonSerializerSettings> SettingsSet = new Dictionary<string, JsonSerializerSettings>()
		{
							{ "default",new JsonSerializerSettings
												{
																	NullValueHandling = NullValueHandling.Ignore,
																	MissingMemberHandling = MissingMemberHandling.Ignore,
																	ContractResolver = new CamelCasePropertyNamesContractResolver(),
																	Formatting = (Newtonsoft.Json.Formatting) Formatting.Indented
												}
							},
							{ "NoCamel",new JsonSerializerSettings
												{
																	NullValueHandling = NullValueHandling.Ignore,
																	MissingMemberHandling = MissingMemberHandling.Ignore,
																	Formatting = (Newtonsoft.Json.Formatting) Formatting.Indented
												}
							},
		};

		private readonly JsonSerializerSettings Settings = SettingsSet["default"];

		public T Deserialize<T>(string json)
		{
			return JsonConvert.DeserializeObject<T>(json); ;
		}

		public string Serialize(object obj)
		{
			return JsonConvert.SerializeObject(obj);
		}
		public string SerializePretty(object obj)
		{
			return JsonConvert.SerializeObject(obj, new JsonSerializerSettings

			{
				ContractResolver = new CamelCasePropertyNamesContractResolver(),
				Formatting = (Newtonsoft.Json.Formatting) Formatting.Indented
			});
		}

		public void SerializeObjectToFile<T>(T obj, string filePath)
		{
			try
			{
				using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.UTF8))
				{
					sw.Write(JsonConvert.SerializeObject(obj, SettingsSet["default"]));
				}
			}
			catch (Exception ex)
			{
				Tracer.Warning(ex.Message);
			}
		}

		public void SerializeObjectToFile<T>(T obj, string filePath, string setName)
		{
			try
			{
				using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.UTF8))
				{
					sw.Write(JsonConvert.SerializeObject(obj, SettingsSet[setName]));
				}
			}
			catch (Exception ex)
			{
				Tracer.Warning(ex.Message);
			}
		}

		public T DeserializeObjectFromFile<T>(string PathToJsonFile)
		{
			if (File.Exists(PathToJsonFile))
			{
				try
				{
					using (StreamReader sr = new StreamReader(PathToJsonFile, Encoding.UTF8))
					{
						return JsonConvert.DeserializeObject<T>(sr.ReadToEnd(), Settings);
					}
				}
				catch (Exception ex)
				{
					Tracer.Warning(ex.Message);
				}
			}
			return (T)Activator.CreateInstance(typeof(T));
		}
		public T DeserializeObjectFromFile<T>(string PathToJsonFile, string setName)
		{
			if (File.Exists(PathToJsonFile))
			{
				try
				{
					using (StreamReader sr = new StreamReader(PathToJsonFile, Encoding.UTF8))
					{
						return JsonConvert.DeserializeObject<T>(sr.ReadToEnd(), SettingsSet[setName]);
					}
				}
				catch (Exception ex)
				{
					Tracer.Warning(ex.Message);
				}
			}
			return (T)Activator.CreateInstance(typeof(T));
		}
	}
}
