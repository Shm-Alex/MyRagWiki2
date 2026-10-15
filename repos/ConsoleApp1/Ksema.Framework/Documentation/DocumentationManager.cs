using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Ksema.Framework.Collections;

namespace Ksema.Framework.Documentation
{
	/// <summary>
	/// Документирует себя и всю цепочку связанных объектов самостоятельно
	/// </summary>
	public interface IDocumentationProvider
	{
		DocumentationData GetObjectDocumentation(DocumentationManager manager, int depth);
	}

	/// <summary>
	/// Предоставляет документацию только о себе, но НЕ о связанных объектах
	/// </summary>
	public interface ISelfDocumentationProvider
	{
		DocumentationBaseData GetSelfDocumentation(DocumentationManager manager);
	}

	/// <summary>
	/// Предоставляет список связанных объектов для документирования
	/// </summary>
	public interface ILinksDocumentationProvider
	{
		IDictionary<DocumentationLinkBaseData, object> GetObjectsToDocument(DocumentationManager manager);
	}

	public class DocumentationManager
	{
		private IDictionary<Type, DocumentationData> _objects;
		private DocumentationManager()
		{
			_objects = new Dictionary<Type, DocumentationData>();
		}

		private DocumentationData DocumentObject(int depth, object item, Type itemType)
		{
			if (item == null && itemType == null)
				return null;
			DocumentationData data;
			if (_objects.TryGetValue(itemType, out data))
			{
				if (data == null)
				{
					data = DocumentSelf(item, itemType);
					if (data != null)
						data.ShortName = "Documentation.Loop".Localize() + data.ShortName;
				}
				return data;
			}

			_objects.Add(itemType, null);

			// full
			if (item is IDocumentationProvider)
				data = (item as IDocumentationProvider).GetObjectDocumentation(this, depth);
			else
			{
				// self
				data = DocumentSelf(item, itemType);
				// children
				data = DocumentLinks(depth, item, itemType, data);
			}

			_objects[itemType] = data;

			return data;
		}

		public DocumentationData DocumentSelf(object item, Type itemType)
		{
			DocumentationData data = null;
			if (item is ISelfDocumentationProvider)
				data = new DocumentationData((item as ISelfDocumentationProvider).GetSelfDocumentation(this));
			else
			{
				DocumentationAttribute attribute = GetAttribute(itemType);
				if (attribute != null)
				{
					data = new DocumentationData(itemType);
					data.SetDataFromAttribute(attribute);
				}
			}
			return data;
		}

		public DocumentationData DocumentLinks(int depth, object item, Type itemType, DocumentationData data)
		{
			if (item is ILinksDocumentationProvider)
			{
				var links = (item as ILinksDocumentationProvider).GetObjectsToDocument(this);
				if (links.Count > 0)
				{
					if (data == null)
						data = new DocumentationData(itemType);
					foreach (var link in links)
						data.Links.Add(new DocumentationLinkData(link.Key)
						{
							LinkedObject = DocumentObject(depth - 1, link.Value, GetProcessingType(link.Value))
						});
				}
			}
			else
			{
				const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;
				data = ProcessLinks(depth, itemType, itemType.GetFields(flags), data, member => member.Name, member => ((FieldInfo)member).FieldType);
				data = ProcessLinks(depth, itemType, itemType.GetProperties(flags), data, member => member.Name, member => ((PropertyInfo)member).PropertyType);
				data = ProcessLinks(depth, itemType, itemType.GetMethods(flags), data, MethodNameProvider, member => ((MethodInfo)member).ReturnType);
			}
			return data;
		}

		private string MethodNameProvider(MemberInfo member)
		{
			var method = (MethodInfo)member;
			var name = new StringBuilder();
			name.Append(method.Name).Append("(");
			bool first = true;
			foreach (var parameter in method.GetParameters())
			{
				if (first)
					first = false;
				else
					name.Append(", ");
				name.Append(parameter.ParameterType.Name).Append(" ").Append(parameter.Name);
				if (parameter.DefaultValue != null && parameter.DefaultValue != DBNull.Value)
					name.Append(" = ").Append(parameter.DefaultValue.ToString());
			}
			name.Append(")");
			return name.ToString();
		}

