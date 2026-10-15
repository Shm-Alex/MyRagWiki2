using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Web;
using Framework.Collections;
using Framework.Macro;
using Framework.Utils;

namespace Framework.ActiveTemplate
{
	public class TemplateGroupResult
	{
		public TemplateGroupResult()
		{
			Groups = new Dictionary<string, IDictionary<string, string>>();
			StoredValues = new Dictionary<ITemplateGroup, string>();
		}

		public IDictionary<string, IDictionary<string, string>> Groups
		{
			get;
			private set;
		}

		public IDictionary<ITemplateGroup, string> StoredValues
		{
			get;
			private set;
		}

		private string GetFreeName(string context, IDictionary<string, string> group)
		{
			int i = 1;
			while (group.ContainsKey(context + "_" + i))
				i++;
			return context + "_" + i;
		}

		public void Add(string name, string context, string value)
		{
			IDictionary<string, string> group;
			if (!Groups.TryGetValue(name, out group))
			{
				group = new Dictionary<string, string>();
				Groups[name] = group;
			}
			if (String.IsNullOrEmpty(context) || group.ContainsKey(context))
				context = GetFreeName(context, group);
			group[context] = value;
		}

		public ICollection<string> GetValues(string name)
		{
			IDictionary<string, string> group;
			if (!Groups.TryGetValue(name, out group))
				return new List<string>();
			return group.Values;
		}
	}

	public interface ITemplateNode
	{
		string ID{get;}
		TemplateGroupResult render(object ctx, TextWriter output);
		TemplateGroupResult render(object ctx, TextWriter output, IMacroManager macroMgr);
		void render(object ctx, TextWriter output, IMacroManager macroMgr, TemplateGroupResult groupResult);
		ITemplateGroup ParentGroup{get;}
	}

	public interface ITemplateGroup : ITemplateNode
	{
		ICollection Contents{get;}
		void addNode(ITemplateNode node);
		ITemplateNode findById(string id);
		string storedValue(TemplateGroupResult groupResult);
	}

	public abstract class TemplateNodeBase : ITemplateNode
	{
		public const string CTX_ALIAS = "ctx";
		string _id;
		ITemplateGroup _parent;

		public virtual ICollection Contents
		{
			get
			{
				return CUtils.EmptyList;
			}
		}

		public string ID
		{
			get { return _id; }
		}

		public TemplateNodeBase(string id)
		{
			_id = id;
		}

		internal void setParent(ITemplateGroup parent)
		{
			_parent = parent;
		}

		public TemplateGroupResult render(object ctx, TextWriter output)
        {
            IMacroManager macro = CreateDefaultMacroManager();

			TemplateGroupResult groupResult = new TemplateGroupResult();
			render(ctx, output, macro, groupResult);
			return groupResult;
        }
		public TemplateGroupResult render(object ctx, TextWriter output, IMacroManager macroMgr)
		{
			TemplateGroupResult groupResult = new TemplateGroupResult();
			render(ctx, output, macroMgr, groupResult);
			return groupResult;
		}

	    public static IMacroManager CreateDefaultMacroManager()
	    {
	        MacroManager macro = new MacroManager("#");
	        macro.RegisterMacro("CoreUtils", typeof(Utils.Utils));
	        macro.RegisterMacro("CUtils", typeof(Utils.CUtils));
	        macro.RegisterMacro("Lib", new StandardMacro());
	        return macro;
	    }

		public abstract void render(object ctx, TextWriter output, IMacroManager macroMgr, TemplateGroupResult groupResult);

		public ITemplateGroup ParentGroup
		{
			get { return _parent; }
		}

	}

	public class TemplateGroup : TemplateNodeBase, ITemplateGroup
	{
		IList _contents = new List();
		public string Name
		{
			get;
			set;
		}
		public string GroupContext
		{
			get;
			set;
		}

		public TemplateGroup(string id) : base(id)
		{
		}
		public TemplateGroup(string id, string name, string context) : base(id)
		{
			Name = name;
			GroupContext = context;
		}

		public override ICollection Contents
		{
			get
			{
				return _contents;
			}
		}

		public void addNode(ITemplateNode node)
		{
			if(node == null)
				throw new ArgumentNullException("node");
			_contents.Add(node);
			((TemplateNodeBase)node).setParent(this);
		}

		public virtual ITemplateNode findById(string id)
		{
			if(this.ID == id)
				return this;
			if(ParentGroup != null)
				return ParentGroup.findById(id);
			return processDescFind(this, id);
		}

		ITemplateNode processDescFind(ITemplateNode node, string id)
		{
			if(node.ID == id)
				return node;
			ITemplateGroup group = node as ITemplateGroup;
			ITemplateNode foundNode = null;
			if(group != null)
				foreach(ITemplateNode child in group.Contents)
				{
					foundNode = processDescFind(child, id);
					if(foundNode != null)
						break;
				}
			return foundNode;
		}

