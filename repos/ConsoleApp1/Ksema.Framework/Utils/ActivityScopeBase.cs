namespace Ksema.Framework
{
    public abstract class ActivityScopeBase<T> : SimpleScopeBase<ActivityScopeBase<T>>
        where T : ActivityScopeBase<T>
    {
        bool _completed;
        bool _innerFailed;

        private bool IsSucceeded
        {
            get { return _completed && !_innerFailed; }
        }

        public void Complete()
        {
            CheckDisposed();
            _completed = true;
            OnComplete();
        }

        protected override void OnDispose()
        {
            if (CurrentScope != null && !_completed)
                ((ActivityScopeBase<T>)CurrentScope).SignalInnerFailed();

            OnDispose(IsSucceeded);
        }

        private void SignalInnerFailed()
        {
            _innerFailed = true;
            ActivityScopeBase<T> scope = (ActivityScopeBase<T>)PriorScope;
            while (scope != null)
            {
                scope._innerFailed = true;
                scope = (ActivityScopeBase<T>)scope.PriorScope;
            }
        }

        protected abstract void OnDispose(bool succeeded);
        protected abstract void OnComplete();

    }


}
