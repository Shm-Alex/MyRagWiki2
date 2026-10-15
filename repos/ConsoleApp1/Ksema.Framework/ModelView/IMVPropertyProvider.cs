using System;

namespace Ksema.Framework.ModelView
{
    public interface IMVPropertyProvider
    {
        bool IsLocalValueSet<T>(MVProperty<T> property);
        T GetLocalValue<T>(MVProperty<T> property);
        T GetValue<T>(MVProperty<T> property);
        void SetValue<T>(MVProperty<T> property, T value);
    }
}
