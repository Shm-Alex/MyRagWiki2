using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Framework.Collections
{
	public sealed class LightWeakDictionary<TKey, TValue>
		where TValue: class
	{
		//private Dictionary<object, WeakReference<TValue>> dictionary;

		private Dictionary<object, IWeakReference> dictionary;

		private IEqualityComparer<object> comparer;

		public LightWeakDictionary()
			: this( 0, null )
		{
		}

		public LightWeakDictionary( int capacity )
			: this( capacity, null )
		{
		}

		public LightWeakDictionary( IEqualityComparer<object> comparer )
			: this( 0, comparer )
		{
		}

		public LightWeakDictionary( int capacity, IEqualityComparer<object> comparer )
		{
			this.comparer = comparer;
			//this.dictionary = new Dictionary<object, WeakReference<TValue>>( capacity, this.comparer );
			this.dictionary = new Dictionary<object, IWeakReference>( capacity, this.comparer );
			ValueIsWeakReference = typeof(IWeakReference).IsAssignableFrom(typeof(TValue));
		}

		public TValue this[TKey key]
		{
			get
			{
				TValue value;
				if( !this.TryGetValue( key, out value ) )
					throw new KeyNotFoundException();

				return value;
			}
			set
			{
				SetValue( key, value );
			}
		}

		private bool ValueIsWeakReference
		{
			get;
			set;
		}

		// WARNING: The count returned here may include entries for which
		// either the key or value objects have already been garbage
		// collected. Call RemoveCollectedEntries to weed out collected
		// entries and update the count accordingly.
		public int Count
		{
			get
			{
				return this.dictionary.Count;
			}
		}

		public void Add( TKey key, TValue value )
		{
			if( key == null )
				throw new ArgumentNullException( "key" );
			if (ValueIsWeakReference)
			{
				this.dictionary.Add( key, (IWeakReference)value );
			}
			else
			{
				WeakReference<TValue> weakValue = WeakReference<TValue>.Create( value );
				this.dictionary.Add( key, weakValue );
			}
		}

		public bool ContainsKey( TKey key )
		{
			return this.dictionary.ContainsKey( key );
		}

		public bool Remove( TKey key )
		{
			return this.dictionary.Remove( key );
		}

		public bool TryGetValue( TKey key, out TValue value )
		{
			if (ValueIsWeakReference)
			{
				IWeakReference weakRef;
				bool result = this.dictionary.TryGetValue( key, out weakRef );
				value = (TValue)weakRef;
				return result;
			}
			else
			{
				IWeakReference weakRef;
				if( this.dictionary.TryGetValue( key, out weakRef ) )
				{
					WeakReference<TValue> weakValue = (WeakReference<TValue>)weakRef;
					value = weakValue.Target;
					return weakValue.IsAlive;
				}
				value = null;
				return false;
			}
		}

		private void SetValue( TKey key, TValue value )
		{
			if (ValueIsWeakReference)
			{
				this.dictionary[key] = (IWeakReference)value;
			}
			else
			{
				this.dictionary[key] = WeakReference<TValue>.Create( value );
			}
		}

		public void Clear()
		{
			this.dictionary.Clear();
		}

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			if (ValueIsWeakReference)
			{
				foreach( KeyValuePair<object, IWeakReference> kvp in this.dictionary )
				{
					IWeakReference weakValue = kvp.Value;
					if( weakValue.IsAlive )
						yield return new KeyValuePair<TKey, TValue>( (TKey)kvp.Key, (TValue)weakValue );
				}
			}
			else
			{
				foreach( KeyValuePair<object, IWeakReference> kvp in this.dictionary )
				{
					WeakReference<TValue> weakValue = (WeakReference<TValue>)kvp.Value;
					TValue value = weakValue.Target;
					if( weakValue.IsAlive )
						yield return new KeyValuePair<TKey, TValue>( (TKey)kvp.Key, value );
				}
			}
		}

		// Removes the left-over weak references for entries in the dictionary
		// whose key or value has already been reclaimed by the garbage
		// collector. This will reduce the dictionary's Count by the number
		// of dead key-value pairs that were eliminated.
		public ICollection<object> RemoveCollectedEntries()
		{
			List<object> toRemove = null;
			//foreach( KeyValuePair<object, WeakReference<TValue>> pair in this.dictionary )
			foreach( KeyValuePair<object, IWeakReference> pair in this.dictionary )
			{
				//WeakReference<TValue> weakValue = pair.Value;
				IWeakReference weakValue = pair.Value;
				if( !weakValue.IsAlive )
				{
					if( toRemove == null )
						toRemove = new List<object>();
					toRemove.Add( pair.Key );
				}
			}

			if( toRemove != null )
			{
				foreach( object key in toRemove )
					this.dictionary.Remove( key );
			}
			return toRemove;
		}
	}

	public interface IWeakReference
	{
		bool IsAlive
		{
			get;
		}
	}

	public class WeakReferencePair<TRef1, TRef2>: IWeakReference
		where TRef1: class
		where TRef2: class
	{
		WeakReference<TRef1> _ref1;
		WeakReference<TRef2> _ref2;

		public WeakReferencePair( TRef1 ref1, TRef2 ref2 )
		{
			_ref1 = WeakReference<TRef1>.Create( ref1 );
			_ref2 = WeakReference<TRef2>.Create( ref2 );
		}


		public bool IsAlive
		{
			get
			{
				return _ref1.IsAlive && _ref2.IsAlive;
			}
		}

		public TRef1 Reference1
		{
			get
			{
				return _ref1.Target;
			}
		}

		public TRef2 Reference2
		{
			get
			{
				return _ref2.Target;
			}
		}
	}
}
