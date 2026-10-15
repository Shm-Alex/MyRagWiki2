using System;
using System.Collections;
using System.Collections.Generic;

namespace Framework.Collections
{
    public class TypedSet<T> : ICollection<T>
    {
        Dictionary<T, int> _table;

        public TypedSet()
        {
            _table = new Dictionary<T, int>();
        }

        public TypedSet(IEqualityComparer<T> comparer)
        {
            _table = new Dictionary<T, int>(comparer);
        }

        public TypedSet(ICollection<T> items)
        {
            _table = new Dictionary<T, int>(items.Count);
            AddRange(items);
        }

        public int Count
        {
            get { return _table.Count; }
        }

        public int GetCount(T item)
        {
            int count;
            if (_table.TryGetValue(item, out count))
                return count;
            else
                return 0;
        }

        public bool Add(T item)
        {
            int count;
            if (_table.TryGetValue(item, out count))
            {
                _table[item] = count + 1;
                return false;
            }
            else
            {
                _table.Add(item, 1);
                return true;
            }
        }

        public bool Remove(T item)
        {
            return _table.Remove(item);
        }

        public bool Contains(T item)
        {
            return _table.ContainsKey(item);
        }

        public ICollection<T> GetItems()
        {
            return _table.Keys;
        }

        public void Clear()
        {
            _table.Clear();
        }

        public void AddRange(ICollection<T> items)
        {
            if (items == null)
                throw new ArgumentNullException("items");
            foreach (T item in items)
                Add(item);
        }

        IEqualityComparer<T> Comparer
        {
            get { return _table.Comparer; }
        }


