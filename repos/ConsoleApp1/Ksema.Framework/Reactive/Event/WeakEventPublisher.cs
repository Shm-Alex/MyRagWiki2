using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ksema.Framework.Reactive
{
    public class WeakEventPublisher : DefaultEventPublisher
    {
        public override IObservable<TEvent> GetEvent<TEvent>()
        {
            var subj = base.GetEvent<TEvent>();
            return GetWeakEvent(subj);
        }
    }
}
