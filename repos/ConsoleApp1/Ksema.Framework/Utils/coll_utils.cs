using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Framework.Collections;
using System.Linq;
using Ksema.Framework.Collections;

namespace Framework.Utils
{
    /// <summary>
    /// Методы для работы с различными коллекциями
    /// </summary>
	public static class CUtils
	{
		static readonly IList __emptyList = new object[0];
		static readonly IDictionary __emptyDictionary = new ReadOnlyDictionary(new ListDictionary());

        public static ICollection<T> AsReadOnly<T>(this ICollection<T> coll)
        {
            return new ReadOnlyCollection<T>(coll);
        }

		public static IEnumerable<T> AsReadOnly<T>(this IEnumerable<T> list)
		{
			return new ReadOnlyEnumerable<T>(list);
		}

	    public static IList EmptyList
		{
			get
			{
				return __emptyList;
			}
		}
		public static IDictionary EmptyDictionary
		{
			get
			{
				return __emptyDictionary;
			}
		}
		public static IDictionary ReadOnly(this IDictionary source)
		{
			return new ReadOnlyDictionary(source);
		}
		public static IList ReadOnly(this IList source)
		{
			return ArrayList.ReadOnly(source);
		}

		public static bool HasItems(this ICollection source)
		{
			return source != null && source.Count > 0;
		}
		public static bool HasItems<T>(this ICollection<T> source)
		{
			return source != null && source.Count > 0;
		}

		public static object getFirstEnumerableItem(this IEnumerable lst)
		{
			if(lst == null)
				throw new ArgumentNullException("lst");
			IEnumerator it = lst.GetEnumerator();
			return it.MoveNext() ? it.Current : null;
		}

	    public static object getFirstItem(this ICollection lst)
		{
			return getItem(lst, 0);
		}

		public static object getLastItem(this ICollection lst)
		{
			if(lst == null)
				throw new ArgumentNullException();
			return getItem(lst, lst.Count - 1);
		}
		public static object getItem(this ICollection lst, int index)
		{
			if(lst == null)
				throw new ArgumentNullException();
			if(index < 0 || index >= lst.Count)
				return null;
			//
			IList _list = lst as IList;
			if (_list != null)
				return _list[index];
			//
			IEnumerator it = lst.GetEnumerator();
			for(int i = 0;i < index && it.MoveNext();i++)
			{
			}
		    return it.MoveNext() ? it.Current : null;
		}

	    public static T GetItem<T>(this ICollection<T> lst, int index )
        {
            if( lst == null )
                throw new ArgumentNullException( "lst" );
            if( index < 0 || index >= lst.Count )
                throw new IndexOutOfRangeException( string.Format( "index: {0}", index ) );
            //
            IList<T> _list = lst as IList<T>;
            if( _list != null )
                return _list[index];
            //
            IEnumerator<T> it = lst.GetEnumerator();
            for( int i = 0; i < index && it.MoveNext(); i++ )
            {
            }
	        if( it.MoveNext() )
                return it.Current;
            throw new IndexOutOfRangeException( string.Format( "index: {0}", index ) );
            //return null;
        }

		public static bool hasItem(this ICollection lst, object item)
		{
			if(lst == null)
				throw new ArgumentNullException();
			//
			IList _list = lst as IList;
			if (_list != null)
				return _list.Contains(item);
			//
			foreach (object element in lst)
				if (Equals(element, item))
					return true;
			return false;
		}

        public static bool hasItem (this ICollection lst, object item, IComparer comparer)
        {
            //cannot use IList.Contains here since we need comparer
            foreach (object element in lst)
            {
                if (comparer.Compare(element, item) == 0)
                {
                    return true;
                }
            }

            return false;
        }

		public static int indexOf(this ICollection lst, object item)
		{
			if(lst == null)
				throw new ArgumentNullException();
			//
			IList _list = lst as IList;
			if (_list != null)
				return _list.IndexOf(item);
			//
			int i = 0;
			foreach (object element in lst)
			{
			    if (Equals(element, item))
					return i;
			    i++;
			}
		    return -1;
		}


		public static IList sortItems(this ICollection items)
		{
			if(items == null)
				throw new ArgumentNullException("items");
			OrderedList res = new OrderedList();
			foreach(object elem in items)
				res.Add(elem);
			return res;
		}
		public static IList sortItems(this ICollection items, IComparer comparer)
		{
			if(items == null)
				throw new ArgumentNullException("items");
			if(comparer == null)
				throw new ArgumentNullException("comparer");
			OrderedList res = new OrderedList(comparer);
			foreach(object elem in items)
				res.Add(elem);
			return res;
		}


		public static object findMaxItem(this ICollection collection)
		{
			IComparable max = null;
			foreach(IComparable item in collection)
			{
				if(item != null && item.CompareTo(max) > 0)
					max = item;
			}
			return max;
		}

		public static object findMinItem(this ICollection collection)
		{
			IComparable min = null;
			foreach(IComparable item in collection)
			{
				if(item == null)
					return null;
				if(min == null)
					min = item;
				if(item.CompareTo(min) < 0)
					min = item;
			}
			return min;
		}

