using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Collections.Generic
{
    public class WeakSubject<T> : ISubject<T>, ISubject<T, T>, IObserver<T>, IObservable<T>
    {
        Subject<T> Subj;
        IObservable<T> WeakSource;

        public WeakSubject()
        {
            Subj = new Subject<T>();
            WeakSource = Subj.AsWeakObservable();
        }

        public void OnCompleted()
        {
            Subj.OnCompleted();
        }

        public void OnError(Exception error)
        {
            Subj.OnError(error);
        }

        public void OnNext(T value)
        {
            Subj.OnNext(value);
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return WeakSource.Subscribe(observer);
        }
    }
}
