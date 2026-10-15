using System;
using System.Collections;


namespace Framework.Collections
{
	
	public interface IAdvancedEnumerator : IEnumerator
	{

		void MoveToEnd();

		bool MovePrev();

		bool Remove();

		bool RemoveAndNext();

		new object Current
		{
			get;
			set;
		}


		int Index
		{
			get;
		}


		bool IsPositioned
		{
			get;
		}


		bool IsValid
		{
			get;
		}


	}


	public interface IAdvancedDictionaryEnumerator : IDictionaryEnumerator, IAdvancedEnumerator
	{

		new object Value
		{
			get;
			set;
		}


	}



	internal enum States {BeforeFirst, AfterLast, Positioned};

	internal class Enumerator : IAdvancedEnumerator
	{

		public ICollectionBase collection;

		public Element current;

		public States state;


		public Enumerator(ICollectionBase Collection)
		{
			collection = Collection;
			Reset();
		}



		public void Reset()
		{
			state = States.BeforeFirst;
		}


		public void MoveToEnd()
		{
			state = States.AfterLast;
		}


		public bool IsPositioned
		{
			get
			{
				return(state == States.Positioned);
			}
		}


		public bool IsValid
		{
			get
			{
				return(IsPositioned && collection.IsValid(current));
			}
		}


		public void CheckEnumerator()
		{
			if ((!IsPositioned) || (!IsValid))
				throw(new InvalidOperationException());
		}



		public bool MoveNext()
		{
			switch (state)
			{
				case States.BeforeFirst:
					current = collection.First();
					if (current == null)
					{
						state = States.AfterLast;
						goto case States.AfterLast;
					}
					state = States.Positioned;
					break;
				case States.Positioned:
					if (!IsValid)
					{
						throw(new InvalidOperationException());
					}
					current = collection.Next(current);
					if (current == null)
					{
						state = States.AfterLast;
						goto case States.AfterLast;
					}
					break;
				case States.AfterLast:
					return(false);
			}
			return(true);
		}


		public bool MovePrev()
		{
			switch (state)
			{
				case States.AfterLast:
					current = collection.Last();
					if (current == null)
					{
						state = States.BeforeFirst;
						goto case States.BeforeFirst;
					}
					state = States.Positioned;
					break;
				case States.Positioned:
					if (!IsValid)
					{
						throw(new InvalidOperationException());
					}
					current = collection.Prev(current);
					if (current == null)
					{
						state = States.BeforeFirst;
						goto case States.BeforeFirst;
					}
					break;
				case States.BeforeFirst:
					return(false);
			}
			return(true);
		}


		public bool Remove()
		{
			CheckEnumerator();
			Element This = current;
			int i = collection.Rank(current);
			bool result = MovePrev();
			collection.DeleteItem(This, i);
			return(result);
		}


		public bool RemoveAndNext()
		{
			CheckEnumerator();
			Element This = current;
			int i = collection.Rank(current);
			bool result = MoveNext();
			collection.DeleteItem(This, i);
			return(result);
		}


		public int Index
		{
			get
			{
				CheckEnumerator();
				return(collection.Rank(current));
			}
		}


		public object Current
		{
			get
			{
				CheckEnumerator();
				return(current.Key);
			}
			set
			{
				CheckEnumerator();
				collection.ChangeItem(current, value, collection.Rank(current));
			}
		}


	}


	internal class DictionaryEnumerator : Enumerator, IAdvancedDictionaryEnumerator
	{

		public DictionaryEnumerator(IDictionaryCollectionBase Collection) : base(Collection) {}


		public object Key
		{
			get
			{
				return(Entry.Key);
			}
		}


		public object Value
		{
			get
			{
				return(Entry.Value);
			}
			set
			{
				CheckEnumerator();
				((IDictionaryCollectionBase) collection).ChangeValue(current, value,
					collection.Rank(current));
			}
		}


		public DictionaryEntry Entry
		{
			get
			{
				return((DictionaryEntry) Current);
			}
		}


	}



	
	internal class MultiEnumerator : IAdvancedEnumerator
	{

		public ICollectionBase innerCollection, outerCollection;

		public Element innerElement, outerElement;

		public States state;


		public MultiEnumerator (ICollectionBase Collection)
		{
			outerCollection = Collection;
			Reset();
		}



		public void Reset()
		{
			state = States.BeforeFirst;
		}


		public void MoveToEnd()
		{
			state = States.AfterLast;
		}