		public static ICollection Combine(this ICollection c1, ICollection c2)
		{
			ArrayList result = new ArrayList(c1.Count + c2.Count);
			result.AddRange(c1);
			result.AddRange(c2);
			return result;
		}
		public static ICollection Combine(params ICollection[] cList)
		{
			int capacity = 0;
			foreach (ICollection c in cList)
				if (c != null)
					capacity += c.Count;
			ArrayList result = new ArrayList(capacity);
			foreach (ICollection c in cList)
				if (c != null)
					result.AddRange(c);
			return result;
		}
		public static ICollection CombineDistinct(params ICollection[] cList)
		{
			ArrayList result = new ArrayList();
			foreach (ICollection c in cList)
				if (c != null)
					foreach (object item in c)
						if (!result.Contains(item))
							result.Add(item);
			return result;
		}


		public static ICollection Clone(this ICollection source)
		{
			return new ArrayList(source);
		}

		public static IDictionary Clone(this IDictionary source)
		{
			if(source == null)
				throw new ArgumentNullException("source");

			IDictionary dst;

			if(source is ICloneable)
				dst = (IDictionary) ((ICloneable)source).Clone();
			else
			{
				dst = new HybridDictionary(source.Count);
				foreach(DictionaryEntry entry in source)
					dst.Add(entry.Key, entry.Value);
			}

			return dst;
		}

		public static IList CopyTo(this IList destination, ICollection source)
		{
			if (destination == null || source == null)
				throw new ArgumentNullException();
			foreach (object item in source)
				destination.Add(item);
			return destination;
		}

        public static CompareResult CompareTestNulls(object x, object y)
        {
            if (x == null && y == null)
                return CompareResult.EqualTo;
            if (x == null)
                return CompareResult.LessThan;
            if (y == null)
                return CompareResult.GreaterThan;

            return CompareResult.Other;
        }

        public static bool AreListsEquals<T>(IList<T> first, IList<T> second)
        {
            AssertUtils.IsNotNull(first, "first list");
            AssertUtils.IsNotNull(second, "second list");

            if (first.Count != second.Count)
                return false;

            for (int i = 0; i < first.Count; i++)
            {
                if (!Equals(first[i], second[i]))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Сравнивает два хэша.
        /// </summary>
        public static bool AreDictionariesEquals(IDictionary<string, object> first, IDictionary<string, object> second)
        {
            if (first.Count != second.Count)
                return false;

            foreach (KeyValuePair<string, object> de in first)
            {
                if (!second.ContainsKey(de.Key))
                    return false;
                object secondValue = second[de.Key];
                if (!Equals(de.Value, secondValue))
                {
                    // еще может быть такая ситуация, когда в одном хэше есть "",
                    // а в другом null - считаем, что эквивалентны...

                    //вообще говоря, это все бред, ибо отсутствие значения и null в принципе
                    //одно и тоже, однако данной реализацией не [всегда] ловится...
                    //т.е. надо убирать сравнение кол-ва элементов и одинаковость набора ключей,
                    //и проходить по эл. второго кэша с учетом первого прохода

                    if (string.Empty.Equals(de.Value) && secondValue == null)
                        continue;
                    if (string.Empty.Equals(secondValue) && de.Value == null)
                        continue;
                    return false;
                }
            }
            return true;
        }

		public static int SafeGetIntFromStr(string s)
		{
			if (string.IsNullOrEmpty(s.Trim()))
				return 0;
			try
			{
				return Convert.ToInt32(s);
			}
			catch (Exception)
			{
				return 0;
			}
		}
        public static void ForEachUnsafe<T>(this IEnumerable list, Action<T> action)
        {
            foreach (T item in list)
                action(item);
        }

		public static IEnumerable<T> ForEach<T>(this IEnumerable<T> list, Action<T> action)
        {
            foreach (T item in list)
                action(item);
			return list;
        }

		public static IEnumerable<T> ForEach<T, R>(this IEnumerable<T> list, Func<T, R> action)
		{
			foreach (T item in list)
				action(item);
			return list;
		}

		public static void Repeat(int times, Action action)
        {
            for (int i = 0; i < times; i++)
                action();
        }

		public static string JoinToString<T>(this IEnumerable<T> array, string separator)
		{
			return String.Join(separator, ConvertToStringArray(array));
		}

    	public static string[] ConvertToStringArray<TSource>(this IEnumerable<TSource> array)
        {
            return array.ConvertToArray(item => item.ToString());
        }


		public static TResult[] ConvertToArray<TSource, TResult>(this IEnumerable<TSource> coll, Func<TSource, TResult> converter)
		{
			TResult[] result = new TResult[coll.Count()];
			int i = 0;
			foreach(TSource item in coll)
				result[i++] = converter(item);
			return result;
		}

		public static TResult[] ConvertToArrayUnsafe<TSource, TResult>(this ICollection coll, Func<TSource, TResult> converter)
		{
			TResult[] result = new TResult[coll.Count];
			int i = 0;
			foreach (TSource item in coll)
				result[i++] = converter(item);
			return result;
		}

	}

    public enum CompareResult { EqualTo = 0, LessThan = -1, GreaterThan = 1, Other = 2 }

}