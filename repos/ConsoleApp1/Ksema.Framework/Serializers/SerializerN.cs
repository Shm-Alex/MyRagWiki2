using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Ksema.Framework.Serializers
{
	public static class SerializerN<T> where T : class
	{
		public static string SerializeWithElementsRemove(T obj, List<string> rmlist, bool removeNameSpaces = true)
		{
			StringReader strR = new StringReader(Serialize(obj, false));
			XmlDocument doc = new XmlDocument();
			doc.Load(strR);
			foreach (var rmPath in rmlist)
			{
				var SelectedNodes = doc.SelectNodes(rmPath); // apply your xpath here
				if (SelectedNodes == null) continue;

				foreach (XmlNode node in SelectedNodes)
				{
					node?.ParentNode?.RemoveChild(node);
				}
			}
			return removeNameSpaces ? RmNameSpacesFromXmlRow(doc.OuterXml) : doc.OuterXml;
		}

		public static string Serialize(T obj, bool removeNameSpaces = true)
		{
			XmlSerializer xsSubmit = new XmlSerializer(typeof(T));
			using (var sww = new StringWriter())
			{
				using (XmlTextWriter writer = new XmlTextWriter(sww)
				{
						Formatting = Formatting.Indented
				})
				{
					xsSubmit.Serialize(writer, obj);
					return removeNameSpaces ? RmNameSpacesFromXmlRow(sww.ToString()) : sww.ToString();
					
				}
			}
		}

		private static string RmNameSpacesFromXmlRow(string row)
		{
			return row.Replace(
							" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"", "")
					.Replace("<?xml version=\"1.0\" encoding=\"utf-16\"?>" + Environment.NewLine, "").Replace("<?xml version=\"1.0\" encoding=\"utf-16\"?>","");
		}


		public static T Deserialize(string xml)
		{
			T obj = Activator.CreateInstance<T>();

			if (string.IsNullOrEmpty(xml)) return obj;
			try
			{
				XmlSerializer xsSubmit = new XmlSerializer(typeof(T));
				using (var sr = new StringReader(xml))
				{
					using (XmlTextReader xr = new XmlTextReader(sr){Namespaces = false})
					{
						obj = (T) xsSubmit.Deserialize(xr);
						return obj;
					}
				}
			}
			catch
			{
				obj = Activator.CreateInstance<T>();
			}

			return obj;
		}
	}
}