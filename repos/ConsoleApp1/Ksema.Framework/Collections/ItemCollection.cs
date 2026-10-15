using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Framework.Collections
{
    public class ItemCollection<TOwner, TItem> : IList<TItem>, IList where TItem : class
    {
		public static BindingFlags BF = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.GetField | BindingFlags.SetField;
		public static FieldInfo GetField<TType>(string name)
		{
			return GetField<TType>(name, BF);
		}
		public static FieldInfo GetField<TType>(string name, BindingFlags flags)
		{
			Type type = typeof(TType);
			FieldInfo field = null;
			while (field == null && type != null)
			{
				field = type.GetField(name, flags);
				if (field == null)
					type = type.BaseType;
			}
			if (field == null)
				throw new System.NotImplementedException("Field '" + name + "' not implemented in type '" + typeof(TType).FullName + "' and its base types");
			return field;
		}

        protected TOwner m_owner;
        private List<TItem> m_list = new List<TItem>();

        public ItemCollection(TOwner owner)
        {
            m_owner = owner;
        }
        public virtual void Add(TItem item)
        {
            m_list.Add(item);
        }
        public virtual bool Remove(TItem item)
        {
            return m_list.Remove(item);
        }
        public int IndexOf(TItem item)
        {
            return m_list.IndexOf(item);
        }
        public virtual void Insert(int index, TItem item)
        {
            m_list.Insert(index, item);
        }
        public TItem this[int index]
        {
            get { return m_list[index]; }
            set
            {
				throw new NotSupportedException();
				//m_list[index] = value;
            }
        }
        public void Clear()
        {
            IList<TItem> list = new List<TItem>(m_list);
            foreach (TItem item in list)
                Remove(item);
        }
        public bool Contains(TItem item)
        {
            return m_list.Contains(item);
        }
        public void CopyTo(TItem[] array, int arrayIndex)
        {
            m_list.CopyTo(array, arrayIndex);
        }
        public int Count
        {
            get { return m_list.Count; }
        }
        public bool IsReadOnly
        {
            get { return ((IList)m_list).IsReadOnly; }
        }
        public IEnumerator<TItem> GetEnumerator()
        {
            return m_list.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_list.GetEnumerator();
        }
        public void RemoveAt(int index)
        {
            Remove(m_list[index]);
        }

		#region IList Members

		int IList.Add(object value)
		{
			if (Contains((TItem)value))
				return 0;
			this.Add((TItem) value);
			return 1;
		}

		bool IList.Contains(object value)
		{
			return this.Contains((TItem) value);
		}

		int IList.IndexOf(object value)
		{
			return this.IndexOf((TItem) value);
		}

		void IList.Insert(int index, object value)
		{
			this.Insert(index, (TItem)value);
		}

		bool IList.IsFixedSize
		{
			get { return ((IList) m_list).IsFixedSize; }
		}

		void IList.Remove(object value)
		{
			this.Remove((TItem)value);
		}

		void IList.RemoveAt(int index)
		{
			this.RemoveAt(index);
		}

		object IList.this[int index]
		{
			get
			{
				return ((IList)m_list)[index];
			}
			set
			{
				this[index] = (TItem)value;
			}
		}

		#endregion

		#region ICollection Members

		void ICollection.CopyTo(Array array, int index)
		{
			((IList)m_list).CopyTo(array, index);
		}

		bool ICollection.IsSynchronized
		{
			get { return ((IList)m_list).IsSynchronized; }
		}

		object ICollection.SyncRoot
		{
			get { return ((IList)m_list).SyncRoot; }
		}

		#endregion

		public void Sort(Comparison<TItem> comparison)
		{
			m_list.Sort(comparison);
		}
	}

    // N - M
	public class MultiOwnerItemCollection<TOwner, TItem> : ItemCollection<TOwner, TItem> where TItem : class
    {
		private FieldInfo m_ownerInfo;

        public MultiOwnerItemCollection(TOwner owner, string itemOwnerCollectionField)
            : base(owner)
        {
			m_ownerInfo = GetField<TItem>(itemOwnerCollectionField, BF);
		}
        public override void Add(TItem item)
        {
            if( Contains(item) ) 
                return;
            base.Add(item);
        	IList col = (IList) m_ownerInfo.GetValue(item);
        	col.Add(m_owner);
        }
        public override bool Remove(TItem item)
        {
            if( !Contains(item) )
                return false;
            bool ret = base.Remove(item);
			IList col = (IList)m_ownerInfo.GetValue(item);
            col.Remove(m_owner);
            return ret;
        }
    }

    // 1 - M
	public class SingleOwnerItemCollection<TOwner, TItem> : ItemCollection<TOwner, TItem> where TItem : class
    {
    	private FieldInfo m_info;
		private FieldInfo m_ownerInfo;

        public SingleOwnerItemCollection(TOwner owner, string ownerCollectionField, string itemOwnerField)
            : base(owner)
        {
			m_info = GetField<TItem>(itemOwnerField, BF);
			m_ownerInfo = GetField<TOwner>(ownerCollectionField, BF);
		}
        public override void Add(TItem item)
        {
            if( initItem(item) )
                base.Add(item);
        }
        private bool initItem(TItem item)
        {
            if( Contains(item) )
                return false;
            object oldOwner = m_info.GetValue(item);
            if( oldOwner != null )
            {
                IList oldCollection = (IList) m_ownerInfo.GetValue(oldOwner);
                oldCollection.Remove(item);
            }
            m_info.SetValue(item, m_owner);
            return true;
        }
        public override bool Remove(TItem item)
        {
            if( !Contains(item) )
                return false;
			m_info.SetValue(item, null);
            return base.Remove(item);
        }
        public override void Insert(int index, TItem item)
        {
            if( initItem(item) )
                base.Insert(index, item);
        }  
    }
    // 0 - M
	public class NoneOwnerItemCollection<TOwner, TItem> : ItemCollection<TOwner, TItem> where TItem : class
    {
        public  NoneOwnerItemCollection(TOwner owner):base(owner) { }
    }
	// 1-1
	public class ItemLink<TOwner, TItem> : SingleOwnerItemCollection<TOwner, TItem> where TItem : class
	{
		public ItemLink(TOwner owner, string ownerCollectionField, string itemOwnerField)
			: base(owner, ownerCollectionField, itemOwnerField)
		{
		}
		public override void Add(TItem item)
		{
			if (this.Count > 0)
				throw new NotSupportedException("It's one-to-one link");
			base.Add(item);
		}
		public TItem Item
		{
			get
			{
				return Count > 0 ? this[0] : null;
			}
			set
			{
				if (Count > 0)
					RemoveAt(0);
				Add(value);
			}
		}
	}
}