		public override void render(object ctx, TextWriter output, IMacroManager macroMgr, TemplateGroupResult groupResult)
		{
			StringBuilder builder = new StringBuilder();
			TextWriter writer = new StringWriter(builder);
			foreach (ITemplateNode node in Contents)
			{
				node.render(ctx, writer, macroMgr, groupResult);
			}
			output.Write(builder);
			setStoredValue(groupResult, builder.ToString());

			object nameObject = !String.IsNullOrEmpty(Name) ? macroMgr.ParseString(Name) : null;
			if (nameObject != null)
			{
				string nameString = nameObject.ToString();
				if (!String.IsNullOrEmpty(nameString))
				{
					groupResult.Add(nameString, GroupContext, storedValue(groupResult));
				}
			}
		}

		public string storedValue(TemplateGroupResult groupResult)
		{
			string value;
			if (!groupResult.StoredValues.TryGetValue(this, out value))
				throw new ApplicationException("Call to storedValue.get before storedValue.set");
			return value;
		}

		protected void setStoredValue(TemplateGroupResult groupResult, string value)
		{
			groupResult.StoredValues[this] = value;
		}

		public override string ToString()
		{
			return "Статическая группа (ID: " + ID + ")";
		}

	}

	public class CommentTemplateGroup : TemplateGroup
	{
		public CommentTemplateGroup(): base("")
		{
		}

		public override void render(object ctx, TextWriter output, IMacroManager macroMgr, TemplateGroupResult groupResult)
		{
		}
	}

    public class TemplateGroupWithIncludes : TemplateGroup
    {
        List _includes = new List();

//        public TemplateGroupWithIncludes(string aId) : base(aId)
//        {
//        }

        public TemplateGroupWithIncludes(ITemplateNode contents) : base("")
        {
            addNode(contents);
        }

        public void AddInclude(ITemplateNode node)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            _includes.Add(node);
        }

