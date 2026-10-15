using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using Framework.Misc;
using System.Windows;
using Framework.Collections;

namespace Ksema.Framework.Async
{
    public class AsyncCommandGroup : DispatcherCollection<IExtendedCommand>, IAsyncCommandGroup
    {
        public AsyncCommandGroup()
        {
        }

        protected override void ClearItems()
        {
            base.ClearItems();

            foreach (var s in subscriptions.Values)
                s.Dispose();
            subscriptions.Clear();
            RaiseIsWorkingChanged();
        }

        protected override void InsertItem(int index, IExtendedCommand item)
        {
            if (!this.Contains(item))
            {
                base.InsertItem(index, item);

                subscriptions[item] = item.Started.Merge(item.Completed).Subscribe(OnTaskStateChanged);
                RaiseIsWorkingChanged();
            }
        }

        protected override void RemoveItem(int index)
        {
            IDisposable disp;
            var item = this[index];
            if (subscriptions.TryGetValue(item, out disp))
            {
                disp.Dispose();
                subscriptions.Remove(item);
            }

            base.RemoveItem(index);

            RaiseIsWorkingChanged();
        }

        Dictionary<IExtendedCommand, IDisposable> subscriptions = new Dictionary<IExtendedCommand, IDisposable>();

        void OnTaskStateChanged()
        {
            if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.BeginInvoke((Action)RaiseIsWorkingChanged, System.Windows.Threading.DispatcherPriority.Normal);
            }
            if(NoInvoke())
            {
                RaiseIsWorkingChanged();
            }
        }

        void RaiseIsWorkingChanged()
        {
            OnPropertyChanged(IsWorkingArgs);
        }

        public static readonly PropertyChangedEventArgs IsWorkingArgs = PropertyChangedHelper.CreateArgs<AsyncCommandGroup>(c => c.IsWorking);

        public bool IsWorking
        {
            get
            {
                return this.Any(c => c.IsWorking);
            }
        }
    }

    public interface IAsyncCommandGroup : INotifyPropertyChanged
    {
        void Add(IExtendedCommand command);
        bool Remove(IExtendedCommand command);
        bool IsWorking { get; }
    }
}
