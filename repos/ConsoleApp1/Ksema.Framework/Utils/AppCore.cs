using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Framework.Localization;
using Framework.Macro;
using Ksema.Framework.Diagnostics;
using System.Diagnostics;
using Ksema.Framework.Reactive;

namespace Ksema.Framework
{
	public interface IApplication
	{
		ILocalizer Localizer
		{
			get;
			set;
		}
		IMacroManager MacroManager
		{
			get;
			set;
		}

		Exception ProcessError(Exception error);

		T GetSettings<T>();
	}

	public class EssentialAppMgr
	{
		public static IApplication App
		{
			get;
			set;
		}
	}

	public static class AppMgrLocalizer
	{
		public static string Localize(this string id)
		{
			try
			{
				if (EssentialAppMgr.App == null || EssentialAppMgr.App.Localizer == null)
					return id;
				return EssentialAppMgr.App.Localizer.LocalizeString(id, id);
			}
			catch (Exception error)
			{
				if (Debugger.IsAttached)
					Debugger.Break();
				Tracer.Error(error);
				return id;
			}
		}

		public static string Localize(this string id, params object[] data)
		{
			id = Localize(id);
			return id != null ? String.Format(id, data) : id;
		}
	}
}
