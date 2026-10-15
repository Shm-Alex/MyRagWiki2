using System;
using System.Xml.Serialization;
using System.Xml;

namespace Ksema.Framework
{
	[XmlRoot]
	public class SnippetCompilerProject
	{
		public SnippetCompilerProject()
		{
			CompilerVersion = SnippetCompilerController.DefaultCompilerVersion;
			if (String.IsNullOrWhiteSpace(CompilerVersion))
				CompilerVersion = "v4.0";
		}

		[XmlIgnore]
		public string Code { get; set; }

		[XmlElement("Code")]
		public XmlCDataSection CodeData
		{
			get
			{
				XmlDocument doc = new XmlDocument();
				return doc.CreateCDataSection(Code);
			}
			set 
			{
				Code = value.Value;
			}
		}

		[XmlArray]
		[XmlArrayItem(ElementName="Assembly")]
		public string[] ReferencedAssemblies { get; set; }

		[XmlAttribute]
		public string CompilerVersion { get; set; }
	}
}
