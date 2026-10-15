using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace Ksema.Framework.Utils
{
	public class XsltMng
	{

		public static string XmlToPlainText(string xml)
		{
			string output;
			try
			{
				var stream = new MemoryStream();
				XDocument xBook2 = XDocument.Load(new StringReader(xml));
				XslCompiledTransform transform = new XslCompiledTransform();
				transform.Load(typeof(XmlToPlainText));
				using (XmlWriter transformedDoc = XmlWriter.Create(stream))
				{
					transformedDoc.WriteStartDocument();
					transformedDoc.WriteStartElement("Out");
					transform.Transform(xBook2.CreateReader(), transformedDoc);
					transformedDoc.WriteEndElement();
					transformedDoc.Flush();
				}
				stream.Position = 0;
				using (StreamReader sr = new StreamReader(stream))
				{
					output = sr.ReadToEnd().Replace("<?xml version=\"1.0\" encoding=\"utf-8\"?><Out>\r\n","").Replace("</Out>", "");
				}
			}
			catch
			{
				return "";
			}

			return xml.Contains("<?xml ") ? output : xml;
		}

	}
}