        public override ITemplateNode findById(string id)
        {
            ITemplateNode res = null;
            foreach (ITemplateNode node in _includes)
            {
                if (node.ID == id)
                {
                    res = node;
                    break;
                }
            }
            if (res == null)
                res = base.findById(id);

            return res;
        }
    }

    public class ConditionTemplateGroup : TemplateGroup
	{
		private string _value;
		public ConditionTemplateGroup(string id, string value) : base(id)
		{
			_value = value;
		}

		public string Value
		{
			get { return _value; }
			set { _value = value; }
		}

		public override void render(object ctx, TextWriter realOutput, IMacroManager macroMgr, TemplateGroupResult groupResult)
		{
			StringBuilder builder = new StringBuilder();
			TextWriter output = new StringWriter(builder);
			try
			{
				object res = macroMgr.ParseString(_value, CTX_ALIAS, ctx);
				bool condition = res is bool ? (bool) res : false;
				if (condition)
				{
					base.render(ctx, output, macroMgr, groupResult);
				}
			}
			finally
			{
				realOutput.Write(builder);
				setStoredValue(groupResult, builder.ToString());
			}
		}
	}
	public class ActiveTemplateGroup : TemplateGroup
	{
		string _expr;
	    private string _preRenderMacro;
	    private string _postRenderMacro;
	    private string _innerMacro;
		private string _always;

		public ActiveTemplateGroup(string id, string expr) : base(id)
		{
			_expr = expr;
		}

	    public string PreRenderMacro
	    {
	        get { return _preRenderMacro; }
	        set { _preRenderMacro = value; }
	    }

	    public string PostRenderMacro
	    {
	        get { return _postRenderMacro; }
	        set { _postRenderMacro = value; }
	    }

	    public string InnerMacro
	    {
	        get { return _innerMacro; }
	        set { _innerMacro = value; }
	    }

		public string Always
		{
			get { return _always; }
			set { _always = value; }
		}

		public override void render(object ctx, TextWriter realOutput, IMacroManager macroMgr, TemplateGroupResult groupResult)
		{
			StringBuilder builder = new StringBuilder();
			TextWriter output = new StringWriter(builder);
			try
			{
				bool always = _always != null;
				if (always)
					renderMacro(ctx, output, macroMgr, PreRenderMacro, groupResult);

				if (string.IsNullOrEmpty(_expr))
					base.render(ctx, output, macroMgr, groupResult);
				else
				{
					object res = macroMgr.ParseString(_expr, CTX_ALIAS, ctx);
					IEnumerable ctxList = res as IEnumerable;
					if (ctxList == null)
						base.render(res, output, macroMgr, groupResult);
					else
					{
						bool inner = false;
						foreach (object newCtx in ctxList)
						{
							if (inner)
								renderMacro(ctx, output, macroMgr, InnerMacro, groupResult);
							else
							{
								inner = true;
								if (!always)
									renderMacro(ctx, output, macroMgr, PreRenderMacro, groupResult);
							}

							string alias = CTX_ALIAS;
							int i = 1;
							while (macroMgr.ContainsMacro(alias + i))
								i++;
							alias += i;
							try
							{
								macroMgr.RegisterMacro(alias, ctx);
								base.render(newCtx, output, macroMgr, groupResult);
							}
							finally
							{
								macroMgr.DropMacro(alias);
							}
						}
						if (!always && inner)
							renderMacro(ctx, output, macroMgr, PostRenderMacro, groupResult);
					}
				}
				if (always)
					renderMacro(ctx, output, macroMgr, PostRenderMacro, groupResult);
			}
			finally
			{
				realOutput.Write(builder);
				setStoredValue(groupResult, builder.ToString());
			}
		}

	    private void renderMacro(object ctx, TextWriter output, IMacroManager macroMgr, string macroText, TemplateGroupResult groupResult)
	    {
            if (string.IsNullOrEmpty(macroText))
                return;
	        MacroTemplate macroTemplate;
	        macroTemplate = new MacroTemplate("", macroText);
			macroTemplate.render(ctx, output, macroMgr, groupResult);
	    }

	    public override string ToString()
		{
			return "Контекстная группа (ID: " + ID + ")";
		}

	}

	public class TextTemplate : TemplateNodeBase
	{
		protected string _text;
		bool _htmlEncode = false;

		public TextTemplate(string text) : base("")
		{
			_text = text;
		}

		public TextTemplate(string id, string text) : base(id)
		{
			_text = text;
		}

		public override void render(object ctx, TextWriter output, IMacroManager macroMgr, TemplateGroupResult groupResult)
		{
//			string res = UseHtmlEncoding ? HttpUtility.HtmlEncode(_text) : _text;
//			output.Write(res);

            object res = null;
            if (!string.IsNullOrEmpty(_text))
                res = macroMgr.ParseString(_text, CTX_ALIAS, ctx);
            if (res != null)
            {
                string str = UseHtmlEncoding ? HttpUtility.HtmlEncode(res.ToString()) : res.ToString();
                output.Write(str);
            }

		}

		public bool UseHtmlEncoding
		{
			get
			{
				return _htmlEncode;
			}
			set
			{
				_htmlEncode = value;
			}
		}

		public override string ToString()
		{
			return this._text;
		}
	}

	public class TemplateReference : TemplateNodeBase
	{
		string _refId;
		bool _recalc = true;
		public TemplateReference(string refId) : base("")
		{
			if(Utils.Utils.isEmpty(refId))
                throw new ArgumentNullException("refId");
			_refId = refId;
		}
		public TemplateReference(string refId, string recalcString) : this(refId)
		{
			bool parsedRecalc;
			if (Boolean.TryParse(recalcString, out parsedRecalc))
				_recalc = parsedRecalc;
		}

		public override void render(object ctx, TextWriter output, IMacroManager macroMgr, TemplateGroupResult groupResult)
		{
			if(ParentGroup != null)
			{
				ITemplateNode refTemplate = ParentGroup.findById(_refId);
				if (refTemplate != null)
				{
					if (!_recalc && refTemplate is ITemplateGroup)
						output.Write((refTemplate as ITemplateGroup).storedValue(groupResult));
					else
						refTemplate.render(ctx, output, macroMgr, groupResult);
				}
			}
		}

		public override string ToString()
		{
			return "Ссылка на шаблон c ID: " + _refId;
		}

	}

	public class MacroTemplate : TextTemplate
	{
		public MacroTemplate(string id, string text) : base(id, text)
		{
			if(Utils.Utils.isEmpty(text))
				throw new ArgumentNullException("text");
		}

		public override string ToString()
		{
			return "Макро[" + _text + "]";
		}

	}

	public class XmlElementTemplate : TemplateGroup
	{
		string _tagName;
		IDictionary _attList = new ListDictionary();

		public XmlElementTemplate(string tagName) : base("")
		{
			if(Utils.Utils.isEmpty(tagName))
				throw new ArgumentNullException("tagName");
			_tagName = tagName;
		}

		public void addAttributeTemplate(string attName, ITemplateNode attValue)
		{
			if(Utils.Utils.isEmpty(attName))
				throw new ArgumentNullException("attName");
			_attList.Add(attName, attValue);
		}

		public override void render(object ctx, TextWriter realOutput, IMacroManager macroMgr, TemplateGroupResult groupResult)
		{
			StringBuilder builder = new StringBuilder();
			TextWriter output = new StringWriter(builder);
			try
			{
				output.Write("<" + _tagName + " ");
				foreach (DictionaryEntry de in _attList)
				{
					ITemplateNode attNode = (ITemplateNode)de.Value;
					string attName = (string)de.Key;
					output.Write(attName + "=\"");
					attNode.render(ctx, output, macroMgr, groupResult);
					output.Write("\"");
				}
				output.Write(">");
				base.render(ctx, output, macroMgr, groupResult);
				output.Write("</" + _tagName + ">");
			}
			finally
			{
				realOutput.Write(builder);
				setStoredValue(groupResult, builder.ToString());
			}
		}

		public override string ToString()
		{
			string s = "Xml<" + _tagName + ">[";
			foreach(DictionaryEntry de in _attList)
			{
				ITemplateNode attNode = (ITemplateNode) de.Value;
				string attName = (string) de.Key;
				s += attName + "=" + attNode.ToString() + "; ";
			}
			return s + "]";
		}
	}
}
