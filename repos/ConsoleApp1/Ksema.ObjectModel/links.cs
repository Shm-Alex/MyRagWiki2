using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Data;
using Framework.Collections;
using Framework.Utils;

namespace Ksema.ObjectModel
{
    [Serializable]
    public class LinkInfo : ICSLinkInfo
    {
        private readonly bool _isComposite;
        private readonly string _backRefName;
        private readonly string _name;

        public LinkInfo(string aName, bool aIsComposite, string aBackRefName)
        {
            _name = aName;
            _backRefName = aBackRefName;
            _isComposite = aIsComposite;
        }

        public bool IsComposite
        {
            get { return _isComposite; }
        }

        public string BackRefName
        {
            get { return _backRefName; }
        }

        public string Name
        {
            get { return _name; }
        }

        public bool Required
        {
            get { return false; }
        }
    }

    [Serializable]
    public abstract class CSLinkBase<TOwner, TLinked> : ICSLink, ICSLink<TOwner, TLinked>, INotifyLinkChanged
        where TOwner : class
        where TLinked : class
    {
        private readonly TOwner _owner;
        private readonly string _refName;
        private readonly string _backRefName;
        private readonly bool _composite;

        protected CSLinkBase(string refName, string backRefName, TOwner owner)
            : this(refName, backRefName, false, owner)
        {
        }


        public abstract ICollection getLinkedObjects();

        protected CSLinkBase(string refName, string backRefName, bool isComposite, TOwner owner)
        {
            if (Utils.isEmpty(refName) || owner == null)
                throw new ArgumentNullException();

            _refName = refName;
            _backRefName = backRefName;
            _owner = owner;
            _composite = isComposite;

            if (_owner is ITrackChanges)
            {
                ((ITrackChanges)_owner).SetupLinkChanges(this);
            }
        }

        public ICSLink<TLinked, TOwner> getBackLink(TLinked obj)
        {
            ICSLink<TLinked, TOwner> backLink = null;
            if (obj != null && !Utils.isEmpty(_backRefName))
                backLink = (ICSLink<TLinked, TOwner>)CSPropMgr.Instance.getLink(_backRefName, obj);
            return backLink;
        }


        ICSLink ICSLink.getBackLink(object obj)
        {
            ICSLink backLink = null;
            if (obj != null && !Utils.isEmpty(_backRefName))
                backLink = CSPropMgr.Instance.getLink(_backRefName, obj);
            return backLink;
        }

        object ICSLink.Owner
        {
            get
            {
                return _owner;
            }
        }

        public abstract void processBackRefAdd(ICSLink<TLinked, TOwner> origLink);
        public abstract void processBackRefRemove(ICSLink<TLinked, TOwner> origLink);

        public bool IsComposite
        {
            get { return _composite; }
        }

        public bool Required { get; set; }

        public string Name
        {
            get { return _refName; }
        }
        public string BackRefName
        {
            get { return _backRefName; }
        }

        public TOwner Owner
        {
            get { return _owner; }
        }

        protected void OnLinkedObjectAdded(object obj)
        {
            if (LinkAdded != null)
                LinkAdded(this, createRefArgs(obj));
        }

        protected void OnLinkedObjectRemoved(object obj)
        {
            if (LinkDeleted != null)
                LinkDeleted(this, createRefArgs(obj));
        }

        LinkChangedEventArgs createRefArgs(object obj)
        {
            return new LinkChangedEventArgs(_refName, Owner, obj);
        }

        public event LinkChangedEventHandler LinkAdded;
        public event LinkChangedEventHandler LinkDeleted;
    }

