using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ksema.Framework.Tasks
{
	public class TaskExportTickProcesInfo
	{
		public Dictionary<string, int> SeetUsedRow { get; set; }
		public Dictionary<string, int[]> ParentGuidIntervals { get; set; }

		public TaskExportTickProcesInfo()
		{
			ParentGuidIntervals = new Dictionary<string, int[]>();
			SeetUsedRow = new Dictionary<string, int>();
		}
	}
}
