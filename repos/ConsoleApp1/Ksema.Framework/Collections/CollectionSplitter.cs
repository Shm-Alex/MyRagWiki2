using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Ksema.Framework.Collections
{
	public class CollectionSplitter<T> : IEnumerable<ICollection<T>>
	{
		private readonly int _clusterSize;

		IEnumerable<T> _collection;
		IEnumerator<T> _currentPos;
		private HashSet<T> _exColl;

		public CollectionSplitter(IEnumerable<T> aCollection, int clusterSize, IEnumerable<T> exColl=null)
		{
			if (aCollection == null)
				throw new ArgumentNullException("aCollection");
			if (clusterSize <= 0)
				throw new ArgumentOutOfRangeException("clusterSize", clusterSize, "Cluster size must be greater than zero");
			if (exColl != null && exColl.Any() )
			{
				_exColl = new HashSet<T>();
				foreach (var cl in exColl)
					_exColl.Add(cl);
				_collection = aCollection.Where(x => !_exColl.Contains(x)).ToArray();
			}
			else
				_collection = aCollection;
			_clusterSize = clusterSize;
			Reset();
		}


		public void Reset()
		{
			_currentPos = _collection.GetEnumerator();
		}

		public bool GetNextCluster(out IList<T> cluster)
		{
			cluster = GetNextClusterImpl();
			return cluster.Count > 0;
		}

		public bool GetNextCluster(out T[] clusterArray)
		{
			clusterArray = GetNextClusterImpl().ToArray();
			return clusterArray.Length > 0;
		}

		private List<T> GetNextClusterImpl()
		{
			List<T> cluster = new List<T>(_clusterSize);
			if (_exColl != null && _exColl.Any())
			{
				var add = _exColl.First();
				cluster.Add(add);
				_exColl.Remove(add);
				return cluster;
			}

			int count = 0;
			while (count < _clusterSize && _currentPos.MoveNext())
			{
				count++;
				cluster.Add(_currentPos.Current);
			}
			return cluster;
		}


		public IEnumerator<ICollection<T>> GetEnumerator()
		{
			IList<T> cluster;
			while (GetNextCluster(out cluster))
				yield return cluster;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
	}

	public static class CollectionSplitter
	{
		public static CollectionSplitter<T> Split<T>(IEnumerable<T> collection, int clusterSize)
		{
			return new CollectionSplitter<T>(collection, clusterSize);
		}
	}
}
