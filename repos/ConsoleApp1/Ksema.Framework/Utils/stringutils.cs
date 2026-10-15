using System;
using System.Text;

namespace Framework.Utils
{
	public class DelimiterInserter
	{
		bool _first;
		string _delimiter;

		public DelimiterInserter(string delimiter)
		{
			if(Utils.isEmpty(delimiter))
				throw new ArgumentNullException("delimiter");

			_first = true;
			_delimiter = delimiter;
		}

		public void setup()
		{
			_first = true;
		}

		public StringBuilder appendDelimiter(StringBuilder sb)
		{
			if(_first)
				_first = false;
			else
				sb.Append(_delimiter);

			return sb;
		}

		public string appendDelimiter(string s)
		{
			if(_first)
				_first = false;
			else
				s = s + _delimiter;

			return s;
		}

		public static int GetComaNotInBracket(string f)
		{
			if (string.IsNullOrEmpty(f))
				return -1;
			int pos = 0;
			for(;pos<f.Length;pos++)
			{
				if (f[pos] == ',')
					break;
				if (f[pos] == '(')
				{
					int br = 1;
					while( ++pos < f.Length )
					{
						if (f[pos] == '(')
						{
							br++;
						}
						else if (f[pos] == ')')
							br--;

						if (br == 0)
							break;
					}
				}
			}

			return pos;
		}
		public static int GetLastBracket (string f, string sub)
		{
			if (string.IsNullOrEmpty(f) )//|| string.IsNullOrEmpty(sub))
				return -1;
			int br = 0;
			int pos = 0;
			if (!string.IsNullOrEmpty(sub))
			{
				pos = f.IndexOf(sub, StringComparison.InvariantCultureIgnoreCase);
				if (pos < 0)
					return -1;
			}

			bool fnd = false;
			for (int currP = pos + sub.Length; currP < f.Length; currP++)
			{
				if (f[currP] == '(')
				{
					br++;
					fnd = true;
				}
				else if (f[currP] == ')')
					br--;
				if (br == 0 && fnd)
					return currP + 1;
			}
			return -1;
		}
	}

}