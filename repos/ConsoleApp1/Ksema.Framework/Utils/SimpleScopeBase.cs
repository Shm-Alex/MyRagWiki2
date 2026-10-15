using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Transactions;

namespace Ksema.Framework
{
    public abstract class SimpleScopeBase<T> : IDisposable
        where T : SimpleScopeBase<T>
    {
        [ThreadStatic]
        static SimpleScopeBase<T> s_current;

        SimpleScopeBase<T> _priorScope;
        Thread _scopeThread;

        bool _disposed = false;

        public SimpleScopeBase()
        {
            _priorScope = s_current;
            s_current = this;
            _scopeThread = Thread.CurrentThread;
        }

        protected abstract void OnDispose();


        protected static SimpleScopeBase<T> CurrentScope
        {
            get { return s_current; }
        }

        protected SimpleScopeBase<T> PriorScope
        {
            get { return _priorScope; }
        }

        void IDisposable.Dispose()
        {
            if (!_disposed)
            {
                if (_scopeThread != Thread.CurrentThread)
                    throw new InvalidOperationException("Cannot dispose on other thread");

                _disposed = true;
                AdjustToNewCurrentScope();

                OnDispose();
            }
        }

        private void AdjustToNewCurrentScope()
        {
            if (s_current == this)
            {
                // In case the user called dispose out of order, skip up the chain until we find
                //  an undisposed scope.
                SimpleScopeBase<T> prior = _priorScope;
                while (null != prior && prior._disposed)
                {
                    prior = prior._priorScope;
                }
                s_current = prior;
            }
        }

        protected void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }
        }
    }

	public static class TransactionHelper
	{
		public static TransactionScope RequiresNew(IsolationLevel isolationLevel, TimeSpan timeout)
		{
			return Create(TransactionScopeOption.RequiresNew, isolationLevel, timeout);
		}

		public static TransactionScope Create(TransactionScopeOption scopeOptions, IsolationLevel isolationLevel, TimeSpan timeout)
		{
			TransactionOptions transactionOptions = new TransactionOptions()
			{
				IsolationLevel = isolationLevel,
				Timeout = timeout
			};
			return new TransactionScope(scopeOptions, transactionOptions);
		}
	}
}
