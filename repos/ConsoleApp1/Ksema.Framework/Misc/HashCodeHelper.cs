using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ksema.Framework
{
    public class HashCodeHelper
    {
        public static int GetHashCode(params object[] fields)
        {
            return fields.Aggregate(17, (hashSoFar, field) => 23 * hashSoFar + (field ?? 0).GetHashCode());
        }
    }
}
