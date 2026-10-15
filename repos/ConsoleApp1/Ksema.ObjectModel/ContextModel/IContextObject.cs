using System.Collections.ObjectModel;
using System.Collections.Generic;
using System;

namespace Ksema.ObjectModel.ContextModel
{
	public enum ContextObjectState
	{
		New,
		Modified,
		Unchanged
	}

	public interface IContextObject : IContextObjectEvents
	{
		ObjectContext Context { get; set; }
		bool HasChanges();
		ContextObjectState State { get; }
		void __OnStateChanged();
	}

	[Flags]
	public enum SaveActionType { Create = 1, Update = 2, Delete = 4 };
	[Flags]
	public enum SaveOrderType { InsteadOf = 1, Before = 2, After = 4 };

	public interface IContextObjectEvents
	{
		void OnObjectAdded(bool forSave);
		void OnObjectRemoving(bool forSave);
		void OnObjectRemoved(bool forSave);

		void OnObjectSaving(SaveActionType action);
		void OnObjectSaved(SaveActionType action);

		void OnObjectLinkAdded(string refName, object linked, bool forSave);
		void OnObjectLinkRemoved(string refName, object linked, bool forSave, bool isNew = false);
	}
}
