using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Xml;
using System.Xml.XPath;
using Framework.Collections;
using Framework.Utils;

namespace Framework.Utils.ObjectParser
{
    public class BuildContext
    {
        public object ContextObject;
        public IDictionary Attributes;
        public bool TerminateBuild = false;

        public BuildContext(object aContextObject, IDictionary anAttList)
        {
            ContextObject = aContextObject;
            Attributes = anAttList;
        }
    }

    //old-style
	public interface IObjectBuilder
	{
		object createObject(object context, IDictionary attValues);
	}

	public interface IObjectBuilderProvider
	{
        IControllingObjectBuilder getBuilder(object context, string childTag);
	}

	public interface IControllingObjectBuilder
	{
        object createObject(BuildContext context);
	}

    public interface IObjectCollection
    {
        void AddObject(object obj);
        void RemoveObject(object obj);
        ICollection Objects { get;}
    }

    public interface ISetupObjectParser
    {
        void setupParser(IObjectParser parser);
    }

	public interface IObjectParser
	{
		ICollection parse();
        void registerDefaultBuilder(IObjectBuilder builder);
        void registerObjectBuilder(string tagName, IObjectBuilder builder);

        void registerDefaultBuilder(IControllingObjectBuilder builder);
        void registerObjectBuilder(string tagName, IControllingObjectBuilder builder);

		IControllingObjectBuilder DefaultBuilder { get; }
		IEnumerable<KeyValuePair<string, IControllingObjectBuilder>> Builders { get; }
		XPathNavigator RootNavigator { get; set; }

		object getBaseBuilder(IControllingObjectBuilder aBuilder);
	}

	public class XMLObjectParser : IObjectParser
	{
		public const string NODE_TEXT = "@text";
		public const string NODE_NAME = "@name";

		IControllingObjectBuilder _defaultBuilder = new EmptyBuilder();
		Dictionary<string, IControllingObjectBuilder> _builderTable = new Dictionary<string, IControllingObjectBuilder>();
		XPathNavigator _rootNav = null;

		public XMLObjectParser(XmlReader reader)
		{
			_rootNav = new XPathDocument(reader).CreateNavigator();
		}

		public XMLObjectParser(XPathNavigator nav)
		{
			_rootNav = nav;
		}

		public XMLObjectParser()
		{
		}

		public void registerDefaultBuilder(IObjectBuilder builder)
		{
			if (builder == null)
				throw new ArgumentNullException("builder");
			_defaultBuilder = getControllingBuilder(builder);
		}

		public void registerObjectBuilder(string tagName, IObjectBuilder builder)
		{
			if (builder == null)
				throw new ArgumentNullException("builder");
			_builderTable[tagName] = getControllingBuilder(builder);
		}

	    public void registerDefaultBuilder(IControllingObjectBuilder builder)
	    {
            if (builder == null)
                throw new ArgumentNullException("builder");
            _defaultBuilder = builder;
        }

	    public void registerObjectBuilder(string tagName, IControllingObjectBuilder builder)
	    {
            if (builder == null)
                throw new ArgumentNullException("builder");
            _builderTable[tagName] = builder;
        }

	    private IControllingObjectBuilder getControllingBuilder(IObjectBuilder aBuilder)
	    {
            if(aBuilder is IControllingObjectBuilder)
                return (IControllingObjectBuilder)aBuilder;
            else
                return new ControllingBuilderWrapper(aBuilder);
	    }

		public object getBaseBuilder(IControllingObjectBuilder aBuilder)
		{
			if (aBuilder is ControllingBuilderWrapper)
				return (aBuilder as ControllingBuilderWrapper).BaseBuilder;
			else
				return aBuilder;
		}

	    public ICollection parse()
		{
			XPathNavigator nav = _rootNav.Clone();
			return parse(null, nav, null);
		}

		//TODO: переделать реализацию под использование непосредственно XmlReader
        ICollection parse(object context, XPathNavigator nav, IControllingObjectBuilder previousBuilder)
		{
			IList result = new List();
			XPathNodeIterator xIt = nav.SelectChildren(XPathNodeType.All);
			IDictionary attList;
            IControllingObjectBuilder builder;

			while(xIt.MoveNext())
			{
				attList = null;
				builder = null;
				if(xIt.Current.NodeType == XPathNodeType.Element)
				{
					builder = getBuilder(xIt.Current.Name, previousBuilder, context, true);
					attList = readAttributes(xIt.Current.Clone());
				}
				else if(xIt.Current.NodeType == XPathNodeType.Text)
				{
					builder = getBuilder(NODE_TEXT, previousBuilder, context, false);
					attList = new ListDictionary();
					attList[NODE_NAME] = NODE_TEXT;
					attList[NODE_TEXT] = xIt.Current.Value;
				}
				if(builder != null)
				{
                    BuildContext ctx = new BuildContext(context, attList);

					object newCtx = builder.createObject(ctx);
                    if(!ctx.TerminateBuild)
					    parse(newCtx, xIt.Current, builder);
					if(newCtx != null)
						result.Add(newCtx);
				}
			}

			return result;
		}

		IDictionary readAttributes(XPathNavigator nav)
		{
			IDictionary result = new ListDictionary();

			if(nav.NodeType != XPathNodeType.Element)
				throw new InvalidOperationException("invalid node type: " + nav.NodeType);

			result[NODE_NAME] = nav.Name;

			string txt = nav.Value;
			if(!Utils.isEmpty(txt))
				result[NODE_TEXT] = txt;

			if(nav.MoveToFirstAttribute())
			{
				do
				{
					string attName = nav.Name;
					result[attName] = nav.Value;
				}while(nav.MoveToNextAttribute());
			}
			
			return result;
		}

        IControllingObjectBuilder getBuilder(string tagName, IControllingObjectBuilder previousBuilder, object context, bool useDefault)
		{
            IControllingObjectBuilder builder = null;

			if (!_builderTable.ContainsKey(tagName))
			{
				IObjectBuilderProvider provider = previousBuilder as IObjectBuilderProvider;
				if (provider != null)
					builder = provider.getBuilder(context, tagName);
				if (builder == null && useDefault)
					builder = _defaultBuilder;
			}
            else
                builder = _builderTable[tagName];

            return builder;
		}

        class EmptyBuilder : IControllingObjectBuilder
		{
            public object createObject(BuildContext context)
            {
                return context.ContextObject;
            }
		}

		public IControllingObjectBuilder DefaultBuilder
		{
			get
			{
				return _defaultBuilder;
			}
		}

		public IEnumerable<KeyValuePair<string, IControllingObjectBuilder>> Builders
		{
			get
			{
				return _builderTable.AsReadOnly();
			}
		}

		public XPathNavigator RootNavigator
		{
			get
			{
				return _rootNav;
			}
			set
			{
				_rootNav = value;
			}
		}
	}

    internal class ControllingBuilderWrapper : IControllingObjectBuilder
    {
    	private IObjectBuilder _internalBuilder;

        public ControllingBuilderWrapper(IObjectBuilder aBuilder)
        {
            _internalBuilder = aBuilder;
        }

    	public IObjectBuilder BaseBuilder
    	{
    		get { return _internalBuilder; }
    	}

    	public object createObject(BuildContext context)
        {
			return _internalBuilder.createObject(context.ContextObject, context.Attributes);
        }
    }
}