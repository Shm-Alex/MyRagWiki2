using System;
using System.Collections.Generic;
using Framework.Utils;
using Ksema.Framework;

namespace Ksema.ObjectModel.ContextModel
{
    public interface IUnitOfWorkHost<T> where T : class, IUnitOfWorkHost<T>
    {
        //void OnCreate(UnitOfWork<T> unit);
        //bool BeforeCommit(UnitOfWork<T> unit);
        //void AfterCommit(UnitOfWork<T> unit, bool succeeded);
    }

    public class UnitOfWork<T> 
        where T : class, IUnitOfWorkHost<T>
    {
        readonly LinkedList<Action> _actions = new LinkedList<Action>();

        public void EnlistAction(Action action)
        {
            AssertUtils.IsNotNull(action, "action");
            _actions.AddLast(action);
        }

        public void Commit()
        {
            _actions.ForEach(a => a());
        }

        public static UnitOfWork<T> Current
        {
            get { return UnitOfWorkScope<T>.Current == null ? null : UnitOfWorkScope<T>.Current.Unit; }
        }
    }

    public class UnitOfWorkScope<T> : ActivityScopeBase<UnitOfWorkScope<T>>
        where T : class, IUnitOfWorkHost<T>
    {
        bool _isLocal;

        public UnitOfWorkScope()
        {
            OnCreateScope();
        }

        public static UnitOfWorkScope<T> Current
        {
            get { return (UnitOfWorkScope<T>) CurrentScope; }
        }

        public UnitOfWork<T> Unit { get; private set; }
        public void EnlistAction(Action action)
        {
            Unit.EnlistAction(action);
        }

        void OnCreateScope()
        {
            UnitOfWorkScope<T> sbase = PriorScope as UnitOfWorkScope<T>;

            if (sbase != null)
            {
                Unit = sbase.Unit;                
                _isLocal = false;
            }
            else
            {
                Unit = new UnitOfWork<T>();
                _isLocal = true;
            }
        }

        protected override void OnDispose(bool succeeded)
        {
            if (succeeded && _isLocal)
                Unit.Commit();
        }

        protected override void OnComplete()
        {
        }
    }
}
