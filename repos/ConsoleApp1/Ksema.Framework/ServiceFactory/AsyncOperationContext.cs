using System;
using System.ComponentModel;
using Framework.Utils;

namespace Ksema.Framework
{
    public delegate IAsyncResult BeginExecuteDelegate(AsyncCallback callback, object state);

	public class AsyncCompletedEventArgs<T> : AsyncCompletedEventArgs
	{
		private readonly T _result;

		public AsyncCompletedEventArgs(T result, Exception error, bool cancelled, object userState) 
			: base(error, cancelled, userState)
		{
			_result = result;
		}

		public T Result
		{
			get 
			{
				RaiseExceptionIfNecessary();
				return _result;
			}
		}
	}

    public class AsyncOperationContext<T>
    {
        AsyncOperation _asyncOp;
        Action<AsyncCompletedEventArgs<T>> _operationCompleted;
    	protected Func<IAsyncResult, T> _endExec;

		protected AsyncOperationContext(Action<AsyncCompletedEventArgs<T>> operationCompleted)
        {
            _operationCompleted = operationCompleted;
            _asyncOp = AsyncOperationManager.CreateOperation(null);
        }

        public void ExecuteAsync(BeginExecuteDelegate beginExec, Func<IAsyncResult, T> endExec)
        {
			AssertUtils.IsNotNull(beginExec, "beginExec");
			AssertUtils.IsNotNull(endExec, "endExec");

			_endExec = endExec;
            beginExec(EndExecute, this);
        }

        protected virtual void EndExecute(IAsyncResult ar)
        {
            T result = default(T);
            Exception error = null;
            try
            {
                result = _endExec(ar);
            }
            catch (Exception ex)
            {
                error = ex;
            }
            CompleteCall(result, error);
        }

    	protected void CompleteCall(T result, Exception error)
    	{
    		AsyncCompletedEventArgs<T> args = CreateArguments(result, error);
    		_asyncOp.PostOperationCompleted(OnOperationCompleted, args);
    	}

    	private AsyncCompletedEventArgs<T> CreateArguments(T result, Exception error)
        {
            return new AsyncCompletedEventArgs<T>(result, error, false, _asyncOp.UserSuppliedState);
        }

        void OnOperationCompleted(object state)
        {
            if (_operationCompleted != null)
				_operationCompleted((AsyncCompletedEventArgs<T>)state);
        }
    }
}