    [Serializable]
    public abstract class CSMultiLinkBase<TOwner, TLinked> : CSLinkBase<TOwner, TLinked>, ICSMultiLink
        where TOwner : class
        where TLinked : class
    {
        protected CSMultiLinkBase(string aRefName, string backRefName, TOwner owner)
            : base(aRefName, backRefName, owner)
        {
        }

        protected CSMultiLinkBase(string aRefName, string backRefName, bool isComposite, TOwner owner)
            : base(aRefName, backRefName, isComposite, owner)
        {
        }


        protected abstract bool canAddLinkedObject(TLinked obj);
        protected abstract bool canRemoveLinkedObject(TLinked obj);
        protected abstract void addLinkedObjectImpl(TLinked obj);
        protected abstract void removeLinkedObjectImpl(TLinked obj);

        public void removeAll()
        {
            ICollection linkedObjects = getLinkedObjects();
            TLinked[] tempList = new TLinked[linkedObjects.Count];
            linkedObjects.CopyTo(tempList, 0);
            foreach (TLinked obj in tempList)
                RemoveLinked(obj);
        }

        protected void AddLinked(TLinked obj)
        {
            if (!canAddLinkedObject(obj))
                return;
            ICSLink<TLinked, TOwner> backRef = getBackLink(obj);
            if (backRef != null)
                backRef.processBackRefAdd(this);
            addLinkedObjectImpl(obj);
        }

        protected void RemoveLinked(TLinked obj)
        {
            if (!canRemoveLinkedObject(obj))
                return;
            ICSLink<TLinked, TOwner> backRef = getBackLink(obj);
            if (backRef != null)
                backRef.processBackRefRemove(this);
            removeLinkedObjectImpl(obj);
        }

        void ICSMultiLink.addLinkedObject(object obj)
        {
            AddLinked((TLinked)obj);
        }

        void ICSMultiLink.removeLinkedObject(object obj)
        {
            RemoveLinked((TLinked)obj);
        }

        public override void processBackRefAdd(ICSLink<TLinked, TOwner> origLink)
        {
            TLinked addObj = origLink.Owner;
            if (!canAddLinkedObject(addObj))
                throw new ArgumentException("cannot add linked object: " + addObj, "origLink");
            addLinkedObjectImpl(addObj);
        }

        public override void processBackRefRemove(ICSLink<TLinked, TOwner> origLink)
        {
            TLinked remObj = origLink.Owner;
            removeLinkedObjectImpl(remObj);
        }

    }

