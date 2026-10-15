using System.Collections.Generic;
//using SCG = System.Collections.Generic;
using System.Collections;
using Framework.Utils;
using System;

namespace Ksema.ObjectModel.ContextModel
{
    public enum ObjectRelationChangeType { Added, Removed };

    public class ObjectRelationChangedArgs : EventArgs
    {
        public object Owner { get; set; }
        public object Linked { get; set; }
        public bool ForSave { get; set; }
        public ObjectRelationChangeType ChangeType { get; set; }
    }

    public interface IObjectRelation
    {
        LinkChangeSet GetChanges();
        void ResetChanges();

        ICollection GetLinkedObjects(object owner);
        ICollection GetBackLinkedObjects(object linked);

        void Add(object owner, object linked, bool forInsert);
        void AddMany(object owner, IEnumerable linked, bool forInsert);
		void MarkAsAdded(object owner, object linked);

        void Remove(object owner, object linked, bool forDelete, bool isNew = false);
        void RemoveMany(object owner, IEnumerable linked, bool forInsert);
        void RemoveAll(object owner, bool forDelete);

        Type OwnerType { get; }
        Type LinkedType { get; }
        ICSLinkInfo Info { get; }

        event EventHandler<ObjectRelationChangedArgs> ObjectRelationChanged;

        /// <summary>
        /// Do not use from user code
        /// </summary>
        void ClearDeletedBackLinkedObjects(object linked);

        /// <summary>
        /// Do not use from user code
        /// </summary>
        void ClearDeletedLinkedObjects(object owner);
    }

