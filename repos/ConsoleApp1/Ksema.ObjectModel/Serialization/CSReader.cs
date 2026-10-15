using System;
using System.Collections;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using Ksema.ObjectModel;
using System.Collections.Generic;
namespace Serializer
{
	class CSReader
	{
		private CSSerializer serializer;
		private IDictionary processedItems; // node -- item
		private IList pendingLinks; // link -- id
		private IDictionary referredItems; // id -- item
		private IDictionary idItems; // id -- node
		private IList templateObjects;
		public CSReader(CSSerializer _serializer, ICollection _templateObjects)
		{
			serializer = _serializer;
			processedItems = new Hashtable();
			pendingLinks = new ArrayList();
			referredItems = new ListDictionary();
			idItems = new ListDictionary();
			if (_templateObjects != null)
				templateObjects = new ArrayList(_templateObjects);
		}

		public ICollection Process(XmlDocument document)
		{
			serializer.MetaData.DeserializeFrom(document);
			FillIdNodes(document);
			IList result = new ArrayList();
			foreach (XmlElement node in document.DocumentElement.SelectNodes("*"))
				if (node.Name != MetaData.METADATA)
					result.Add(ProcessItem(node, true));
			ProcessPendingLinks();
			CallBack();
			return result;
		}

		private void CallBack()
		{
			foreach (object item in processedItems.Values)
				if (item is IDeserializationCallback)
				{
					((IDeserializationCallback)item).OnDeserialization(serializer);
				}
		}
		private object ProcessItem(XmlElement node, bool withTemplate)
		{
			if (node == null)
				return null;
			if (processedItems.Contains(node))
				return processedItems[node];
			// Self
			Type type = serializer.MetaData.GetType(node.Name);
			object item = null;
			if (withTemplate && templateObjects != null)
				foreach (object templatedItem in templateObjects)
					if (templatedItem != null && templatedItem.GetType() == type)
					{
						item = templatedItem;
						break;
					}
			if (item == null)
				item = Activator.CreateInstance(type);
			processedItems.Add(node, item);
			if (node.HasAttribute(serializer.MetaData.IdAttribute))
				referredItems.Add(node.GetAttribute(serializer.MetaData.IdAttribute), item);
			// Content
			ArrayList childNodes = new ArrayList();
			foreach (XmlElement childNode in node.SelectNodes("*"))
				childNodes.Add(childNode);
			ProcessValues(node, childNodes, item);
			ProcessLinks(node, childNodes, item);
			return item;
		}
		private void ProcessValues(XmlElement node, IList childNodes, object item)
		{
			ICollection<ICSValue> attributes = serializer.PropManager.getAttributeList(item);
			foreach (ICSValue attribute in attributes)
			{
				string name = serializer.MetaData.GetName(item, attribute);
				if (Framework.Utils.Utils.isEmpty(name))
					continue;
				string stringValue = node.GetAttribute(name);
				if (Framework.Utils.Utils.isEmpty(stringValue))
				{
					XmlElement valueNode = (XmlElement)node.SelectSingleNode(name);
					if (valueNode != null)
					{
						childNodes.Remove(valueNode);
						if (serializer.MetaData.ValueAttribute != MetaData.UseTextInsteadOfAttribute)
							stringValue = valueNode.GetAttribute(serializer.MetaData.ValueAttribute);
						if (Framework.Utils.Utils.isEmpty(stringValue))
							stringValue = valueNode.InnerText;
					}
				}
				if (!Framework.Utils.Utils.isEmpty(stringValue))
				{
					object value = attribute.TypeDef.ToObject(stringValue);
					attribute.setValue(value);
				}
			}
		}
		private void ProcessLinks(XmlElement node, IList childNodes, object item)
		{
			IDictionary linkNameTable = new ListDictionary(); // name - link
			IDictionary linkTypeTable = new ListDictionary(); // refTypeName - link
			ICollection<ICSLink> links = serializer.PropManager.getLinkList(item);
			foreach (ICSLink link in links)
			{
				string refTypeName = serializer.MetaData.GetRefTypeName(item, link);
				if (!Framework.Utils.Utils.isEmpty(refTypeName))
					linkTypeTable.Add(refTypeName, link);
				string name = serializer.MetaData.GetName(item, link);
				if (Framework.Utils.Utils.isEmpty(name))
					continue;
				linkNameTable.Add(name, link);
				// В контейнере
				XmlElement linkContainer = (XmlElement)node.SelectSingleNode(name);
				if (linkContainer != null)
				{
					childNodes.Remove(linkContainer);
					foreach (XmlElement linkNode in linkContainer.SelectNodes("*"))
						ProcessLinkedNode(linkTypeTable, node, link, linkNode);
				}
			}
			// В элементах с именем ссылки
			foreach (XmlElement childNode in node.SelectNodes("*[@" + serializer.MetaData.ByRefAttribute + "]"))
			{
				childNodes.Remove(childNode);
				string name = childNode.GetAttribute(serializer.MetaData.ByRefAttribute);
				ICSLink link = (ICSLink)linkNameTable[name];
				ProcessLinkedNode(linkTypeTable, node, link, childNode);
			}
			// В элементах без имени ссылки
			if (childNodes.Count > 0)
			{
				foreach (XmlElement childNode in childNodes)
				{
					ICSLink link = FindLinkForUnnamedRef(linkTypeTable, node, childNode);
					ProcessLinkedNode(linkTypeTable, node, link, childNode);
				}
			}
		}
		private void FillIdNodes(XmlDocument document)
		{
			foreach (XmlElement node in document.SelectNodes("//*[@" + serializer.MetaData.IdAttribute + " and name() != '" + serializer.MetaData.RefElement + "']"))
			{
				idItems.Add(node.GetAttribute(serializer.MetaData.IdAttribute), node);
			}
		}
		private ICSLink FindLinkForUnnamedRef(IDictionary linkTypeTable, XmlElement node, XmlElement childNode)
		{
			if (childNode.Name == serializer.MetaData.RefElement)
			{
				string id = childNode.GetAttribute(serializer.MetaData.IdAttribute);
				//childNode = childNode.SelectSingleNode("//*[@id='" + id + "' and name() != '" + serializer.MetaData.RefElement + "']") as XmlElement;
				childNode = idItems[id] as XmlElement;
				if (childNode == null)
					throw new SerializationException("Object with id = '" + id + "' not found (referred by object '" + node.Name + "')");
			}
			else if (childNode.Name == serializer.MetaData.ExternalObjectsElement)
				return null;
			Type linkedObjectType = serializer.MetaData.GetType(childNode.Name);
			return FindLinkForType(linkTypeTable, node, childNode, linkedObjectType);
		}
		private ICSLink FindLinkForType(IDictionary linkTypeTable, XmlElement node, XmlElement linkedNode, Type linkedObjectType)
		{
			foreach (DictionaryEntry entry in linkTypeTable)
			{
				Type refType = serializer.MetaData.GetType(entry.Key as string);
				if (refType.IsAssignableFrom(linkedObjectType))
					return (ICSLink)entry.Value;
			}
			throw new SerializationException("There is no default link between objects '" + node.Name + "' and '" + linkedNode.Name + "'");
		}
		private void ProcessLinkedNode(IDictionary linkTypeTable, XmlElement node, ICSLink link, XmlElement linkNode)
		{
			if (linkNode.Name == serializer.MetaData.RefElement)
				ProcessRefLink(link, linkNode);
			else if (linkNode.Name == serializer.MetaData.ExternalObjectsElement)
			{
				IExternalObjects objects = serializer.ExternalObjectsProvider.getExternalObjectsInstance(linkNode);
				if (objects == null)
					throw new SerializationException("Object provider not found for node:" + Environment.NewLine + linkNode.InnerXml);
				ICollection linkedObjects = objects.getObjects();
				if (link == null && linkedObjects.Count > 0)
				{
					Type linkedObjectType = Framework.Utils.CUtils.getFirstItem(linkedObjects).GetType();
					link = FindLinkForType(linkTypeTable, node, linkNode, linkedObjectType);
				}
				foreach (object linkedItem in linkedObjects)
					AppendLink(link, linkedItem);
			}
			else
			{
				object linkedItem = ProcessItem(linkNode, false);
				if (linkedItem != null)
					AppendLink(link, linkedItem);
			}
		}
		private static void AppendLink(ICSLink link, object linkedItem)
		{
			if (link is ICSSingleLink)
				(link as ICSSingleLink).setObject(linkedItem);
			else if (link is ICSMultiLink)
				(link as ICSMultiLink).addLinkedObject(linkedItem);
			else
				throw new SerializationException("Unknown link type - " + link.GetType().FullName);
		}
		private void ProcessRefLink(ICSLink link, XmlElement linkNode)
		{
			string id = linkNode.GetAttribute(serializer.MetaData.IdAttribute);
			pendingLinks.Add(new DictionaryEntry(link, id));
		}
		private void ProcessPendingLinks()
		{
			foreach (DictionaryEntry pair in pendingLinks)
			{
				string id = pair.Value as string;
				ICSLink link = pair.Key as ICSLink;
				object item = referredItems[id];
				if (item == null)
					throw new SerializationException("Object with id = '" + id + "' not found (referred by link '" + link.Name + "')");
				AppendLink(link, item);
			}
		}
	}
}
