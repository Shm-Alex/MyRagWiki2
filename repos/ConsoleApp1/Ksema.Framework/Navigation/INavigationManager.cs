using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace Ksema.Framework.Navigation
{
    public interface INavigationManager : INotifyPropertyChanged
    {
        /// <summary>
        /// Приостановить учет нотификаций об изменениях
        /// </summary>
        /// <returns></returns>
        IDisposable SuspendChanges();

        /// <summary>
        /// Приостановить учет нотификаций об изменениях
        /// </summary>
        /// <param name="getLastChange">Учесть последнюю нотификацию в сессии приостановки</param>
        /// <returns></returns>
        IDisposable SuspendChanges(bool getLastChange);

        IList<INavigationNodeProvider> NodeProviders
        {
            get;
        }

        /// <summary>
        /// Восстановить состояние
        /// </summary>
        /// <param name="state">Состояние</param>
        void Open(string state);

        /// <summary>
        /// Восстановить состояние
        /// </summary>
        /// <param name="state">Состояние</param>
        /// <param name="useLastState">Использовать в качестве нового состояние последнее состояние после восстановления, по умолчанию true</param>
        void Open(string state, bool useLastState);

        /// <summary>
        /// Нотификация об изменении состояния
        /// </summary>
        /// <param name="sender"></param>
        void OnNavigationNodeStateChanged(object sender);

        void OnNavigationNodeLoaded(object sender);

        /// <summary>
        /// История состояний
        /// </summary>
        ObservableCollection<string> History
        { 
            get;
        }

        /// <summary>
        /// Вью синхронизированный с CurrentItem
        /// для изменения CurrentStep достаточно изменить CurrentItem
        /// </summary>
        ICollectionView HistoryView
        {
            get;
        }

        /// <summary>
        /// Индекс текущего состояния в History
        /// </summary>
        int CurrentStep
        {
            get;
            set;
        }

        /// <summary>
        /// Текущее собранное состояние, доступно в момент сбора состояний узлов навигации
        /// </summary>
        NavigationState CurrentState
        {
            get;
        }

        /// <summary>
        /// Текущий адрес
        /// </summary>
        string CurrentURI
        {
            get;
        }

        /// <summary>
        /// Назад
        /// </summary>
        void Back();

        /// <summary>
        /// Вперед
        /// </summary>
        void Forward();

        /// <summary>
        /// Назад внутри контрола
        /// </summary>
        void BackInsideControl();

        /// <summary>
        /// Вперед внутри контрола
        /// </summary>
        void ForwardInsideControl();

        ICommand ForwardCommand
        {
            get;
        }

        ICommand BackCommand
        {
            get;
        }

        ICommand ForwardInControlCommand
        {
            get;
        }

        ICommand BackInControlCommand
        {
            get;
        }

    	NavigationState GetNavigationNodeState(INavigationNode node);
    }
}
