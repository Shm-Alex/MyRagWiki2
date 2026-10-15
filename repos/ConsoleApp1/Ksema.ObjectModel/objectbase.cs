using System;
using System.Collections;
using System.Collections.Generic;
using Framework.Collections;
using Framework.Utils;

namespace Ksema.ObjectModel
{
    [Serializable]
    public class CSObjectBase : ICSObject, IComparable
    {
        object _key;

        public CSObjectBase()
        {
            //TODO: Использовать KeyProvider
            _key = "_" + Guid.NewGuid();
        }

        public CSObjectBase(object _key)
        {
            this._key = _key;
        }

        public virtual int CompareTo(object o)
        {
            if (o == null)
                return 1;
            ISimpleObject obj = (ISimpleObject)o;
            return ((IComparable)_key).CompareTo(obj.Key);
        }

        public override bool Equals(object obj)
        {
            ISimpleObject s_obj = obj as ISimpleObject;
            if (s_obj == null)
                return false;
            return this._key.Equals(s_obj.Key);
        }

        public override int GetHashCode()
        {
            return _key.GetHashCode();
        }

        public object Key
        {
            get
            {
                return _key;
            }
        }


        public event LinkChangedEventHandler LinkAdded;
        public event LinkChangedEventHandler LinkDeleted;
        public event ValueChangedEventHandler ValueChanged;

        void ITrackChanges.SetupValueChanges(INotifyValueChanged eventSource)
        {
            eventSource.ValueChanged += new ValueChangedEventHandler(eventSource_ValueChanged);
        }

        void ITrackChanges.SetupLinkChanges(INotifyLinkChanged eventSource)
        {
            eventSource.LinkAdded += new LinkChangedEventHandler(eventSource_LinkAdded);
            eventSource.LinkDeleted += new LinkChangedEventHandler(eventSource_LinkDeleted);
        }

        void ITrackChanges.SetupSingleLinkChanges(INotifyValueChanged eventSource)
        {
            
        }

        void eventSource_ValueChanged(object sender, ValueChangedEventArgs args)
        {
            if (ValueChanged != null)
                ValueChanged(this, args);
        }

        void eventSource_LinkDeleted(object sender, LinkChangedEventArgs args)
        {
            if (LinkDeleted != null)
                LinkDeleted(this, args);
        }

        void eventSource_LinkAdded(object sender, LinkChangedEventArgs args)
        {
            if (LinkAdded != null)
                LinkAdded(this, args);
        }
    }

    [Serializable]
    public class SimpleKeyIndexer : IIndexer
    {
        public object GetIndexKey(object o)
        {
            return ((ISimpleObject)o).Key;
        }
    }
}