        void ICollection<T>.Add(T item)
        {
            Add(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            GetItems().CopyTo(array, arrayIndex);
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return GetItems().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    public interface ISet : IList
    {
        ISet Intersect (ISet anotherSet);
        ISet Combine (ISet anotherSet);
        ISet Subtract (ISet anotherSet);

        void AddRange (ICollection col);

        string[] ToStringArray ();
    }

    public class Set : ISet
	{
        private Hashtable htItems;
        private ArrayList alItems;

		public Set () 
        {
            htItems = new Hashtable ();
            alItems = new ArrayList ();
        }
        
        public Set (ICollection items) 
        {
            htItems = new Hashtable ();
            alItems = new ArrayList ();

            foreach (object item in items) 
            {
                Add (item);
            }
        }

        #region ISet Members
        public ISet Intersect (ISet anotherSet) 
        {
            Set result = new Set ();
            ISet traversingSet = (Count > anotherSet.Count) ? anotherSet : this;
            ISet checkSet = (this == traversingSet) ? anotherSet : this;

            foreach (object o in traversingSet) 
            {
                if (checkSet.Contains (o)) { result.Add (o); }
            }

            return result;
        }

        public ISet Combine (ISet anotherSet) 
        {
            Set result = new Set (this);
            foreach (object o in anotherSet) 
            {
                result.Add (o);
            }
            return result;
        }

        public ISet Subtract (ISet anotherSet) 
        {
            if (anotherSet == null || anotherSet.Count == 0) 
            {
                return new Set (this);
            }

            Set result = new Set ();
            //Adding instead of removing because Remove() is slow
            foreach (object o in this) 
            {
                if (!anotherSet.Contains (o)) 
                {
                    result.Add (o);
                }
            }

            return result;
        }

        public void AddRange(ICollection col)
        { 
            foreach (object obj in col)
            {
                Add (obj);
            }
        }

        public string[] ToStringArray ()
        {
            // Following realisation is possible too:
            //(string[]) alItems.ToArray(typeof (string));

            // But this version should be more fast and reliable
            IList thisAsList = this;
            string[] result = new string[Count];

            for (int i = 0; i < Count; i++)
            {
                result[i] = thisAsList[i].ToString();
            }

            return result;
        }

        #endregion

        #region ICollection Members

        public bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        public int Count
        {
            get
            {
                return alItems.Count;
            }
        }

        public void CopyTo(Array array, int index)
        {
            if (array == null) 
            {
                throw new ArgumentNullException ("array", "array parameter is null");
            }

            if (index < 0) 
            {
                throw new ArgumentOutOfRangeException ("index", index, "Index is less than zero");
            }

            if ((array.Length < index + Count) || array.Rank > 1) 
            {
                throw new ArgumentException ("Collection won't fit into the given array or array is multidimensional");
            }

            for (int i = index; i < index + Count; i++) 
            {
                array.SetValue (this[i], i);
            }
        }

        public object SyncRoot
        {
            get
            {
                // TODO:  Add Set.SyncRoot getter implementation
                return null;
            }
        }

        #endregion

        #region IEnumerable Members

        private class SetEnumerator : IEnumerator
        {
            private int index = -1;
            private Set _set;
            private object current;
            private int setCount;

            public SetEnumerator (Set _set) 
            {
                this._set = _set;
                current = _set;
                setCount = _set.Count;
            }

            #region IEnumerator Members

            public void Reset()
            {
                index = -1;
                current = _set;
            }

            public object Current
            {
                get
                {
                    if (current == _set) 
                    {
                        throw new InvalidOperationException ("Enumerator is not in valid state");
                    }
                    if (setCount != _set.Count) 
                    {
                        throw new InvalidOperationException ("Set was modified during enumeration");
                    }

                    return current;
                }
            }

            public bool MoveNext()
            {
                index++;
                if (index >= _set.Count) 
                {
                    current = _set;
                    return false;
                }
                current = (_set as IList)[index];
                return true;
            }

            #endregion

        }


        public IEnumerator GetEnumerator()
        {
            return new SetEnumerator (this);
        }

        #endregion

        #region IList Members

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        object System.Collections.IList.this[int index]
        {
            get
            {
                CheckIndexFail (index);
                return alItems[index];
            }
            set
            {
                CheckIndexFail (index);
                htItems.Remove (alItems[index]);
                alItems[index] = value;
                htItems.Add (alItems[index], index);
            }
        }

        
        #region Adding/removing an elements inside the set - if you need to use these oftenly, better choose other collection
        public void RemoveAt(int index)
        {
            CheckIndexFail (index);
            htItems.Remove (alItems[index]);
            alItems.RemoveAt (index);

            RefreshIndexes (index);
        }

        public void Insert(int index, object value)
        {
            CheckIndexFail (index);

            if (Contains (value)) 
            {
                Remove (value);
            }

            alItems.Insert (index, value);
            htItems.Add (value, index);

            RefreshIndexes (index + 1);
        }

        public void Remove(object value)
        {
            object oItemIndex = htItems[value];
            if (oItemIndex != null) 
            {
                int nItemIndex = (int) oItemIndex;
                htItems.Remove (value);
                alItems.RemoveAt (nItemIndex);

                RefreshIndexes (nItemIndex);
            }
        }
        #endregion

        public bool Contains(object value)
        {
            return htItems.ContainsKey (value);
        }

        public void Clear()
        {
            htItems.Clear ();
            alItems.Clear ();
        }

        public int IndexOf(object value)
        {
            object oItemIndex = htItems[value];
            return (oItemIndex == null) ? -1 : (int) oItemIndex;
        }

        public int Add(object value)
        {
            if (Contains (value)) 
            {
                return (int) htItems[value];
            }

            int nItemIndex = alItems.Add (value);
            htItems.Add (value, nItemIndex);
            return nItemIndex;
        }

        public bool IsFixedSize
        {
            get
            {
                return false;
            }
        }

        #endregion

        public object this[object key] 
        {
            get 
            {
                return Contains(key) ? key : null;
            }
            set 
            {
                Add (key);
            }
        }

        private void CheckIndexFail (int index) 
        {
            if (index >= Count || index < 0) 
            {
                throw new ArgumentOutOfRangeException ("index", "Given index is out of range");                
            }
        }


        private void RefreshIndexes (int startIndex) 
        {
            for (int i = startIndex; i < alItems.Count; i++) 
            {
                htItems[alItems[i]] = i;
            }
        }
	}
}
