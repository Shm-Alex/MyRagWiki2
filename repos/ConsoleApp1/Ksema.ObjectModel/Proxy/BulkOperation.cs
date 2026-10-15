using System;
using System.Collections.Generic;
using Framework.Utils;
using Ksema.Framework.Collections;

namespace Ksema.ObjectModel
{
	public interface IBulkOperationObject
	{
		void Prepare(BulkOperation context);
		void Commit(BulkOperation context);
	}

	public class BulkOperation : INamedServiceProvider, IDisposable
	{
		Dictionary<string, object> _serviceDictionary = new Dictionary<string,object>();
		LinkedList<IBulkOperationObject> _commitList = new LinkedList<IBulkOperationObject>();

		public T GetService<T>(string key)
		{
			return (T)_serviceDictionary[key];
		}

		public T GetService<T>(string key, Func<T> activator)
		{
			return _serviceDictionary.GetValueOrDefault(key, activator);
		}

		private void ClearState()
		{
			_serviceDictionary.Clear();
			_commitList.Clear();
		}

		public void PrepareObject(IBulkOperationObject obj)
		{
			try
			{
				obj.Prepare(this);
				_commitList.AddLast(obj);
			}
			catch
			{
				ClearState();
				throw;
			}
		}

		public static void Process(IEnumerable<IBulkOperationObject> objList)
		{
			using (BulkOperation op = new BulkOperation())
			{
				foreach (IBulkOperationObject obj in objList)
					op.PrepareObject(obj);
			}
		}

		public void Commit()
		{
			try
			{
				_commitList.ForEach(obj => obj.Commit(this));
			}
			finally
			{
				ClearState();
			}
		}

		public void Dispose()
		{
			Commit();
		}
	}
}