		private DocumentationData ProcessLinks(int depth, Type itemType, IEnumerable<MemberInfo> members, DocumentationData data, Func<MemberInfo, string> description, Func<MemberInfo, Type> returnType)
		{
			foreach (var link in FilterMembers(members))
			{
				if (data == null)
					data = new DocumentationData(itemType);
				var linkData = new DocumentationLinkData
				{
					LinkName = description(link.Key),
					LinkedObject = DocumentObject(depth - 1, null, returnType(link.Key))
				};
				linkData.SetDataFromAttribute(link.Value);
				data.Links.Add(linkData);
			}
			return data;
		}

		private IEnumerable<KeyValuePair<MemberInfo, DocumentationAttribute>> FilterMembers(IEnumerable<MemberInfo> members)
		{
			//var result = new SortedDictionary<MemberInfo, DocumentationAttribute>(new GenericComparer<MemberInfo>((x, y) => x.Name.CompareTo(y.Name)));
			var result = new List<KeyValuePair<MemberInfo, DocumentationAttribute>>();
			foreach (var member in members)
			{
				var attribute = GetAttribute(member);
				if (attribute != null)
					result.Add(new KeyValuePair<MemberInfo, DocumentationAttribute>(member, attribute));
			}
			result.Sort(new GenericComparer<KeyValuePair<MemberInfo, DocumentationAttribute>>((x, y) => x.Key.Name.CompareTo(y.Key.Name)));
			return result;
		}


		public static DocumentationData DocumentObject(object item)
		{
			return DocumentObject(Byte.MaxValue, item);
		}

		public static DocumentationData DocumentObject(int depth, object item)
		{
			return new DocumentationManager().DocumentObject(depth, item, GetProcessingType(item));
		}

		public static DocumentationAttribute GetAttribute(MemberInfo member)
		{
			var attribute = member.GetCustomAttributes(typeof(DocumentationAttribute), false).FirstOrDefault() as DocumentationAttribute;
			if (attribute == null)
				attribute = member.GetCustomAttributes(typeof(DocumentationAttribute), true).FirstOrDefault() as DocumentationAttribute;
			return attribute;
		}

		public static Type GetProcessingType(object item)
		{
			return item != null ? (item is Type ? item as Type : item.GetType()) : null;
		}

		public static DocumentationData DocumentObjects(int depth, object item1, object item2)
		{
			return DocumentObjects(depth, new object[2] { item1, item2 });
		}

		public static DocumentationData DocumentObjects(int depth, IEnumerable<object> items)
		{
			if (depth <= 0)
				return null;

			var data = new DocumentationData();
			data.ShortName = DocumentationData.StubName;
			var manager = new DocumentationManager();
			foreach (var item in items)
			{
				var childData = manager.DocumentObject(depth, item, GetProcessingType(item));
				if (childData != null)
				{
					//*
					data.Categories.Add(childData);
					/*/
					data.Links.Add(new DocumentationLinkData
					{
						LinkName = item.GetType().Name,
						LinkedObject = childData
					});
					//*/
				}
			}

			return data;
		}

#if DEBUG
		public static string Print(DocumentationData data)
		{
			var text = new StringBuilder();
			Print(text, 0, data, true);
			return text.ToString();
		}
		private static void Print(StringBuilder text, int level, DocumentationData data, bool shiftFirstLine)
		{
			if (data == null)
				return;
			string prefix = String.Join("", Enumerable.Repeat(" ", level).ToArray());
			if (shiftFirstLine)
				text.Append(prefix);
			text.Append(data.ObjectName).Append(" : ").AppendLine(data.ShortName ?? data.ObjectType.FullName);
			foreach (var category in data.Categories)
				Print(text, level + 3, category, true);
			foreach (var link in data.Links)
			{
				text.Append(prefix).Append(" ->").Append(link.LinkName).Append(" : ").Append(link.ShortName ?? "no description");
				//text.AppendLine();
				if (link.LinkedObject != null)
				{
					text.Append(", Class: ");
					Print(text, level + 5, link.LinkedObject, false);
				}
				else
					text.AppendLine();
			}
		}
#endif
	}

	// - "Standard" documentations (for type string, for example)
}
