using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using Framework.Utils;
using Framework.Utils.ObjectParser;
using Ksema.Framework;
using Ksema.Framework.Documentation;

namespace Framework.ActiveTemplate.Parser
{
	[Documentation("Framework.ActiveTemplateParser.Name", null, "Framework.ActiveTemplateParser.Usage")]
	public class ActiveTemplateParser : IDocumentationProvider
	{
		XMLObjectParser _xmlParser;

		public ActiveTemplateParser()
		{
			_xmlParser = new XMLObjectParser();
			setupParser();
		}

		public ActiveTemplateParser(XmlReader reader)
		{
			_xmlParser = new XMLObjectParser(reader);
			setupParser();
		}

		public void SetReader(XmlReader reader)
		{
			_xmlParser.RootNavigator = new XPathDocument(reader).CreateNavigator();
		}

		public void SetReader(XPathNavigator navigator)
		{
			_xmlParser.RootNavigator = navigator;
		}

		void setupParser()
		{
			_xmlParser.registerObjectBuilder("t_ref", new TemplateReferenceBuilder());
			_xmlParser.registerObjectBuilder("t_group", new TemplateGroupBuilder());
			_xmlParser.registerObjectBuilder("t_comment", new CommentTemplateGroupBuilder());
			_xmlParser.registerObjectBuilder("t_if", new ConditionTemplateGroupBuilder());
			_xmlParser.registerObjectBuilder("t_context", new ActiveTemplateGroupBuilder());
			_xmlParser.registerObjectBuilder("t_macro", new MacroTemplateBuilder());
			_xmlParser.registerObjectBuilder(XMLObjectParser.NODE_TEXT, new TextTemplateBuilder());
			_xmlParser.registerDefaultBuilder(new XmlElementTemplateBuilder());
		}

		public ITemplateNode parse()
		{
			ICollection result = _xmlParser.parse();
			if(result == null || result.Count != 1)
				throw new InvalidOperationException("invalid parse results");
			return (ITemplateNode) CUtils.getFirstItem(result);
		}

        public static ITemplateNode getTemplate(string resName)
        {
            Stream xmlStream = Utils.AppUtils.GetResourceStream(resName);
            XmlTextReader reader = new XmlTextReader(xmlStream);
            ActiveTemplateParser parser = new ActiveTemplateParser(reader);
            ITemplateNode root = parser.parse();
            return root;
		}

		#region Documentation

		private IDictionary<DocumentationLinkBaseData, object> GetObjectsToDocument(DocumentationManager manager)
		{
			var links = new Dictionary<DocumentationLinkBaseData, object>();
			foreach (var builder in _xmlParser.Builders)
				links.Add(new DocumentationLinkBaseData { LinkName = builder.Key}, _xmlParser.getBaseBuilder(builder.Value));
			if (_xmlParser.DefaultBuilder != null)
				links.Add(new DocumentationLinkBaseData { LinkName = "DEFAULT"}, _xmlParser.getBaseBuilder(_xmlParser.DefaultBuilder));
			return links;
		}

		DocumentationData IDocumentationProvider.GetObjectDocumentation(DocumentationManager manager, int depth)
		{
			var data = manager.DocumentSelf(this, typeof(ActiveTemplateParser));

			foreach (var linkItem in GetObjectsToDocument(manager))
			{
				var attribute = DocumentationManager.GetAttribute(linkItem.Value.GetType());
				if (attribute != null)
				{
					var link = new DocumentationLinkData();
					link.LinkName = linkItem.Key.LinkName;
					link.SetDataFromAttribute(attribute);
					data.Links.Add(link);
				}
			}

			return data;
		}

		#endregion
	}

	[Documentation("Framework.TemplateReferenceBuilder.Name", null, "Framework.TemplateReferenceBuilder.Usage")]
	class TemplateReferenceBuilder : IObjectBuilder
	{
		public const string KEY_ID = "id";
		public const string KEY_NAME = "name";
		public const string KEY_CONTEXT = "context";
		public const string KEY_RECALC = "recalc";
		const string KEY_RID = "refid";
		public object createObject(object context, IDictionary attValues)
		{
			string refId = (string) attValues[KEY_RID];
			string recalcString = (string)attValues[KEY_RECALC];
			return TemplateGroupBuilder.addToGroup(context, new TemplateReference(refId, recalcString));
		}
	}

	[Documentation("Framework.TemplateGroupBuilder.Name", null, "Framework.TemplateGroupBuilder.Usage")]
	class TemplateGroupBuilder : IObjectBuilder
	{
		public static ITemplateNode addToGroup(object context, ITemplateNode node)
		{
			ITemplateGroup parent = (ITemplateGroup) context;
			if(parent != null)
				parent.addNode(node);
			return node;
		}

