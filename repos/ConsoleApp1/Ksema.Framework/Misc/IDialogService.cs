using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Ksema.Framework
{
    public interface IDialogService
    {
        MessageBoxResult Ask(Window owner, string question, string title, bool showCancel = true);
        MessageBoxResult Ask(string question, string title, bool showCancel = true);
	    string AskDirectoryPath(string question, string title, string initPath = "");
	    string AskFilePath(string question, string title, bool showCancel = true);
		void ErrorMessage(Exception ex);
        void ErrorMessage(string msg);
        void ErrorMessage(string msg, Exception ex);
        void ErrorMessage(string msg, string details);
        void ErrorMessage(string msg, string details, Window ownerWindow);
        void Message(string title, string msg, string details);
        void Message(string msg);
        void Message(string msg, string title, Window ownerWindow);
        void Message(string msg, string title, string details, Window ownerWindow);
        void Message(string msg, string title);
        void SevereErrorMessage(string title, string msg, string details);
    }
}
