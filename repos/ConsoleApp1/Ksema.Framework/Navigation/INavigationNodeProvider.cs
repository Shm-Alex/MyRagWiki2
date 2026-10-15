using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ksema.Framework.Navigation
{
    public interface INavigationNodeProvider
    {
        bool Open(NavigationState state);
    }

	public interface IIsSavingAllowed
	{
		string IsImportRunning ();
		void EnterImportRuningMode();
		void ExitImportRuningMode();
		void WaitWhileSaveBiFinished ();
	}
}
