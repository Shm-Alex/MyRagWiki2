using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ksema.Framework.Navigation
{
    public interface INavigationNode
    {
        /// <summary>
        /// Восстановить состояние узла
        /// </summary>
        /// <param name="state"></param>
        void RestoreState(NavigationState state);

        /// <summary>
        /// Локальное состояние узла
        /// </summary>
        NavigationNodeState[] LocalState
        {
            get;
        }
    }
}
