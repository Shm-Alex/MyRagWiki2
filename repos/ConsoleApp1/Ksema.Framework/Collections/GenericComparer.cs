using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ksema.Framework.Collections
{
	public class GenericComparer<T> : IComparer<T>
	{
		private readonly Func<T, T, int> _comparer;
		public GenericComparer(Func<T, T, int> comparer)
		{
			_comparer = comparer;
		}

		public int Compare(T x, T y)
		{
			return _comparer(x, y);
		}
	}

	public class SimpleEqualityComparer<T> : IEqualityComparer<T>
	{
		private readonly Func<T, object> _mapper;
		public SimpleEqualityComparer(Func<T, object> mapper)
		{
			_mapper = mapper;
		}

		public bool Equals(T x, T y)
		{
			return _mapper(x).Equals(_mapper(y));
		}

		public int GetHashCode(T obj)
		{
			return _mapper(obj).GetHashCode();
		}
	}
}
