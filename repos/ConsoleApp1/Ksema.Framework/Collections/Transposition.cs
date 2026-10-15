using System;
using System.Collections;

namespace Framework.Collections
{
	/// <summary>
	/// Represents a transposition of elements
	/// </summary>
    public class Transposition : IList, ICollection, IEnumerable
	{
        private ArrayList alIndexes;
        private ArrayList alValues;

        private int _count;

		public Transposition()
		{
            alIndexes = new ArrayList ();
            alValues = new ArrayList ();
        }

        public Transposition (ICollection indexes, ICollection values) 
        {
            if (indexes.Count != values.Count) 
            {
                throw new ArgumentException ("Argument collections have different number of values");
            }
            alIndexes = new ArrayList (indexes);
            alValues = new ArrayList (values);
            _count = alIndexes.Count;
        }

        // Algorithm description could be found at http://rain.ifmo.ru/~mikhalev/samples/disarray/disarray.html
        public IList GetAllTranspositions () 
        {
            if (Count < 2) 
            {
                return new Transposition[] { this };
            }

            ArrayList alTranspositions = new ArrayList ();
            //No checks now - assume that types are equal and IComparable is OK.
            ArrayList tList = (alValues.Clone () as ArrayList);
            tList.Sort ();
            IList transposedValues = tList;
            alTranspositions.Add (new Transposition (alIndexes, transposedValues));

            for (;;) 
            {
                int breakingElementIndex = FindElementBreakingIncreasingSequence (transposedValues);
                // breakingElement is 'M' :)
                if (breakingElementIndex == NO_INDEX) { return alTranspositions; }
                int replacingElementIndex = FindLesserRightElementGreaterThanGiven (transposedValues, breakingElementIndex);
                // replacingElement is 'K' :)
                if (replacingElementIndex == NO_INDEX) { return alTranspositions; }
                ReplaceElementsByIndexes (ref transposedValues, breakingElementIndex, replacingElementIndex);
                ReverceElementsAfterIndex (ref transposedValues, breakingElementIndex);
                alTranspositions.Add (new Transposition (alIndexes, transposedValues));
            }

        }

        #region Transposition helper routines - working with values only now
        const int NO_INDEX = -1;

        int FindElementBreakingIncreasingSequence (IList values) 
        {
            /* Already handled in GetAllTranspositions () *
             * if (values.Count < 2) { return NO_INDEX; } */

            IComparable previousElement = values[values.Count - 1] as IComparable;
            for (int i = values.Count - 2; i >= 0; i--) 
            {
                IComparable currentElement = values[i] as IComparable;
                if (currentElement.CompareTo (previousElement) < 0) 
                {
                    return i;
                }
                previousElement = currentElement;
            }
            // Collection is reverse - ordered
            return NO_INDEX;
        }

        int FindLesserRightElementGreaterThanGiven (IList values, int givenElementIndex) 
        {
            IComparable lesserRightElementGreaterThanGiven = null;
            for (int i = givenElementIndex + 1; i < values.Count; i++) 
            {
                IComparable currentElement = values[i] as IComparable;
                if (currentElement.CompareTo (values[givenElementIndex]) > 0 && 
                    (lesserRightElementGreaterThanGiven == null || 
                    currentElement.CompareTo (lesserRightElementGreaterThanGiven) < 0)) 
                {
                    lesserRightElementGreaterThanGiven = currentElement;
                }
            }
            return values.IndexOf (lesserRightElementGreaterThanGiven);
        }

        void ReplaceElementsByIndexes (ref IList values, int firstIndex, int secondIndex) 
        {
            object oFirst = values[firstIndex];
            values[firstIndex] = values[secondIndex];
            values[secondIndex] = oFirst;
        }

        void ReverceElementsAfterIndex (ref IList values, int index) 
        {
            for (int nFirst = index + 1, nLast = values.Count - 1; nFirst < nLast; nFirst++, nLast--) 
            {
                ReplaceElementsByIndexes (ref values, nFirst, nLast);
            }
        }
        #endregion