    [Serializable]
    public class CSMultiLink<TOwner, TLinked> : CSMultiLinkBase<TOwner, TLinked>, ICSMultiLink<TOwner, TLinked>, INotifyCollectionChanged, ICollectionViewFactory
        where TOwner : class
        where TLinked : class
    {
        readonly List<TLinked> _linkedObjects;
        readonly HashSet<TLinked> _linkedSet;

        public CSMultiLink(string aRefName, string backRefName, TOwner owner)
            : this(aRefName, backRefName, false, owner)
        {
        }

        public CSMultiLink(string aRefName, string backRefName, bool isComposite, TOwner owner)
            : this(aRefName, backRefName, isComposite, owner, EqualityComparer<TLinked>.Default)
        {
        }

        public CSMultiLink(string aRefName, string backRefName, bool isComposite, TOwner owner, IEqualityComparer<TLinked> linkedCmp)
            : base(aRefName, backRefName, isComposite, owner)
        {
            _linkedObjects = new List<TLinked>(10);
            _linkedSet = new HashSet<TLinked>(linkedCmp);
        }

        protected override bool canAddLinkedObject(TLinked obj)
        {
            if (obj == null)
                return false;
            if (_linkedSet.Contains(obj))
                return false;
            return true;
        }
        protected override bool canRemoveLinkedObject(TLinked obj)
        {
            if (obj == null)
                return false;
            if (!_linkedSet.Contains(obj))
                return false;
            return true;
        }


        public bool containsObject(TLinked obj)
        {
            return _linkedSet.Contains(obj);
        }

        protected override void addLinkedObjectImpl(TLinked obj)
        {
            if (!canAddLinkedObject(obj))
                return;

            _linkedSet.Add(obj);
            _linkedObjects.Add(obj);

            OnLinkedObjectAdded(obj);

            if (ListChanged != null)
                ListChanged(this, new ListChangedEventArgs(ListChangedType.ItemAdded, _linkedObjects.Count - 1));
            if (CollectionChanged != null)
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, obj, _linkedObjects.Count - 1));
        }

        protected override void removeLinkedObjectImpl(TLinked obj)
        {
            if (!canRemoveLinkedObject(obj))
                return;

            //TODO: store list indexes in dictionary for fast removing
            int index = _linkedObjects.IndexOf(obj);

            _linkedSet.Remove(obj);
            _linkedObjects.RemoveAt(index);

            OnLinkedObjectRemoved(obj);

            if (ListChanged != null)
                ListChanged(this, new ListChangedEventArgs(ListChangedType.ItemDeleted, index));
            if (CollectionChanged != null)
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, obj, index));
        }

        public override ICollection getLinkedObjects()
        {
            return _linkedObjects.AsReadOnly();
        }


        public void addLinkedObject(TLinked obj)
        {
            AddLinked(obj);
        }

        public void removeLinkedObject(TLinked obj)
        {
            RemoveLinked(obj);
        }

        #region IList<TLinked>

        public int IndexOf(TLinked item)
        {
            return _linkedObjects.IndexOf(item);
        }

        void IList<TLinked>.Insert(int index, TLinked item)
        {
            throw new NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            TLinked item = _linkedObjects[index];
            RemoveLinked(item);
        }

        public TLinked this[int index]
        {
            get { return _linkedObjects[index]; }
            set
            {
                throw new NotSupportedException();
            }
        }

        public void Add(TLinked item)
        {
            AddLinked(item);
        }

        public void Clear()
        {
            removeAll();
        }

        public bool Contains(TLinked item)
        {
            return _linkedSet.Contains(item);
        }

        public void CopyTo(TLinked[] array, int arrayIndex)
        {
            _linkedObjects.CopyTo(array, arrayIndex);
        }

        public bool Remove(TLinked item)
        {
            RemoveLinked(item);
            return true;
        }

        public int Count
        {
            get { return _linkedObjects.Count; }
        }

        bool ICollection<TLinked>.IsReadOnly
        {
            get { return false; }
        }

        public IEnumerator<TLinked> GetEnumerator()
        {
            return _linkedObjects.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region INotifyCollectionChanged Members

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion

        #region IBindingList

        public event ListChangedEventHandler ListChanged;

        object IBindingList.AddNew()
        {
            throw new NotSupportedException();
        }

        void IBindingList.AddIndex(PropertyDescriptor property)
        {
            throw new NotSupportedException();
        }

        void IBindingList.ApplySort(PropertyDescriptor property, ListSortDirection direction)
        {
            throw new NotSupportedException();
        }

        int IBindingList.Find(PropertyDescriptor property, object key)
        {
            throw new NotSupportedException();
        }

        void IBindingList.RemoveIndex(PropertyDescriptor property)
        {
            throw new NotSupportedException();
        }

        void IBindingList.RemoveSort()
        {
            throw new NotSupportedException();
        }

        bool IBindingList.AllowNew
        {
            get { return false; }
        }

        bool IBindingList.AllowEdit
        {
            get { return true; }
        }

        bool IBindingList.AllowRemove
        {
            get { return true; }
        }

        bool IBindingList.SupportsChangeNotification
        {
            get { return true; }
        }

        bool IBindingList.SupportsSearching
        {
            get { return false; }
        }

        bool IBindingList.SupportsSorting
        {
            get { return false; }
        }

        bool IBindingList.IsSorted
        {
            get { return false; }
        }

        PropertyDescriptor IBindingList.SortProperty
        {
            get { throw new NotSupportedException(); }
        }

        ListSortDirection IBindingList.SortDirection
        {
            get { throw new NotSupportedException(); }
        }

        int IList.Add(object value)
        {
            Add((TLinked)value);
            return _linkedObjects.Count - 1;
        }

        bool IList.Contains(object value)
        {
            return Contains((TLinked)value);
        }

        int IList.IndexOf(object value)
        {
            return IndexOf((TLinked)value);
        }

        void IList.Insert(int index, object value)
        {
            throw new NotSupportedException();
        }

        void IList.Remove(object value)
        {
            Remove((TLinked)value);
        }

        object IList.this[int index]
        {
            get { return this[index]; }
            set { this[index] = (TLinked)value; }
        }

        bool IList.IsReadOnly
        {
            get { return false; }
        }

        bool IList.IsFixedSize
        {
            get { return false; }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            for (int i = 0; i < Count; i++)
                array.SetValue(this[i], i + index);
        }

        readonly object _syncRoot = new object();
        object ICollection.SyncRoot
        {
            get { return _syncRoot; }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        #endregion

        #region ICollectionViewFactory Members

        /// <summary>
        /// Супер гипер метод, который решает много проблем в WPF
        /// </summary>
        /// <returns></returns>
        public ICollectionView CreateView()
        {
            return new ListCollectionView(this);
        }

        #endregion
    }

    [Serializable]
    public class CSKeyMultiLink<TOwner, TLinked, TKey, TIndexer> : CSMultiLinkBase<TOwner, TLinked>, ICSKeyMultiLink<TOwner, TLinked, TKey, TIndexer>
        where TOwner : class
        where TLinked : class
        where TIndexer : IIndexer<TLinked, TKey>, new()
    {
        readonly Dictionary<TKey, TLinked> _linkedObjects;
        readonly TIndexer _indexer;

        public CSKeyMultiLink(string aRefName, string backRefName, bool isComposite, TOwner owner)
            : base(aRefName, backRefName, isComposite, owner)
        {
            _indexer = new TIndexer();
            _linkedObjects = new Dictionary<TKey, TLinked>(_indexer.GetKeyComparer());
        }

        public CSKeyMultiLink(string aRefName, string backRefName, TOwner owner)
            : this(aRefName, backRefName, false, owner)
        {
        }


        public void addLinkedObject(TLinked obj)
        {
            AddLinked(obj);
        }

        public void removeLinkedObject(TLinked obj)
        {
            RemoveLinked(obj);
        }


        public void removeObjectByKey(TKey key)
        {
            removeObjectByKeyImpl(key, true);
        }

        #region IDictionary<>

        public bool ContainsKey(TKey key)
        {
            return _linkedObjects.ContainsKey(key);
        }

        public void Add(TKey key, TLinked value)
        {
            CheckObjectKey(key, value);
            AddLinked(value);
        }

        private void CheckObjectKey(TKey key, TLinked value)
        {
            TKey valueKey = _indexer.GetIndexKey(value);
            if (!Equals(key, valueKey))
                throw new InvalidOperationException("Given key must match value's key");
        }

        public bool Remove(TKey key)
        {
            removeObjectByKey(key);
            return true;
        }

        public bool TryGetValue(TKey key, out TLinked value)
        {
            return _linkedObjects.TryGetValue(key, out value);
        }

        public TLinked this[TKey key]
        {
            get { return _linkedObjects[key]; }
            set
            {
                TLinked oldVal;
                if (_linkedObjects.TryGetValue(key, out oldVal))
                    throw new NotSupportedException("Object already exists. Key: " + key);
                Add(key, value);
            }
        }

        public ICollection<TKey> Keys
        {
            get { return _linkedObjects.Keys; }
        }

        public ICollection<TLinked> Values
        {
            get { return _linkedObjects.Values; }
        }

        void ICollection<KeyValuePair<TKey, TLinked>>.Add(KeyValuePair<TKey, TLinked> item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            removeAll();
        }

        bool ICollection<KeyValuePair<TKey, TLinked>>.Contains(KeyValuePair<TKey, TLinked> item)
        {
            return ((ICollection<KeyValuePair<TKey, TLinked>>)_linkedObjects).Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TLinked>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TKey, TLinked>>)_linkedObjects).CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<TKey, TLinked>>.Remove(KeyValuePair<TKey, TLinked> item)
        {
            throw new NotSupportedException();
        }

        public int Count
        {
            get { return _linkedObjects.Count; }
        }

        bool ICollection<KeyValuePair<TKey, TLinked>>.IsReadOnly
        {
            get { return false; }
        }

        IEnumerator<KeyValuePair<TKey, TLinked>> IEnumerable<KeyValuePair<TKey, TLinked>>.GetEnumerator()
        {
            return _linkedObjects.GetEnumerator();
        }

        public IEnumerator GetEnumerator()
        {
            return _linkedObjects.GetEnumerator();
        }

        #endregion

        #region ICSMultiLink Members

        protected override bool canAddLinkedObject(TLinked obj)
        {
            if (obj == null)
                return false;
            TKey key = _indexer.GetIndexKey(obj);
            if ( /*key == null ||*/ _linkedObjects.ContainsKey(key))
                return false;
            return true;
        }
        protected override void addLinkedObjectImpl(TLinked obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            TKey key = _indexer.GetIndexKey(obj);
            //if( key == null )
            //    throw new InvalidOperationException("object index key cannot be null");
            _linkedObjects.Add(key, obj);
            OnLinkedObjectAdded(obj);
        }

        protected override bool canRemoveLinkedObject(TLinked obj)
        {
            if (obj == null)
                return false;
            TKey key = _indexer.GetIndexKey(obj);
            //			if(key == null)
            //				return false;
            if (!ReferenceEquals(_linkedObjects[key], obj))
                return false;
            return true;
        }


        void removeObjectByKeyImpl(TKey key, bool processBackRef)
        {
            //			if(key == null)
            //				throw new ArgumentNullException("key");

            TLinked obj = _linkedObjects[key];
            if (obj == null)
                return;

            if (processBackRef)
            {
                ICSLink<TLinked, TOwner> backRef = getBackLink(obj);
                if (backRef != null)
                    backRef.processBackRefRemove(this);
            }

            _linkedObjects.Remove(key);
            OnLinkedObjectRemoved(obj);
        }

        protected override void removeLinkedObjectImpl(TLinked obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            removeObjectByKeyImpl(_indexer.GetIndexKey(obj), false);
        }


        public override ICollection getLinkedObjects()
        {
            return _linkedObjects.Values;
        }

        #endregion
    }

    [Serializable]
    public class CSSingleLink<TOwner, TLinked> : CSLinkBase<TOwner, TLinked>, ICSSingleLink, ICSSingleLink<TOwner, TLinked>, INotifyValueChanged
        where TOwner : class
        where TLinked : class
    {
        TLinked _value;

        public CSSingleLink(string refName, string backRefName, TOwner owner)
            : this(refName, backRefName, false, owner)
        {
        }
        public CSSingleLink(string refName, string backRefName, bool isComposite, TOwner owner)
            : base(refName, backRefName, isComposite, owner)
        {
            if (owner is ITrackChanges)
                ((ITrackChanges)owner).SetupSingleLinkChanges(this);
        }

        public virtual TLinked LinkedObject
        {
            get { return _value; }
            set
            {
                SetObject(value);
            }
        }


        public override ICollection  getLinkedObjects()
        {
            return LinkedObject == null ? new object[0] : new object[] { LinkedObject };
        }

        private bool SetObject(TLinked value)
        {
            if (!canSetObject(value))
                return false;

            ICSLink<TLinked, TOwner> backRef = getBackLink(value);
            if (backRef != null)
                backRef.processBackRefAdd(this);

            return setObject(value, true);//если это упадет... все будет очень плохо

        }

        bool ICSSingleLink.setObject(object obj)
        {
            return SetObject((TLinked)obj);
        }

        object ICSSingleLink.getObject()
        {
            return _value;
        }

        bool canSetObject(TLinked obj)
        {
            if (ReferenceEquals(obj, _value))
                return false;
            return true;
        }

        bool setObject(TLinked obj, bool trackBackRef)
        {
            TLinked old_val = _value;

            if (!canSetObject(obj))
                return false;

            try
            {
                if (old_val != null)
                {
                    if (trackBackRef)
                    {
                        ICSLink<TLinked, TOwner> backLink = getBackLink(old_val);
                        if (backLink != null)
                            backLink.processBackRefRemove(this);
                    }
                    OnLinkedObjectRemoved(old_val);
                }
                _value = obj;
                if (obj != null)
                    OnLinkedObjectAdded(obj);

                OnLinkSet(old_val, obj);

                return true;
            }
            catch
            {
                _value = old_val;
                throw;
            }
        }

        private void OnLinkSet(TLinked old_obj, TLinked new_obj)
        {
            if(ValueChanged != null)
                ValueChanged(this, new ValueChangedEventArgs(Owner, Name, old_obj, new_obj, true));
        }


        public override void processBackRefAdd(ICSLink<TLinked, TOwner> origLink)
        {
            if (LinkedObject != null)
                throw new InvalidOperationException("Добавляемый по ссылке <1>-<?> объект уже связан с другим объектом");
            setObject(origLink.Owner, false);
        }

        public override void processBackRefRemove(ICSLink<TLinked, TOwner> origLink)
        {
            setObject(null, false);
        }


        public event ValueChangedEventHandler ValueChanged;
    }

}
