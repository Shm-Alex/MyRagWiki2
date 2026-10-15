using System;
using System.Collections.Generic;
using System.Text;

namespace Ksema.Framework.Collections
{
    public class CompositeComparer<T> : IComparer<T>
    {
        IComparer<T> _main, _precise;

        public CompositeComparer(IComparer<T> main, IComparer<T> precise)
        {
            if (main == null)
                throw new ArgumentNullException("main");

            _main = main;
            _precise = precise;
        }

        public int Compare(T x, T y)
        {
            int result = _main.Compare(x, y);
            if (result == 0 && _precise != null)
                result = _precise.Compare(x, y);
            return result;
        }
    }
}
