using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ksema.Framework
{
    public class DeferredTask
    {
        public DeferredTask(Action action)
        {
            Action = action;
        }

        internal class DeferUnit : IDisposable
        {
            public DeferUnit(DeferredTask task)
            {
                this.Task = task;
            }

            DeferredTask Task;

            #region IDisposable Members

            public void Dispose()
            {
                Task.OnDeferUnitDisposed(this);
            }

            #endregion
        }

        public bool IsDeferred
        {
            get
            {
                return issuedUnits.Count > 0;
            }
        }

        HashSet<DeferUnit> issuedUnits = new HashSet<DeferUnit>();

        internal void OnDeferUnitDisposed(DeferUnit unit)
        {
            issuedUnits.Remove(unit);
            if (issuedUnits.Count == 0)
            {
                OnEndDefer();
            }
        }

        protected virtual void OnEndDefer()
        {
            try
            {
                if (needExecute)
                {
                    Action();
                }
            }
            finally
            {
                needExecute = false;
            }
        }

        public IDisposable Defer()
        {
            var unit = new DeferUnit(this);
            issuedUnits.Add(unit);
            return unit;
        }

        Action Action;

        bool needExecute = false;

        public void Execute()
        {
            if (IsDeferred)
            {
                needExecute = true;
            }
            else
            {
                Action();
            }
        }
    }
}
