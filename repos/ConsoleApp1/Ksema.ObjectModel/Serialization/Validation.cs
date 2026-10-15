using System;
using System.Collections;
using System.Text;
using System.IO;
using System.Xml;
using Ksema.ObjectModel;
using System.Collections.Generic;

namespace Serializer
{
	public class Validation
	{
		public Validation()
		{
		}
		/// <summary>
		/// —оздать XSD-схему
		/// в xml-документ
		/// </summary>
		/// <param name="meta">ћетаданные</param>
		/// <returns>Xml-документ</returns>
		public XmlDocument CreateXSD(MetaData meta)
		{

			return new XSDCreator(meta).Create();
		}
		/// <summary>
		/// —оздать XSD-схему
		/// использу€ XmlWriter
		/// </summary>
		/// <param name="meta">ћетаданные</param>
		/// <param name="writer">XmlWriter</param>
		public void CreateXSD(MetaData meta, XmlWriter writer)
		{
			XmlDocument document = CreateXSD(meta);
			document.WriteTo(writer);
		}
		/// <summary>
		/// —оздать XSD-схему
		/// использу€ TextWriter
		/// </summary>
		/// <param name="meta">ћетаданные</param>
		/// <param name="writer">TextWriter</param>
		public void CreateXSD(MetaData meta, TextWriter writer)
		{
			XmlTextWriter xmlWriter = new XmlTextWriter(writer);
			SerializerUtils.InitIdentation(meta, xmlWriter);
			CreateXSD(meta, xmlWriter);
		}
		/// <summary>
		/// —оздать XSD-схему
		/// в поток использу€ определенную кодировку
		/// </summary>
		/// <param name="meta">ћетаданные</param>
		/// <param name="stream">ѕоток дл€ записи</param>
		/// <param name="encoding">»спользуема€ кодировка</param>
		public void CreateXSD(MetaData meta, Stream stream, Encoding encoding)
		{
			XmlTextWriter writer = new XmlTextWriter(stream, encoding);
			SerializerUtils.InitIdentation(meta, writer);
			CreateXSD(meta, writer);
		}
		/// <summary>
		/// —оздать XSD-схему
		/// в файл использу€ определенную кодировку
		/// </summary>
		/// <param name="meta">ћетаданные</param>
		/// <param name="fileName">»м€ файла</param>
		/// <param name="encoding">»спользуема€ кодировка</param>
		public void CreateXSD(MetaData meta, string fileName, Encoding encoding)
		{
			XmlTextWriter writer = new XmlTextWriter(fileName, encoding);
			SerializerUtils.InitIdentation(meta, writer);
			try
			{
				CreateXSD(meta, writer);
			}
			finally
			{
				writer.Close();
			}
		}
		/// <summary>
		/// —оздать XSD-схему
		/// в файл использу€ кодировку по умолчанию
		/// </summary>
		/// <param name="meta">ћетаданные</param>
		/// <param name="fileName">»м€ файла</param>
		public void CreateXSD(MetaData meta, string fileName)
		{
			CreateXSD(meta, fileName, null);
		}
	}
	class XSDCreator
	{
		private const string prefix = "xs";
		private const string NSUri = "http://www.w3.org/2001/XMLSchema";
		private MetaData metaData;
		private ICSObjectPropertyManager propManager;
		private XmlDocument document;
		private XmlElement root;
		private XmlElement metaRoot;
		public XSDCreator(MetaData metaData)
		{
			this.propManager = metaData.Serializer.PropManager;
			this.metaData = metaData;

			document = new XmlDocument();
			document.AppendChild(root = CreateElement("schema"));
			root.SetAttribute("elementFormDefault", "qualified");


			XmlElement node = CreateElement("element");
			node.SetAttribute("name", metaData.RootElement);
			root.AppendChild(node);

			XmlElement nodeType = CreateElement("complexType");
			node.AppendChild(nodeType);

			XmlElement nodeSeq = CreateElement("sequence");
			nodeSeq.SetAttribute("maxOccurs", "unbounded");
			nodeType.AppendChild(nodeSeq);

			metaRoot = nodeSeq;


			XmlElement refElement = CreateElement("element");
			refElement.SetAttribute("name", metaData.RefElement);
			XmlElement refElementType = CreateElement("complexType");
			XmlElement refIdElement = CreateElement("attribute");
			refIdElement.SetAttribute("name", metaData.IdAttribute);
			refIdElement.SetAttribute("use", "required");
			refElementType.AppendChild(refIdElement);
			refElement.AppendChild(refElementType);
			root.AppendChild(refElement);
		}
		public XmlDocument Create()
		{
			foreach (DictionaryEntry entry in metaData.Storage)
			{
				ProcessType(entry.Key as Type, entry.Value as AliasStorage);
			}
			return document;
		}


