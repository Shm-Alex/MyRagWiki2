using System;
using System.Collections;
using System.Collections.Specialized;
using System.Text;
using System.Xml;
using Framework.Collections;
using Ksema.ObjectModel;
using System.Collections.Generic;

namespace Serializer
{
	class CSWriter
	{
		private CSSerializer serializer;
		private XmlDocument document;
		private bool metaDataWritten;
		private IDictionary processedItems;
		private string namespaceURI;
		public CSWriter(CSSerializer _serializer)
		{
			serializer = _serializer;
			processedItems = new Hashtable();
			namespaceURI = serializer.MetaData.NamespaceURI;
			document = new XmlDocument();
			document.AppendChild(document.CreateElement(serializer.MetaData.RootElement, namespaceURI));
			metaDataWritten = false;
		}

		public XmlDocument Process(object item)
		{
			ProcessItem(document.DocumentElement, item);
			if (!metaDataWritten)
			{
				serializer.MetaData.SerializeTo(document);
				metaDataWritten = true;
			}
			return document;
		}

		private XmlElement ProcessItem(XmlElement parent, object item)
		{
			if (item == null)
				return null;
			if (processedItems.Contains(item))
				return processedItems[item] as XmlElement;
			// Self
			string name = serializer.MetaData.GetName(item);
			if (Framework.Utils.Utils.isEmpty(name))
				return null;
			XmlElement node = document.CreateElement(name, namespaceURI);
			parent.AppendChild(node);
			processedItems.Add(item, node);
			// Content
			ProcessValues(item, node);
			ProcessLinks(item, node);
			return node;
		}
		private IDictionary SortValues(object item)
		{
			IDictionary sortedValues = new MultiMap();
			ICollection<ICSValue> attributes = serializer.PropManager.getAttributeList(item);
			foreach (ICSValue attribute in attributes)
			{
				string name = serializer.MetaData.GetName(item, attribute);
				if (Framework.Utils.Utils.isEmpty(name))
					continue;
				object value = attribute.getValue();
				if (value == null && !serializer.MetaData.WriteEmptyValues)
					continue;
				string stringValue = value != null ? attribute.TypeDef.ToString(value) : String.Empty;
				sortedValues.Add(serializer.MetaData.GetIndex(item, attribute), new ObjectPair(name, stringValue));
			}
			return sortedValues;
		}
		private void ProcessValues(object item, XmlElement node)
		{
			IDictionary sortedValues = SortValues(item);
			ValueStyle style = serializer.MetaData.ValueStyle;
			foreach (DictionaryEntry entry in sortedValues)
			{
				ObjectPair pair = (ObjectPair)entry.Value;
				string name = pair.First as string;
				string stringValue = pair.Second as string;
				switch (style)
				{
					case ValueStyle.Attribute:
						node.SetAttribute(name, stringValue);
						break;
					case ValueStyle.Element:
						XmlElement valueNode = document.CreateElement(name, namespaceURI);
						if (serializer.MetaData.ValueAttribute != MetaData.UseTextInsteadOfAttribute)
							valueNode.SetAttribute(serializer.MetaData.ValueAttribute, stringValue);
						else
							valueNode.InnerText = stringValue;
						node.AppendChild(valueNode);
						break;
					default:
						throw new SerializationException("Unknown value style - " + style.ToString());
				}
			}
		}
		private IDictionary SortLinks(object item)
		{
			IDictionary sortedLinks = new MultiMap();
			ICollection<ICSLink> links = serializer.PropManager.getLinkList(item);
			foreach (ICSLink link in links)
			{
				string name = serializer.MetaData.GetName(item, link);
				if (Framework.Utils.Utils.isEmpty(name))
					continue;
				sortedLinks.Add(serializer.MetaData.GetIndex(item, link), new ObjectPair(name, link));
			}
			return sortedLinks;
		}
		private void ProcessLinks(object item, XmlElement node)
		{
			IDictionary sortedLinks = SortLinks(item);
			foreach (DictionaryEntry entry in sortedLinks)
			{
				ObjectPair pair = (ObjectPair)entry.Value;
				string name = pair.First as string;
				ICSLink link = pair.Second as ICSLink;
				XmlElement linkContainer;
				if (serializer.MetaData.OmitTags)
					linkContainer = node;
				else
					node.AppendChild(linkContainer = document.CreateElement(name, namespaceURI));
				bool isLinkToCompositeParent = false;
				if (link is ICSSingleLink)
				{
					object linkedItem = (link as ICSSingleLink).getObject();
					ProcessLink(linkContainer, item, name, link, linkedItem, out isLinkToCompositeParent);
				}
				else if (link is ICSMultiLink)
				{
					IEnumerable linkedItems = (link as ICSMultiLink).getLinkedObjects();
					if (linkedItems != null)
						foreach (object linkedItem in linkedItems)
							ProcessLink(linkContainer, item, name, link, linkedItem, out isLinkToCompositeParent);
				}
				else
					throw new SerializationException("Unknown link type - " + link.GetType().FullName);
				if (linkContainer.IsEmpty && !serializer.MetaData.OmitTags && (!serializer.MetaData.WriteEmptyLinks || isLinkToCompositeParent))
					node.RemoveChild(linkContainer);
			}
		}
		private void ProcessLink(XmlElement node, object item, string linkAlias, ICSLink link, object linkedItem, out bool isLinkToCompositeParent)
		{
			ICSLink backLink = link.getBackLink(linkedItem);
			isLinkToCompositeParent = IsLinkToCompositeParent(link, backLink);
			if (linkedItem == null)
				return;

			ILinkWriter linkWriter = serializer.LinkWriter;
			bool isComposite = linkWriter.IsComposite(link, backLink);
			XmlElement parent = isLinkToCompositeParent || !isComposite ? document.DocumentElement : node;
			linkWriter.LinkStarted(link, backLink);
			XmlElement linkedNode = ProcessItem(parent, linkedItem);
			linkWriter.LinkFinished(link, backLink);
			if (linkedNode == null || isLinkToCompositeParent)
				return;

			if (!isComposite)
			{
				XmlElement refNode = CreateRefLink(node, linkedNode, linkedItem);
				CreateByRefAttribute(item, link, refNode, linkAlias);
			}
			else if (linkedNode.ParentNode != parent)
			{
				XmlNode temp = parent.ParentNode;
				while (temp != null && temp != linkedNode)
					temp = temp.ParentNode;
				if (temp == null)
				{
					if (linkedNode.ParentNode != document.DocumentElement)
					{
						XmlElement refNode = CreateRefLink((XmlElement)linkedNode.ParentNode, linkedNode, linkedItem);
						CreateByRefAttribute(refNode, linkedNode);
					}
					linkedNode.ParentNode.RemoveChild(linkedNode);
					parent.AppendChild(linkedNode);
					CreateByRefAttribute(item, link, linkedNode, linkAlias);
				}
				else
				{
					XmlElement refNode = CreateRefLink(node, linkedNode, linkedItem);
					CreateByRefAttribute(item, link, refNode, linkAlias);
				}
			}
			else
				CreateByRefAttribute(item, link, linkedNode, linkAlias);
		}
		private void CreateByRefAttribute(object item, ICSLink link, XmlElement linkedNode, string linkAlias)
		{
			if (serializer.MetaData.OmitTags && Framework.Utils.Utils.isEmpty(serializer.MetaData.GetRefTypeName(item, link)))
			{
				linkedNode.SetAttribute(serializer.MetaData.ByRefAttribute, linkAlias);
			}
		}
		private void CreateByRefAttribute(XmlElement refNode, XmlElement linkedNode)
		{
			if (serializer.MetaData.OmitTags)
			{
				string linkAlias = linkedNode.GetAttribute(serializer.MetaData.ByRefAttribute);
				if (!Framework.Utils.Utils.isEmpty(linkAlias))
					refNode.SetAttribute(serializer.MetaData.ByRefAttribute, linkAlias);
			}
		}
		private XmlElement CreateRefLink(XmlElement node, XmlElement linkedNode, object linkedItem)
		{
			string id = linkedNode.GetAttribute(serializer.MetaData.IdAttribute);
			if (Framework.Utils.Utils.isEmpty(id))
				linkedNode.SetAttribute(serializer.MetaData.IdAttribute, id = GetId(linkedItem));
			XmlElement refNode = document.CreateElement(serializer.MetaData.RefElement, namespaceURI);
			refNode.SetAttribute(serializer.MetaData.IdAttribute, id);
			/*
			node.AppendChild(refNode);
			/*/
			XmlNode childBefore = null;
			XmlNode childAfter = null;
			if (node.HasChildNodes)
			{
				foreach (XmlElement child in node.ChildNodes)
				{
					if (child.Name != serializer.MetaData.RefElement)
						continue;
					string childId = child.GetAttribute(serializer.MetaData.IdAttribute);
					if (String.IsNullOrWhiteSpace(childId))
						continue;
					if (String.Compare(childId, id) < 0)
						childBefore = child;
					if (childAfter == null && String.Compare(id, childId) < 0)
						childAfter = child;
				}
			}
			if (childBefore == null)
			{
				if (childAfter == null)
				{
					node.AppendChild(refNode);
				}
				else
				{
					node.InsertBefore(refNode, childAfter);
				}
			}
			else
			{
				node.InsertAfter(refNode, childBefore);
			}
			//*/
			return refNode;
		}
		private string GetId(object item)
		{
			IIdentityProvider provider = null;
			if (item is IIdProviderFactory)
				provider = (item as IIdProviderFactory).getProvider(item);
			if (provider == null)
			{
				if (item is IIdentityProvider)
					provider = item as IIdentityProvider;
				else
				{
					Type type = null;
					do
					{
						type = type == null ? item.GetType() : type.BaseType;
						object[] attributes = type.GetCustomAttributes(typeof(IdProviderAttribute), false);
						if (attributes != null && attributes.Length > 0)
						{
							IdProviderAttribute attribute = (IdProviderAttribute) attributes[0];
							IIdProviderFactory factory = attribute.getFactory();
							if (factory != null)
								provider = factory.getProvider(item);
						}
					}
					while (type.BaseType != null && provider == null);
					if (provider == null)
						provider = serializer.DefaultProvider;
				}
			}
			return provider.getIdentity(item).ToString();
		}


		public static bool IsLinkToCompositeParent(ICSLink link, ICSLink backLink)
		{
			return backLink != null && backLink.IsComposite && !link.IsComposite;
		}
	}
}