		public bool IsPositioned
		{
			get
			{
				return(state == States.Positioned);
			}
		}


		public bool IsValid
		{
			get
			{
				return(IsPositioned && outerCollection.IsValid(outerElement) && 
					innerCollection.IsValid(innerElement));
			}
		}


		public void CheckEnumerator()
		{
			if ((!IsPositioned) || (!IsValid))
				throw(new InvalidOperationException());
		}



		public bool MoveNext()
		{
			switch (state)
			{
				case States.BeforeFirst:
					outerElement = outerCollection.First();
					if (outerElement == null)
					{
						state = States.AfterLast;
						goto case States.AfterLast;
					}
					innerCollection = (ICollectionBase) outerElement.Key;
					innerElement = innerCollection.First();
					state = States.Positioned;
					break;
				case States.Positioned:
					if (!IsValid)
					{
						throw(new InvalidOperationException());
					}
					innerElement = innerCollection.Next(innerElement);
					if (innerElement == null)
					{
						outerElement = outerCollection.Next(outerElement);
						if (outerElement == null)
						{
							state = States.AfterLast;
							goto case States.AfterLast;
						}
						innerCollection = (ICollectionBase) outerElement.Key;
						innerElement = innerCollection.First();
					}
					break;
				case States.AfterLast:
					return(false);
			}
			return(true);
		}


		public bool MovePrev()
		{
			switch (state)
			{
				case States.AfterLast:
					outerElement = outerCollection.Last();
					if (outerElement == null)
					{
						state = States.BeforeFirst;
						goto case States.BeforeFirst;
					}
					innerCollection = (ICollectionBase) outerElement.Key;
					innerElement = innerCollection.Last();
					state = States.Positioned;
					break;
				case States.Positioned:
					if (!IsValid)
					{
						throw(new InvalidOperationException());
					}
					innerElement = innerCollection.Prev(innerElement);
					if (innerElement == null)
					{
						outerElement = outerCollection.Prev(outerElement);
						if (outerElement == null)
						{
							state = States.BeforeFirst;
							goto case States.BeforeFirst;
						}
						innerCollection = (ICollectionBase) outerElement.Key;
						innerElement = innerCollection.Last();
					}
					break;
				case States.BeforeFirst:
					return(false);
			}
			return(true);
		}


		public bool Remove()
		{
			CheckEnumerator();
			Element outerCurrent = outerElement;
			int i = outerCollection.Rank(outerElement),
				j = innerCollection.Rank(innerElement),
                number = innerCollection.Number;
			bool result = MovePrev();
			if (number == 1)
				outerCollection.DeleteItem(outerCurrent, i);
			else
				((IMultiCollectionBase) outerCollection).DeleteInternalItem(outerCurrent, i+j, j);
			return(result);
		}


		public bool RemoveAndNext()
		{
			CheckEnumerator();
			Element outerCurrent = outerElement;
			int i = outerCollection.Rank(outerElement),
				j = innerCollection.Rank(innerElement),
				number = innerCollection.Number;
			bool result = MoveNext();
			if (number == 1)
				outerCollection.DeleteItem(outerCurrent, i);
			else
				((IMultiCollectionBase) outerCollection).DeleteInternalItem(outerCurrent, i+j, j);
			return(result);
		}


		public int Index
		{
			get
			{
				CheckEnumerator();
				return(outerCollection.Rank(outerElement)+innerCollection.Rank(innerElement));
			}
		}


		public object Current
		{
			get
			{
				CheckEnumerator();
				return(innerElement.Key);
			}
			set
			{
				CheckEnumerator();
				outerCollection.ChangeItem(outerElement, value,
					outerCollection.Rank(outerElement)+innerCollection.Rank(innerElement));
			}
		}


	}


	internal class MultiDictionaryEnumerator : MultiEnumerator, IAdvancedDictionaryEnumerator
	{

		public MultiDictionaryEnumerator(IDictionaryCollectionBase Collection) : base(Collection)
		{}


		public object Key
		{
			get
			{
				return(Entry.Key);
			}
		}


		public object Value
		{
			get
			{
				return(Entry.Value);
			}
			set
			{
				CheckEnumerator();
				((IDictionaryCollectionBase) outerCollection).ChangeValue(outerElement, value,
					outerCollection.Rank(outerElement)+innerCollection.Rank(innerElement));
			}
		}


		public DictionaryEntry Entry
		{
			get
			{
				return((DictionaryEntry) Current);
			}
		}


	}


}
