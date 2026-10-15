using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ksema.Framework.Utils
{
	public class ConcurrentHashSet<T> : ConcurrentDictionary<T, byte>
			where T : class

	{
		const byte DummyByte = byte.MinValue;

		// For convenience, we add HashSet equivalent APIs here...
		public bool Contains(T item) => ContainsKey(item);
		public bool Add(T item) => TryAdd(item, DummyByte);
		public bool Remove(T item) => TryRemove(item, out _);
	}
}
