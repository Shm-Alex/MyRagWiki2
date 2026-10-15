using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ksema.Framework.ModelView;
using Framework.Misc;

namespace Ksema.Framework.ModelView
{
    /// <summary>
    /// Заглушка на случай если настойщего юнита ещё нет
    /// при появлении юнита(дата врапера или команды) юнит будет работать как прокси
    /// тк маркап экстеншены уже используют его режим в байндинге
    /// Если юнит не появится, то быдут просто юнит доступа без прибамбасов
    /// </summary>
    class DummyAccesUnit : PropertyChangedHelper, IAccessUnit
    {
        public DummyAccesUnit(IAccessUnitModeProvider provider)
        {
            Provider = provider;
        }

        IAccessUnitModeProvider Provider;

        public AccessUnitMode Mode
        {
            get
            {
                return Provider.GetMode(realUnit);
            }
            set
            {
                Provider.SetMode(realUnit, value);
            }
        }

        IAccessUnit realUnit
        {
            get
            {
                return Source ?? this;
            }
        }

        IAccessUnit source = null;
        internal IAccessUnit Source
        {
            get
            {
                return source;
            }
            set
            {
                // Исходный юнит найден, работаем далее как прокси
                source = value;
                source.ToObservable(s => s.Mode).Subscribe(__RaiseModeChanged);
                __RaiseModeChanged();
            }
        }

        public void __RaiseModeChanged()
        {
            OnPropertyChanged("Mode");
        }
    }
}
