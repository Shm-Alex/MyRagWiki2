using System;
using System.Reflection;
using System.Collections;
using Framework.Collections;

namespace Ksema.ObjectModel
{
    public class CSLinkPropertyProxy : CSPropertyProxyBase, ICSLinkInfo
    {
        bool _isComposite;
        private string _backRefName;
        public CSLinkPropertyProxy(object _owner, PropertyInfoEntry entry)
            : base(_owner, entry.PropertyInfo.Name, entry.PropertyInfo.Required)
        {
            _isComposite = ((ICSLinkInfo)entry.PropertyInfo).IsComposite;
            _backRefName = ((ICSLinkInfo)entry.PropertyInfo).BackRefName;
        }


        public bool IsComposite
        {
            get { return _isComposite; }
        }
        public string BackRefName
        {
            get { return _backRefName; }
        }

        public ICSLink getBackLink(object linkedObj)
        {
            if (BackRefName != null && linkedObj != null)
                return CSPropMgr.Instance.getLink(BackRefName, linkedObj);
            else
                return null;
        }

    }

    public class CSMultiLinkProxy : CSLinkPropertyProxy, ICSMultiLink
    {
        public CSMultiLinkProxy(object _owner, PropertyInfoEntry entry)
            : base(_owner, entry)
        {
        }

        internal static IList GetListFromValue(object pv)
        {
            IList list = pv as IList;
            if (list != null)
                return list;

            ICollection col = pv as ICollection;
            if (col != null)
            {
                list = new ArrayList(col);
                return list;
            }

            IEnumerable it = (IEnumerable)pv;
            List linkedList = new List();
            foreach (object item in it)
                linkedList.Add(item);

            return linkedList;
        }

        protected virtual IList getListImpl()
        {
            return null;
        }
        public void addLinkedObject(object obj)
        {
            IList list = getListImpl();
            if (list != null)
            {
                list.Add(obj);
            }
        }

        public void removeLinkedObject(object obj)
        {
            IList list = getListImpl();
            if (list != null)
            {
                list.Remove(obj);
            }
        }

        public void removeAll()
        {
            IList list = getListImpl();
            if (list != null)
            {
                list.Clear();
            }
        }

        public ICollection getLinkedObjects()
        {
            IList list = getListImpl();
            return list;
        }
    }

    public class CSMultiLinkPropertyProxy : CSMultiLinkProxy, ICSMultiLink
    {
        PropertyInfo _pi;

        public CSMultiLinkPropertyProxy(object _owner, PropertyInfoEntry entry)
            : base(_owner, entry)
        {
            _pi = (PropertyInfo)entry.MemberInfo;
        }


        protected override IList getListImpl()
        {
            object pv = _pi.GetValue(Owner, null);
            if (pv == null)
                throw new InvalidOperationException("property value is null");
            IList list = GetListFromValue(pv);
            return list;
        }
    }

    public class CSMultiLinkFieldProxy : CSMultiLinkProxy, ICSMultiLink
    {
        FieldInfo _pi;

        public CSMultiLinkFieldProxy(object _owner, PropertyInfoEntry entry)
            : base(_owner, entry)
        {
            _pi = (FieldInfo)entry.MemberInfo;
        }


        protected override IList getListImpl()
        {
            object pv = _pi.GetValue(Owner);
            if (pv == null)
                throw new InvalidOperationException("field value is null");
            IList list = GetListFromValue(pv);
            return list;
        }

    }

    public class CSSingleLinkPropertyProxy : CSLinkPropertyProxy, ICSSingleLink
    {
        PropertyInfo _pi;

        public CSSingleLinkPropertyProxy(object _owner, PropertyInfoEntry entry)
            : base(_owner, entry)
        {
            _pi = (PropertyInfo)entry.MemberInfo;
        }

        public bool setObject(object obj)
        {
            _pi.SetValue(Owner, obj, null);
            return true;
        }

        public object getObject()
        {
            return _pi.GetValue(Owner, null);
        }


        ICollection ICSLink.getLinkedObjects()
        {
            object linked = getObject();
            return linked == null ? new object[0] : new[] { linked };
        }
    }

    public class CSSingleLinkFieldProxy : CSLinkPropertyProxy, ICSSingleLink
    {
        FieldInfo _pi;

        public CSSingleLinkFieldProxy(object _owner, PropertyInfoEntry entry)
            : base(_owner, entry)
        {
            _pi = (FieldInfo)entry.MemberInfo;
        }

        public bool setObject(object obj)
        {
            _pi.SetValue(Owner, obj);
            return true;
        }

        public object getObject()
        {
            return _pi.GetValue(Owner);
        }

        ICollection ICSLink.getLinkedObjects()
        {
            object linked = getObject();
            return linked == null ? new object[0] : new[] { linked };
        }

    }
}