        public DictionaryEntry this[int index] 
        {
            get 
            {
                CheckIndexFail (index);
                return new DictionaryEntry (alIndexes[index], alValues[index]);
            }
            set 
            {
                CheckIndexFail (index);
                alIndexes[index] = value.Key;
                alValues[index] = value.Value;
            }
        }

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
                return _count;
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
                return this;
            }
        }

        #endregion

        #region IEnumerable Members

        private class TranspositionEnumerator : IEnumerator, ICloneable
        {
            private Transposition transposition;
            private int index = -1;
            private object currentElement;

            internal TranspositionEnumerator (Transposition transposition) 
            {
                this.transposition = transposition;
                this.currentElement = transposition;
            }

            #region IEnumerator Members

            public void Reset()
            {
                currentElement = transposition;
                index = -1;
            }

            public object Current
            {
                get
                {
                    if (currentElement != transposition) 
                    {
                        return currentElement;
                    }
                    if (index == -1) 
                    {
                        throw new InvalidOperationException ("Enumeration does not started");
                    }
                    throw new InvalidOperationException ("Enumeration has already finished");
                }
            }

            public bool MoveNext()
            {
                if (index < (this.transposition.Count - 1))
                {
                    ++index;
                    currentElement = transposition[index];
                    return true;
                }
                currentElement = transposition;
                index = transposition.Count;
                return false;
            }

            #endregion

            #region ICloneable Members

            public object Clone()
            {
                return base.MemberwiseClone ();
            }

            #endregion
        }

        public IEnumerator GetEnumerator()
        {
            // TODO:  Add Transposition.GetEnumerator implementation
            return new TranspositionEnumerator (this);
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
                return this[index];
            }
            set
            {
                CheckObjectTypeFail (value);
                this[index] = (DictionaryEntry) value ;
            }
        }

        public void RemoveAt(int index)
        {
            CheckIndexFail (index);

            alIndexes.RemoveAt (index);
            alValues.RemoveAt (index);
            _count--;
        }

        public void Insert(int index, object value)
        {
            CheckObjectTypeFail (value);
            DictionaryEntry _value = (DictionaryEntry) value ;
            CheckIndexFail (index);

            alIndexes.Insert (index, _value.Key);
            alValues.Insert (index, _value.Value);
            _count++;
        }

        public void Remove(object value)
        {
            CheckObjectTypeFail (value);
            DictionaryEntry _value = (DictionaryEntry) value ;
            alIndexes.Remove (_value.Key);
            alValues.Remove (_value.Value);
            _count--;
        }

        public bool Contains(object value)
        {
            CheckObjectTypeFail (value);
            DictionaryEntry _value = (DictionaryEntry) value ;
            int nIndexIndex = alIndexes.IndexOf (_value.Key);
            if (nIndexIndex < 0) { return false; }
            int nValueIndex = alValues.IndexOf (_value.Value);
            return nIndexIndex == nValueIndex;
        }

        public void Clear()
        {
            alIndexes.Clear ();
            alValues.Clear ();
            _count = 0;
        }

        public int IndexOf(object value)
        {
            CheckObjectTypeFail (value);
            return alIndexes.IndexOf (((DictionaryEntry) value ).Key);
        }

        public int Add(object value)
        {
            CheckObjectTypeFail (value);
            DictionaryEntry _value = (DictionaryEntry) value ;
            int objectIndex = alIndexes.Add (_value.Key);
            alValues.Add (_value.Value);
            _count++;
            return objectIndex;
        }

        public bool IsFixedSize
        {
            get
            {
                return false;
            }
        }
        #endregion
    
        private void CheckObjectTypeFail (object value) 
        {
            if (!(value is DictionaryEntry)) 
            {
                throw new InvalidCastException ("Value cannot be cast to DictionaryEntry");
            }
        }

        private void CheckIndexFail (int index) 
        {
            if (index >= _count || index < 0) 
            {
                throw new ArgumentOutOfRangeException ("index", "Given index is out of range");                
            }
        }
    }
}
