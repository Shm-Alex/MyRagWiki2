using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ksema.Framework.Database
{
    public static class DataReaderExtensions
    {
        public static IEnumerable<Object[]> DataRecord(this System.Data.IDataReader source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            while (source.Read())
            {
                Object[] row = new Object[source.FieldCount];
                source.GetValues(row);
                yield return row;
            }
        }
    }

}
