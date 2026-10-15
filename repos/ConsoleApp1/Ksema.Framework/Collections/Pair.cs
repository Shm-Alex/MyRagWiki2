using System;
using System.Collections.Generic;
using System.Text;

namespace Ksema.Framework.Collections
{
	public class Pair<T1, T2> : IEquatable<Pair<T1, T2>>
	{
		private T1 first;
		private T2 second;
		public Pair(T1 first, T2 second)
		{
			this.First = first;
			this.Second = second;
		}

		public T1 First
		{
			get { return first; }
			set { first = value; }
		}
		public T2 Second
		{
			get { return second; }
			set { second = value; }
		}

		public override bool Equals(object obj)
		{
			return Equals((Pair<T1, T2>)obj);
		}
		public bool Equals(Pair<T1, T2> other)
		{
			return first.Equals(other.first) && second.Equals(other.second);
		}
		public override int GetHashCode()
		{
			T2 temp2 = default(T2);
			T1 temp1 = default(T1);
			if (object.Equals(temp2, second)
				&& object.Equals(temp1, first))
				return 0;
			if (object.Equals(temp2, second))
				return first.GetHashCode();
			if (object.Equals(temp1, first))
				return second.GetHashCode();
			return first.GetHashCode() ^ second.GetHashCode();
		}
	}

	public class Tuple<T1,T2,T3> : Pair<T1, T2>
	{
		public Tuple(T1 first, T2 second, T3 third) : base(first, second)
		{
			Third = third;
		}

		public T3 Third { get; set; }

		public override bool Equals(object obj)
		{
			return Equals(obj as Tuple<T1, T2, T3>);
		}

		public bool Equals(Tuple<T1, T2, T3> other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return base.Equals(other) && Equals(other.Third, Third);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				T3 temp = default(T3);
				return !object.Equals(Third, temp) ? (base.GetHashCode()*397) ^ Third.GetHashCode() : base.GetHashCode();
			}
		}
	}

	public class SingleTypePair<T> : Pair<T, T>
	{
		public SingleTypePair(T first, T second) : base(first, second)
		{
		}
	}
}