    public class ObjectRelation<TOwner, TLinked> : IObjectRelation
        where TOwner : class, IContextObject
        where TLinked : class, IContextObject
    {
        readonly ICSLinkInfo _info;
        readonly C5.MultiHashDictionary<TOwner, TLinked> _links;
		readonly C5.MultiHashDictionary<TOwner, TLinked> _addedLinks;
		readonly C5.MultiHashDictionary<TOwner, TLinked> _deletedLinks;

		readonly C5.MultiHashDictionary<TLinked, TOwner> _backLinks;
		readonly C5.MultiHashDictionary<TLinked, TOwner> _deletedBackLinks;

        internal ObjectRelation(ICSLinkInfo aInfo, IEqualityComparer<TOwner> ownerCmp, IEqualityComparer<TLinked> linkedCmp)
        {
            if (aInfo == null)
                throw new ArgumentNullException("aInfo");

            _info = aInfo;
			_links = new C5.MultiHashDictionary<TOwner, TLinked>(ownerCmp, linkedCmp);
			_addedLinks = new C5.MultiHashDictionary<TOwner, TLinked>( ownerCmp, linkedCmp);
			_deletedLinks = new C5.MultiHashDictionary<TOwner, TLinked>( ownerCmp, linkedCmp);
			_backLinks = new C5.MultiHashDictionary<TLinked, TOwner>(linkedCmp, ownerCmp);
			_deletedBackLinks = new C5.MultiHashDictionary<TLinked, TOwner>(linkedCmp, ownerCmp);
        }

        public event EventHandler<ObjectRelationChangedArgs> ObjectRelationChanged;

        public ICSLinkInfo Info
        {
            get { return _info; }
        }

        public Type OwnerType
        {
            get { return typeof(TOwner); }
        }

        public Type LinkedType
        {
            get { return typeof(TLinked); }
        }

        public ICollection<TOwner> GetBackLinkedObjects(TLinked linked)
        {
            AssertUtils.IsNotNull(linked, "linked");
            return _backLinks[linked];
        }

        void IObjectRelation.ClearDeletedLinkedObjects(object objOwner)
        {
            TOwner owner = (TOwner)objOwner;
            AssertUtils.IsNotNull(owner, "owner");

            _deletedLinks[owner].ForEach( linked => _deletedBackLinks.Remove(linked, owner) );
            _deletedLinks.Remove(owner);
        }

        void IObjectRelation.ClearDeletedBackLinkedObjects(object objLinked)
        {
            TLinked linked = (TLinked)objLinked;
            AssertUtils.IsNotNull(linked, "linked");

            _deletedBackLinks[linked].ForEach(owner => _deletedLinks.Remove(owner, linked));
            _deletedBackLinks.Remove(linked);
        }


        public ICollection<TLinked> GetLinkedObjects(TOwner owner)
        {
            if (owner == null)
                throw new ArgumentNullException("owner");

            return _links[owner];
        }

		public IList<TLinked> GetDeletedLinkedObjects(TOwner owner)
		{
			if (owner == null)
				throw new ArgumentNullException("owner");

			return _deletedLinks[owner].ToArray();
		}

		public ICollection<TLinked> GetAddedLinkedObjects(TOwner owner)
		{
			if (owner == null)
				throw new ArgumentNullException("owner");

			return _addedLinks[owner];
		}

        public void ResetChanges()
        {
            _deletedLinks.Clear();
            _deletedBackLinks.Clear();
            _addedLinks.Clear();
        }

    	public void Add(TOwner owner, TLinked linked, bool forInsert)
        {
            AssertUtils.IsNotNull(owner, "owner");
            AssertUtils.IsNotNull(linked, "linked");

            if (_links.Contains(owner, linked))
                return;

            _links.Add(owner, linked);
            _backLinks.Add(linked, owner);

            if (forInsert)
            {
                if (_deletedLinks.Contains(owner, linked))
                {
                    _deletedLinks.Remove(owner, linked);
                    _deletedBackLinks.Remove(linked, owner);
                }
                else
                    _addedLinks.Add(owner, linked);
            }
            else
            {
                _deletedLinks.Remove(owner, linked);
                _deletedBackLinks.Remove(linked, owner);
                _addedLinks.Remove(owner, linked);
            }

            OnLinkAdded(owner, linked, forInsert);
        }

		public void MarkAsAdded(TOwner owner, TLinked linked)
		{
			_addedLinks.Add(owner, linked);
		}


        private void OnLinkRemoved(TOwner owner, TLinked linked, bool forInsert, bool isNew = false)
        {
            owner.OnObjectLinkRemoved(Info.Name, linked, forInsert, isNew);

            if (ObjectRelationChanged != null)
            {
                using (ContextEventScope scope = new ContextEventScope())
                {
                    scope.EnlistAction(()=>ObjectRelationChanged(this, new ObjectRelationChangedArgs
                                                    {
                                                        Owner = owner,
                                                        Linked = linked,
                                                        ForSave = forInsert,
                                                        ChangeType = ObjectRelationChangeType.Removed
                                                    }));
                    scope.Complete();
                }
            }
        }

        private void OnLinkAdded(TOwner owner, TLinked linked, bool forInsert)
        {
            owner.OnObjectLinkAdded(Info.Name, linked, forInsert);

            if (ObjectRelationChanged != null)
            {
                using (ContextEventScope scope = new ContextEventScope())
                {
                    scope.EnlistAction(()=>ObjectRelationChanged(this, new ObjectRelationChangedArgs
                                                    {
                                                        Owner = owner,
                                                        Linked = linked,
                                                        ForSave = forInsert,
                                                        ChangeType = ObjectRelationChangeType.Added
                                                    }));
                    scope.Complete();
                }
            }
        }

        public void AddMany(TOwner owner, IEnumerable<TLinked> list, bool forInsert)
        {
            foreach (TLinked linked in list)
                Add(owner, linked, forInsert);
        }

        public void Remove(TOwner owner, TLinked linked, bool forDelete, bool isNew = false)
        {
            AssertUtils.IsNotNull(owner, "owner");
            AssertUtils.IsNotNull(linked, "linked");

            if (!_links.Contains(owner, linked))
                return;

            _links.Remove(owner, linked);
            _backLinks.Remove(linked, owner);

            if (forDelete)
            {
                if (_addedLinks.Contains(owner, linked))
                    _addedLinks.Remove(owner, linked);
                else
                {
                    _deletedLinks.Add(owner, linked);
                    _deletedBackLinks.Add(linked, owner);
                }
            }
            else
            {
                _addedLinks.Remove(owner, linked);
                _deletedLinks.Remove(owner, linked);
                _deletedBackLinks.Remove(linked, owner);
            }
            OnLinkRemoved(owner, linked, forDelete, isNew);
        }

        public void RemoveMany(TOwner owner, IEnumerable<TLinked> list, bool forDelete)
        {
            foreach (TLinked linked in list)
                Remove(owner, linked, forDelete);
        }

        public void RemoveAll(TOwner owner, bool forDelete)
        {
            if (owner == null)
                throw new ArgumentNullException("owner");

            foreach (TLinked linked in _links[owner].ToArray())
                Remove(owner, linked, forDelete);
        }

        public LinkChangeSet GetChanges()
        {
            LinkChangeSet changeSet = new LinkChangeSet();
            changeSet.AddedLinks = CreateUntypedLinkSet(_addedLinks);
            changeSet.DeletedLinks = CreateUntypedLinkSet(_deletedLinks);

            return changeSet;
        }

		static ICollection<KeyValuePair<Object, ICollection>> CreateUntypedLinkSet(C5.MultiHashDictionary<TOwner, TLinked> typedSet)
        {
			List<KeyValuePair<Object, ICollection>> untypedSet = new List<KeyValuePair<Object, ICollection>>(typedSet.Count);

			foreach (var entry in typedSet)
            {
                if (entry.Value == null || entry.Value.Count == 0 || entry.Key.Context == null)
                    continue;

				KeyValuePair<Object, ICollection> untyped_entry =
					new KeyValuePair<object, ICollection>(entry.Key, new List<TLinked>(entry.Value));

                untypedSet.Add(untyped_entry);
            }

            return untypedSet;
        }


        ICollection IObjectRelation.GetLinkedObjects(object owner)
        {
            return (ICollection)GetLinkedObjects((TOwner)owner);
        }

        ICollection IObjectRelation.GetBackLinkedObjects(object linked)
        {
            return (ICollection)GetBackLinkedObjects((TLinked)linked);
        }

        void IObjectRelation.Add(object owner, object linked, bool forInsert)
        {
            Add((TOwner)owner, (TLinked)linked, forInsert);
        }


        void IObjectRelation.AddMany(object owner, IEnumerable list, bool forInsert)
        {
            foreach (TLinked linked in list)
                Add((TOwner)owner, linked, forInsert);
        }

		void IObjectRelation.MarkAsAdded(object owner, object linked)
		{
			MarkAsAdded((TOwner)owner, (TLinked)linked);
		}


        void IObjectRelation.Remove(object owner, object linked, bool forDelete, bool isNew)
        {
            Remove((TOwner)owner, (TLinked)linked, forDelete, isNew);
        }

        void IObjectRelation.RemoveAll(object owner, bool forDelete)
        {
            RemoveAll((TOwner)owner, forDelete);
        }

        void IObjectRelation.RemoveMany(object owner, IEnumerable list, bool forInsert)
        {
            foreach(TLinked linked in list)
                Remove((TOwner)owner, linked, forInsert);
        }

    }


    public class LinkChangeSet
    {
        public ICollection<KeyValuePair<Object,ICollection>> AddedLinks { get; set; }
        public ICollection<KeyValuePair<Object, ICollection>> DeletedLinks { get; set; }
    }
}