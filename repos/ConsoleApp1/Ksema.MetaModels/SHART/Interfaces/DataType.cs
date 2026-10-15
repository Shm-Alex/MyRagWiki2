using System;
using System.Collections.Generic;
using System.Text;

namespace Shart.ModelDef
{
	public enum DataType
	{
		Number = 0,
		String = 1,
		Boolean = 2,
		DateTime = 3,
        Integer = 4,
        LongBinary = 5,
        LongInteger = 6,
		Text = 7,
		Image = 8
	}

    public enum DatabaseTable
    {
        MainTable,
        ViewTable
    }
}
