using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ksema.Framework.Misc
{
	public static class MatchingHelper
	{
		public static T Match<T>(this T val, Func<T,bool> predicate, Action<T> action)
		{
			if (predicate(val))
				action(val);
			return val;
		}
	}
}
