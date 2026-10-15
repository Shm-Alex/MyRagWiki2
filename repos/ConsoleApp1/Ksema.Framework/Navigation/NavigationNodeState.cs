using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ksema.Framework.Navigation
{
    /// <summary>
    /// Состояние узла навигации
    /// </summary>
    public class NavigationNodeState
    {
        public static implicit operator NavigationNodeState[](NavigationNodeState state)
        {
            return new NavigationNodeState[] { state };
        }

        public const string openingIdBracket = "(";
        public const string closingIdBracket = ")";

        public const string openingBracket = "[";
        public const string closingBracket = "]";

        public const string equal = "=";
        public const string delimeter = ",";

		public const char wrapper = '"';

        /// <summary>
        /// Идентифицирующие значения состояния
        /// </summary>
        public Dictionary<string, string> IdValues = new Dictionary<string, string>();

        /// <summary>
        /// Значения состояния
        /// </summary>
        public Dictionary<string, string> Values = new Dictionary<string, string>();

        /// <summary>
        /// Идентифицирующие флаги состояния
        /// </summary>
        public List<string> IdFlags = new List<string>();

        /// <summary>
        /// Флаги состояния
        /// </summary>
        public List<string> Flags = new List<string>();

        /// <summary>
        /// Идентификатор узла, вместе с идентифицирующими флагами и значениями идентифицирует контрол
        /// </summary>
        public string NodeId;

        /// <summary>
        /// Идентификатор контрола
        /// </summary>
        public string ControlId
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append(NodeId);

                var idVals = IdFlags.Concat(IdValues.Select(n => n.Key + equal + n.Value)).ToArray();
                if (idVals.Length > 0)
                {
                    sb.Append(openingIdBracket);
                    sb.Append(String.Join(delimeter, idVals.ToArray()));
                    sb.Append(closingIdBracket);
                }
                return sb.ToString();
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(ControlId);

			var vals = Flags.Concat(Values.Select(n => n.Key + equal + (n.Value != null && n.Value.Contains(delimeter) ? wrapper + n.Value + wrapper : n.Value))).ToArray();
            if(vals.Length > 0)
            {
                sb.Append(openingBracket);
                sb.Append(String.Join(delimeter, vals));
                sb.Append(closingBracket);
            }
            return sb.ToString();
        }

        public static NavigationNodeState FromString(string state)
        {
            var nodeState = new NavigationNodeState();

            string operandsString = null;
            if (state.Contains(openingBracket))
            {
                var openingIndex = IndexOf(state, openingBracket);
				var closingIndex = IndexOf(state, closingBracket);
                nodeState.NodeId = state.Substring(0, openingIndex);
                operandsString = state.Substring(openingIndex + 1, closingIndex - openingIndex - 1);
            }
            else
            {
                nodeState.NodeId = state;
            }

            string operandsIdString = null;
            if (nodeState.NodeId.Contains(openingIdBracket))
            {
				var openingIndex = IndexOf(nodeState.NodeId, openingIdBracket);
				var closingIndex = IndexOf(nodeState.NodeId, closingIdBracket);
                operandsIdString = nodeState.NodeId.Substring(openingIndex + 1, closingIndex - openingIndex - 1);
                nodeState.NodeId = nodeState.NodeId.Substring(0, openingIndex);
            }

            if (!String.IsNullOrEmpty(operandsIdString))
            {
				var operands = Split(operandsIdString, delimeter);

                foreach (var operand in operands)
                {
                    if (operand.Contains(equal))
                    {
						int pos = IndexOf(operand, equal);
                    	string name = operand.Substring(0, pos);
                    	string value = operand.Substring(pos+1, operand.Length - pos - 1);
                        nodeState.IdValues[name.Trim()] = value.Trim();
                    }
                    else
                    {
                        nodeState.IdFlags.Add(operand.Trim());
                    }
                }
            }

            if (!String.IsNullOrEmpty(operandsString))
            {
				var operands = Split(operandsString, delimeter);

                foreach (var operand in operands)
                {
                    if (operand.Contains(equal))
                    {
						int pos = IndexOf(operand, equal);
                    	string name = operand.Substring(0, pos);
                    	string value = operand.Substring(pos + 1, operand.Length - pos - 1);
                        nodeState.Values[name.Trim()] = value.Trim().Trim('"');
                    }
                    else
                    {
                        nodeState.Flags.Add(operand.Trim());
                    }
                }
            }

            return nodeState;
        }

		private static int IndexOf(string text, string substring)
		{
			return IndexOf(text, substring, 0);
		}

		private static int IndexOf(string text, string substring, int start)
		{
			if (String.IsNullOrEmpty(text) || String.IsNullOrEmpty(substring))
				return -1;
			int i = start;
			while (i <= text.Length - substring.Length)
			{
				if (text[i] == wrapper)
				{
					i = text.IndexOf(wrapper, i + 1);
					if (i < 0)
						throw new Exception("Закрывающие кавычки не найдены");
					i++;
					if (i > text.Length - substring.Length)
						return -1;
				}
				if (text.Substring(i, substring.Length) == substring)
					return i;
				i++;
			}
			return -1;
		}

		private static IEnumerable<string> Split(string text, string marker)
		{
			if (String.IsNullOrEmpty(text))
				yield break;
			if (String.IsNullOrEmpty(marker))
			{
				yield return text;
				yield break;
			}
			int i1 = 0;
			int i2 = 0;
			while (i1 < text.Length)
			{
				i2 = IndexOf(text, marker, i1);
				if (i2 == -1)
				{
					yield return text.Substring(i1);
					yield break;
				}
				else
				{
					yield return text.Substring(i1, i2 - i1);
					i1 = i2 + marker.Length;
				}
			}
			if (i1 == text.Length)
				yield return String.Empty;
		}
    }
}
