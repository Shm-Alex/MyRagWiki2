using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ksema.Framework.Navigation
{
    /// <summary>
    /// Описание состояния
    /// </summary>
    public class NavigationState
    {
        public const string StateDelim = "/";

        /// <summary>
        /// Состояния отдельных узлов
        /// </summary>
        public List<NavigationNodeState> NodesState = new List<NavigationNodeState>();

        /// <summary>
        /// Получить состояние узла по идентификатору
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public NavigationNodeState this[string nodeId]
        {
            get
            {
                return GetNodeState(nodeId);
            }
        }

        /// <summary>
        /// Получить состояние узла по идентификатору
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public NavigationNodeState GetNodeState(string nodeId)
        {
            foreach (var n in NodesState)
            {
                if (n.NodeId == nodeId)
                    return n;
            }
            return null;
        }

        public override string ToString()
        {
            return String.Join(StateDelim, NodesState.Select(n => n.ToString()).ToArray());
        }

        public static NavigationState FromString(string state)
        {
            string regex = "/(?=[^\\]]*(?:\\[|$))";
            System.Text.RegularExpressions.RegexOptions options = ((System.Text.RegularExpressions.RegexOptions.IgnorePatternWhitespace | System.Text.RegularExpressions.RegexOptions.Multiline)
                        | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex(regex, options);

            var navState = new NavigationState();
            foreach (var nodeState in reg.Split(state)/* state.Split(StateDelim[0])*/)
            {
                navState.NodesState.Add(NavigationNodeState.FromString(nodeState));
            }
            return navState;
        }

        //public void Open()
        //{
        //    if (NavigationManager.IsNavigationManagerAvailable)
        //    {
        //        NavigationManager.Instance.Open(this.ToString());
        //    }
        //}
    }
}
