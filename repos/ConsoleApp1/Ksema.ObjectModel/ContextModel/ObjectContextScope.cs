using Ksema.Framework;

namespace Ksema.ObjectModel.ContextModel
{
    class ObjectContextScope : SimpleScopeBase<ObjectContextScope>
    {
        public static ObjectContextScope Current
        {
            get 
            {
                return (ObjectContextScope)CurrentScope;
            }
        }

        public ObjectContextScope(ObjectContext aCtx)
        {
            Context = aCtx;
        }

        protected override void OnDispose()
        {
        }

        public ObjectContext Context { get; private set; }
    }
}