		public object createObject(object context, IDictionary attValues)
		{
			string id = (string) attValues[TemplateReferenceBuilder.KEY_ID];
			string name = (string)attValues[TemplateReferenceBuilder.KEY_NAME];
			string groupContext = (string)attValues[TemplateReferenceBuilder.KEY_CONTEXT];
			return addToGroup(context, new TemplateGroup(id, name, groupContext));
		}
	}

	[Documentation("Framework.CommentTemplateGroupBuilder.Name", null, "Framework.CommentTemplateGroupBuilder.Usage")]
	class CommentTemplateGroupBuilder : IObjectBuilder
	{
        public object createObject(object context, IDictionary attValues)
		{
			return TemplateGroupBuilder.addToGroup(context, new CommentTemplateGroup());
		}
	}

	[Documentation("Framework.ConditionTemplateGroupBuilder.Name", null, "Framework.ConditionTemplateGroupBuilder.Usage")]
	class ConditionTemplateGroupBuilder : IObjectBuilder
	{
		public object createObject(object context, IDictionary attValues)
		{
			string id = (string)attValues[TemplateReferenceBuilder.KEY_ID];
			string expr = (string)attValues[ActiveTemplateGroupBuilder.KEY_EXPR];
			ConditionTemplateGroup group = new ConditionTemplateGroup(id, expr);

			return TemplateGroupBuilder.addToGroup(context, group);
		}
	}

	[Documentation("Framework.ActiveTemplateGroupBuilder.Name", null, "Framework.ActiveTemplateGroupBuilder.Usage")]
	class ActiveTemplateGroupBuilder : IObjectBuilder
	{
		public const string KEY_EXPR = "Value";
		const string KEY_ALWAYS = "Always";
		const string KEY_PRE = "Pre";
        const string KEY_POST = "Post";
        const string KEY_INNER = "Inner";

		public object createObject(object context, IDictionary attValues)
		{
			string id = (string) attValues[TemplateReferenceBuilder.KEY_ID];
			string expr = (string) attValues[KEY_EXPR];
		    ActiveTemplateGroup group = new ActiveTemplateGroup(id, expr);
			group.Always = (string)attValues[KEY_ALWAYS];
			group.PreRenderMacro = (string)attValues[KEY_PRE];
            group.PostRenderMacro = (string)attValues[KEY_POST];
            group.InnerMacro = (string)attValues[KEY_INNER];
            
		    return TemplateGroupBuilder.addToGroup(context, group);
		}
	}

	//[Documentation("Framework.TextTemplateBuilder.Name", null, "Framework.TextTemplateBuilder.Usage")]
	class TextTemplateBuilder : IObjectBuilder
	{
		public object createObject(object context, IDictionary attValues)
		{
			string text = (string) attValues[XMLObjectParser.NODE_TEXT];
			return TemplateGroupBuilder.addToGroup(context, new TextTemplate(text));
		}
	}

	[Documentation("Framework.MacroTemplateBuilder.Name", null, "Framework.MacroTemplateBuilder.Usage")]
	class MacroTemplateBuilder : IObjectBuilder
	{
		const string KEY_VALUE = "Value";
		const string KEY_HTMLENC = "Encode";
		public object createObject(object context, IDictionary attValues)
		{
			string id = (string) attValues[TemplateReferenceBuilder.KEY_ID];
			string text = (string) attValues[KEY_VALUE];
			string strHtmlEnc = (string)attValues[KEY_HTMLENC];
			bool enc = true;
			if(!Utils.Utils.isEmpty(strHtmlEnc))
				enc = Boolean.Parse(strHtmlEnc);
			MacroTemplate mt = new MacroTemplate(id, text);
			mt.UseHtmlEncoding = enc;
			return TemplateGroupBuilder.addToGroup(context, mt);
		}
	}

	[Documentation("Framework.XmlElementTemplateBuilder.Name", null, "Framework.XmlElementTemplateBuilder.Usage")]
	class XmlElementTemplateBuilder : IObjectBuilder
	{
		public object createObject(object context, IDictionary attValues)
		{
			string tagName = (string) attValues[XMLObjectParser.NODE_NAME];
			XmlElementTemplate xml = new XmlElementTemplate(tagName);
			foreach(DictionaryEntry de in attValues)
			{
				string attName = (string) de.Key;
				if(attName != XMLObjectParser.NODE_NAME && attName != XMLObjectParser.NODE_TEXT)
					xml.addAttributeTemplate(attName, new MacroTemplate("", (string) de.Value));
			}
			return TemplateGroupBuilder.addToGroup(context, xml);
		}
	}
}
