using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ksema.Framework.Collections
{
    class ReadOnlyCollection<T> : ICollection<T>
    {
        ICollection<T> _base;


        public ReadOnlyCollection(ICollection<T> aBase)
        {
            _base = aBase;
        }

        void ICollection<T>.Add(T item)
        {
            throw new NotSupportedException();
        }

        void ICollection<T>.Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(T item)
        {
            return _base.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _base.CopyTo(array, arrayIndex);
        }

        bool ICollection<T>.Remove(T item)
        {
            throw new NotSupportedException();
        }

        public int Count
        {
            get { return _base.Count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _base.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return  this.GetEnumerator();
        }
    }

	// Keep it private - so no one will be able to cast and find the base collection
	class ReadOnlyEnumerable<T> : IEnumerable<T>
	{
		private IEnumerable<T> _base;
		public ReadOnlyEnumerable(IEnumerable<T> aBase)
		{
			_base = aBase;
		}

		public IEnumerator<T> GetEnumerator()
		{
			return _base.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _base.GetEnumerator();
		}

	}
}