		private XmlElement CreateElement(string name)
		{
			return document.CreateElement(prefix, name, NSUri);
		}

		private void CreateRef(XmlElement container, string refName, string min, string max)
		{
			XmlElement refNode = CreateElement("element");
			container.AppendChild(refNode);
			refNode.SetAttribute("ref", refName);
			if (min != null)
				refNode.SetAttribute("minOccurs", min);
			if (max != null)
				refNode.SetAttribute("maxOccurs", max);
		}


		private void ProcessType(Type type, AliasStorage storage)
		{
			if (type.IsAbstract)
				return;

			XmlElement node = CreateElement("element");
			node.SetAttribute("name", storage.Alias);
			root.AppendChild(node);
			CreateRef(metaRoot, storage.Alias, "0", null);

			XmlElement nodeType = CreateElement("complexType");
			node.AppendChild(nodeType);

			ProcessLinks(nodeType, type);
			ProcessValues(nodeType, type);
		}


		private void ProcessLinks(XmlElement nodeType, Type type)
		{
			bool linkAdded = false;
			XmlElement nodeSeq = CreateElement("sequence");
			nodeSeq.SetAttribute("minOccurs", "0");
			nodeSeq.SetAttribute("maxOccurs", "unbounded");
			foreach (DictionaryEntry entry in metaData.Storage)
			{
				Type probeType = entry.Key as Type;
				if (probeType.IsAssignableFrom(type))
				{
					linkAdded = linkAdded | ProcessTypeLinks(nodeSeq, probeType, entry.Value as AliasStorage);
				}
			}
			if (linkAdded)
			{
				CreateRef(nodeSeq, metaData.RefElement, "0", "unbounded");
				nodeType.AppendChild(nodeSeq);
			}
		}
		private bool ProcessTypeLinks(XmlElement nodeSeq, Type type, AliasStorage storage)
		{
			bool linkAdded = false;
			foreach (DictionaryEntry entry in storage.PropertyAlias)
			{
				linkAdded = linkAdded | ProcessLink(nodeSeq, type, entry.Key as string, entry.Value as AliasItem);
			}
			return linkAdded;
		}
		private bool ProcessLink(XmlElement nodeSeq, Type type, string refName, AliasItem item)
		{
			Type refType = metaData.GetType(item.RefTypeAlias);
			bool linkAdded = false;
			foreach (DictionaryEntry entry in metaData.Storage)
			{
				Type probeType = entry.Key as Type;
				if (refType.IsAssignableFrom(probeType) && !probeType.IsAbstract)
				{
					linkAdded = true;
					CreateRef(nodeSeq, ((AliasStorage)entry.Value).Alias, "0", "unbounded");
				}
			}
			return linkAdded;
		}


		private void ProcessValues(XmlElement nodeType, Type type)
		{
			bool idAdded = false;
			object instance = Activator.CreateInstance(type, true);
			ICollection<ICSValue> values = propManager.getAttributeList(instance);
			foreach (ICSValue valueDef in values)
			{
				idAdded = idAdded | valueDef.Name == metaData.IdAttribute;
				XmlElement nodeAtt = ProcessValue(nodeType, valueDef.Name);
				ProcessType(nodeAtt, valueDef);
			}
			if (!idAdded)
			{
				ProcessValue(nodeType, metaData.IdAttribute);
			}
		}
		private XmlElement ProcessValue(XmlElement nodeType, string valueDefName)
		{
			XmlElement nodeAtt = CreateElement("attribute");
			nodeType.AppendChild(nodeAtt);
			nodeAtt.SetAttribute("name", valueDefName);
			return nodeAtt;
		}
		private void ProcessType(XmlElement node, ICSValue valueDef)
		{
			string typeName = null;
			if (valueDef.TypeDef.SystemType == typeof(string))
				typeName = prefix + ":string";
			if (typeName != null)
				node.SetAttribute("type", typeName);
			if (valueDef.Required)
				node.SetAttribute("use", "required");
		}
	}
}
