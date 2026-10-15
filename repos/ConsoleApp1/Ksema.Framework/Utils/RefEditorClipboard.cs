using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Ksema.Framework.Utils
{
	[Serializable]
	public class RefEditorClipboard
	{
		public int ID { get; private set; }
		public string TargetName { get; private set; }
		public string Code { get; private set; }
		public RefEditorClipboard ()
		{
			GenerateCode();
		}
		private void GenerateCode ()
		{
			string txtVal = GetString();
			if (string.IsNullOrEmpty(txtVal))
				return;

			Code = GetCode(txtVal);
		}

		static string GetCode (string txtVal)
		{
			int postBr = txtVal.IndexOf(']');
			if (postBr >= 2)
			{
				return txtVal.Substring(1, postBr - 1);
			}
			return txtVal;
		}
		public RefEditorClipboard (int id, string targetName)
		{
			ID = id;
			TargetName = targetName;
			GenerateCode();
		}

		public static object GetPersistObject ()
		{
			IDataObject data = Clipboard.GetDataObject();
			var formats = data.GetFormats(true);
			string persistObjectFormat = formats.Where(x => x == "PersistentObject").FirstOrDefault();

			if (persistObjectFormat == null)
				return null;

			return data.GetData("PersistentObject");
		}

		private static string[] strFormats = { "UnicodeText", "OEMText", "System.String", "Text" };
		public static bool CanGetObject ()
		{
			IDataObject data = Clipboard.GetDataObject();
			var formats = data.GetFormats(true);
			if (formats.Any(x => x == "PersistentObject"))
				return true;
			return formats.Any(x => strFormats.Contains(x));
		}
		public static string GetString ()
		{
			foreach (string strFormat in strFormats)
			{
				object clipboardText = Clipboard.GetData(strFormat);
				if (clipboardText != null && clipboardText is string)
					return (string)clipboardText;
			}
			return null;
		}

		public static string[] GetCodesFromClipboard ()
		{
			string txt = GetString();
			if (string.IsNullOrEmpty(txt))
				return null;
			return txt.Split(';').Select(x => GetCode(x)).ToArray();
		}
	}
}
