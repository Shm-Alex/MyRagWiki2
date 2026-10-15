using System;
using System.Collections;
using System.ComponentModel;


namespace Framework.Collections
{

	public class CheckDataEventArgs : EventArgs
	{
		
		public CheckDataEventArgs(object data)
		{
			this.data = data;
		}


		public object Data
		{
			get {return(data);}
		}


		private object data;

	}


	public class OperateDataEventArgs : EventArgs
	{
		
		public OperateDataEventArgs(int index, object data)
		{
			this.index = index;
			this.data = data;
		}


		public int Index
		{
			get {return(index);}
		}


		public object Data
		{
			get {return(data);}
		}


		private int index;

		private object data;

	}


	public class ChangeDataEventArgs : EventArgs
	{
		
		public ChangeDataEventArgs(int index, object oldData, object newData)
		{
			this.index = index;
			olddata = oldData;
			newdata = newData;
		}


		public int Index
		{
			get {return(index);}
		}


		public object OldData
		{
			get {return(olddata);}
		}


		public object NewData
		{
			get {return(newdata);}
		}


		private int index;

		private object newdata;

		private object olddata;

	}


 	public class OnChangedEvents
	{
		
		private EventHandlerList eventTable = new EventHandlerList();
		
		protected OnChangedEvents() 
		{
		}


		protected void CallOnClear()
		{
			EventHandler handler = (EventHandler)eventTable["Clear"];
			if (handler != null) handler(this, EventArgs.Empty);
		}

		
		protected void CallOnClearComplete()
		{
			EventHandler handler = (EventHandler)eventTable["ClearComplete"];
			if (handler != null) handler(this, EventArgs.Empty);
		}


		protected void CallOnValidate(object Data)
		{
			EventHandler handler = (EventHandler)eventTable["Validate"];
			if (handler != null) handler(this, new CheckDataEventArgs(Data));
		}

		
		protected void CallOnInsert(int Index, object Data)
		{
			EventHandler handler = (EventHandler)eventTable["Insert"];
			if (handler != null) handler(this, new OperateDataEventArgs(Index, Data));
		}


		protected void CallOnInsertComplete(int Index, object Data)
		{
			EventHandler handler = ((EventHandler)eventTable["InsertComplete"]);
			if (handler != null) handler(this, new OperateDataEventArgs(Index, Data));
		}


		protected void CallOnRemove(int Index, object Data)
		{
			EventHandler handler = (EventHandler)eventTable["Remove"];
			if (handler != null) handler(this, new OperateDataEventArgs(Index, Data));
		}


		protected void CallOnRemoveComplete(int Index, object Data)
		{
			EventHandler handler = (EventHandler)eventTable["RemoveComplete"];
			if (handler != null) handler(this, new OperateDataEventArgs(Index, Data));
		}


		protected void CallOnSet(int Index, object OldData, object NewData)
		{
			EventHandler handler = (EventHandler)eventTable["Set"];
			if (handler != null) handler(this, new ChangeDataEventArgs(Index, OldData, NewData));
		}


		protected void CallOnSetComplete(int Index, object OldData, object NewData)
		{
			EventHandler handler = (EventHandler)eventTable["SetComplete"];
			if (handler != null) handler(this, new ChangeDataEventArgs(Index, OldData, NewData));
		}
		

		public event EventHandler OnClear
		{
			add
			{
				eventTable.AddHandler("Clear", value);
			}
			remove
			{
				eventTable.RemoveHandler("Clear", value);
			}
		}


		public event EventHandler OnClearComplete
		{
			add
			{
				eventTable.AddHandler("ClearComplete", value);
			}
			remove
			{
				eventTable.RemoveHandler("ClearComplete", value);
			}
		}
		

		public event EventHandler OnValidate
		{
			add
			{
				eventTable.AddHandler("Validate", value);
			}
			remove
			{
				eventTable.RemoveHandler("Validate", value);
			}
		}


		public event EventHandler OnInsert
		{
			add
			{
				eventTable.AddHandler("Insert", value);
			}
			remove
			{
				eventTable.RemoveHandler("Insert", value);
			}
		}	
	

		public event EventHandler OnInsertComplete
		{
			add
			{
				eventTable.AddHandler("InsertComplete", value);
			}
			remove
			{
				eventTable.RemoveHandler("InsertComplete", value);
			}
		}


		public event EventHandler OnRemove
		{
			add
			{
				eventTable.AddHandler("Remove", value);
			}
			remove
			{
				eventTable.RemoveHandler("Remove", value);
			}
		}	
	

		public event EventHandler OnRemoveComplete
		{
			add
			{
				eventTable.AddHandler("RemoveComplete", value);
			}
			remove
			{
				eventTable.RemoveHandler("RemoveComplete", value);
			}
		}


		public event EventHandler OnSet
		{
			add
			{
				eventTable.AddHandler("Set", value);
			}
			remove
			{
				eventTable.RemoveHandler("Set", value);
			}
		}	
	

		public event EventHandler OnSetComplete
		{
			add
			{
				eventTable.AddHandler("SetComplete", value);
			}
			remove
			{
				eventTable.RemoveHandler("SetComplete", value);
			}
		}


	}


}
