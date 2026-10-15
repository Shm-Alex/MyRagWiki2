using System;
using System.Collections;


namespace Framework.Collections
{
	
	public interface IMultiDictionary : IDictionary
	{
		new int Remove(object key);

		int Remove(object key, object value);

		ICollection UniqueKeys{get;}


		ICollection GetValues(object key);

		int GetCount(object key);

		IAdvancedDictionaryEnumerator FindFirst(object key);

		IAdvancedDictionaryEnumerator FindLast(object key);
	}


	public interface IAdvancedDictionary : IDictionary
	{
		object Key(int index);

		object Value(int index);

		DictionaryEntry Entry(int index);
	}
	



	internal enum Colors {black, red};

	internal class Element
	{
		public Element(object key)
		{
			Key = key;
		}


		public object Key;
	}


	internal class ListElement : Element
	{
		public ListElement(object key) : base(key) {}


		public ListElement Prev, Next;
	}


	internal class TreeElement : Element
	{
		public TreeElement(object key, Colors color) : base(key) 
		{
			Color = color;
			Size = 0;
			Quantity = 0;
		}


		public Colors Color;

		public int Size, Quantity;

		public TreeElement Parent, Left, Right;	
	}




	internal interface ICollectionBase
	{
		
		Element First();

		Element Last();

		Element Next(Element X);

		Element Prev(Element X);



		void ClearAll();

		void ChangeItem(Element Old, object key, int i);

		int DeleteItem(Element X, int i);

		void InsertItemAt(object key, int i);

		int AddItem(object key);



		int Rank(Element X);

		Element Found(object key, out int i);

		Element Select(ref int i);

		int Number{get;}



		bool IsValid(Element X);

	}


	internal interface IMultiCollectionBase : ICollectionBase
	{
		
		int GetCount(object key);

		void DeleteInternalItem(Element X, int index, int i);

	}


	internal interface IDictionaryCollectionBase : ICollectionBase
	{

		void ChangeValue(Element Old, object Value, int i);

	}


	public class StandardComparer : IComparer
	{
		private bool asc;
		public StandardComparer()
		{
			asc = true;
		}
		public StandardComparer(bool order)
		{
			asc = order;
		}


		public int Compare(object x, object y)
		{
			int result = ((IComparable)x).CompareTo(y);
			return asc ? result : -1 * result;
		}
	}
}
