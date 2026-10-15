using System;
using System.Collections;
using System.IO;
using System.Xml;
using Framework.Utils;
using Framework.Macro;

namespace Serializer
{
	public class XmlResourceObjectsProvider : EmptyObjectsProvider
	{
		MetaData _md;

		public XmlResourceObjectsProvider(MetaData _md)
		{
			this._md = _md;
		}

		public XmlResourceObjectsProvider(IExternalObjectsProvider _next, MetaData _md) : base(_next)
		{
			this._md = _md;
		}

		public override IExternalObjects getInstance(XmlElement node)
		{
			return node.HasAttribute("res") ? new XmlResExternalObjects(_md, node.GetAttribute("res")) : null;
		}
	}


	public class XmlResExternalObjects : IExternalObjects
	{
		MetaData _md;
		string _resName;

		public XmlResExternalObjects(MetaData _md, string resName)
		{
			this._md = _md;
			_resName = resName;
		}

		public ICollection getObjects()
		{
            return CSSerializer.DeserializeFromResource(_resName, _md);
		}

	}

    public class MacroObjectsProvider : EmptyObjectsProvider
    {
        IMacroManager _mgr;

        public MacroObjectsProvider(IExternalObjectsProvider next, IMacroManager mgr)
            : base(next)
        {
            _mgr = mgr;
        }

        public MacroObjectsProvider(IMacroManager mgr)
            : this(null, mgr)
        {
        }

        public override IExternalObjects getInstance(XmlElement node)
        {
            return node.HasAttribute("macro") ? new MacroExternalObjects(_mgr, node.GetAttribute("macro")) : null;
        }
    }

    public class MacroExternalObjects : IExternalObjects
    {
        string _macro;
        IMacroManager _mgr;

        public MacroExternalObjects(IMacroManager mgr, string macro)
        {
            _macro = macro;
            _mgr = mgr;
        }

        public ICollection getObjects()
        {
            return (ICollection)_mgr.ParseString(_macro);
        }
    }
}