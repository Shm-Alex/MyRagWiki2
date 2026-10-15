using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ksema.Framework.ModelView
{
    public class AccessUnitModeChangedEventArgs : EventArgs
    {
        public AccessUnitModeChangedEventArgs(IAccessUnit unit)
        {
            Unit = unit;
        }

        public virtual IAccessUnit Unit
        {
            get;
            private set;
        }
    }

    public delegate void AccessUnitModeChangedEventHandler(object sender, AccessUnitModeChangedEventArgs e);

    public interface IAccessUnitModeProvider
    {
        AccessUnitMode DefaultMode
        {
            get;
        }

        event AccessUnitModeChangedEventHandler UnitModeChanged;

        void SetMode(IAccessUnit unit, AccessUnitMode mode);
        AccessUnitMode GetMode(IAccessUnit unit);

        /// <summary>
        /// Получение единицы по имени(пока используется для поулучения враппера по имени свойства)
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        IAccessUnit GetUnit(string name);
    }
}
