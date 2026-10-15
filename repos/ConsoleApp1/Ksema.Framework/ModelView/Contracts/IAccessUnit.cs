using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Ksema.Framework.ModelView
{
    public enum AccessUnitMode
    {
        Edit,
        ReadOnly,
        Hidden
    }

    /// <summary>
    /// Единица режима доступа, может быть как свойство так и класс или что угодно
    /// </summary>
    public interface IAccessUnit : INotifyPropertyChanged
    {
        /// <summary>
        /// Режим доступа, только враппер для доступа к IAccessUnitModeProvider.GetMode
        /// </summary>
        AccessUnitMode Mode
        {
            get;
            set;
        }

        /// <summary>
        /// Служебная
        /// </summary>
        void __RaiseModeChanged();
    }

    public interface IDimensionAccessUnit : IAccessUnit
    {
        string Dimension
        {
            get;
        }
        string Field
        {
            get;
        }
    }
}
