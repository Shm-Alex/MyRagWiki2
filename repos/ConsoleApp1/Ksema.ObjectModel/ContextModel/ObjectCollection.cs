using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using C5;
using Framework.Collections;
using Framework.Utils;

namespace Ksema.ObjectModel.ContextModel
{
	public interface ICheckDepTimeDependency
	{
		string CheckTimeParent();
	}

	[Flags]
	public enum ObjectChangeScope { Values = 1, Links = 2, All = Values | Links };

	public static class ObjectChangeScopeExtensions
	{
		public static bool IsValueChanged(this ObjectChangeScope scope)
		{
			return (scope & ObjectChangeScope.Values) == ObjectChangeScope.Values;
		}
		public static bool IsLinkChanged(this ObjectChangeScope scope)
		{
			return (scope & ObjectChangeScope.Links) == ObjectChangeScope.Links;
		}

	}

	public class ObjectCollectionChangeEventArgs : EventArgs
	{
		public ObjectCollectionChangeEventArgs(object aObj, bool aForSave)
		{
			Object = aObj;
			ForSave = aForSave;
		}

		public bool ForSave { get; set; }
		public object Object { get; set; }
	}

	public interface IObjectCollection : IEnumerable
	{
		bool HasChanges { get; }
		ObjectChangeSet GetChanges();
		void ResetChanges();

		bool Add(object obj, bool forInsert);
		void AddMany(IEnumerable list, bool forInsert);

		void Remove(object obj, bool forDelete);
		void RemoveMany(IEnumerable list, bool forDelete);
		void RemoveAll(bool forDelete);

		bool Contains(object item);
		object FindByPrimaryKey(string key);

		IEnumerable ModifiedObjects { get; }

		IEnumerable DeletedObjects { get; }

		IEnumerable NewObjects { get; }


		bool IsNew(object obj);
		bool IsModified(object obj, ObjectChangeScope scope);
		bool IsDeleted(object obj);

		int Count { get; }

		void MarkAsModified(object obj);
		void MarkAsNew(object obj);

		event EventHandler<ObjectCollectionChangeEventArgs> ObjectAdded;
		event EventHandler<ObjectCollectionChangeEventArgs> ObjectRemoved;
		event EventHandler<ObjectCollectionChangeEventArgs> ObjectChanged;
		IObjectLookup<K> GetIndexCollection<K>(string name);

		/// <summary>
		/// Do not use from user code
		/// </summary>
		void ClearDeletedObject(object obj);
	}

	[Serializable]
	class UniqueKeyIndexer<TObj, TKey> : IIndexer<TObj, TKey>
		where TObj : class, IUniqueKeyProvider<TKey>
	{
		readonly IEqualityComparer<TKey> _keyComparer;

		public UniqueKeyIndexer()
		{
		}


		public UniqueKeyIndexer(IEqualityComparer<TKey> aKeyComparer)
		{
			_keyComparer = aKeyComparer;
		}

		public TKey GetIndexKey(TObj obj)
		{
			if (obj == null)
				throw new ArgumentNullException("obj");
			return obj.UniqueKey;
		}

		public IEqualityComparer<TKey> GetKeyComparer()
		{
			return _keyComparer ?? System.Collections.Generic.EqualityComparer<TKey>.Default;
		}
	}

	public class CustomSaveAction
	{
		public CustomSaveAction()
		{
		}
		public CustomSaveAction(Action action, SaveActionType type, SaveOrderType order)
		{
			Action = action;
			Type = type;
			Order = order;
		}

		public Action Action { get; set; }
		public SaveActionType Type { get; set; }
		public SaveOrderType Order { get; set; }
		public bool Executed { get; set; }
	}

	public class ObjectCollection<T> : IObjectCollection, System.Collections.Generic.ICollection<T>
		where T : class, IUniqueKeyProvider<string>, IContextObject
	{
		readonly ObjectContext _context;
		readonly Dictionary<T, T> _objects;
		readonly System.Collections.Generic.HashSet<T> _newObjects;
		readonly System.Collections.Generic.HashSet<T> _deletedObjects;
		readonly System.Collections.Generic.HashSet<T> _modifiedObjects;
		readonly System.Collections.Generic.ICollection<T> _RO_newObjects;
		readonly System.Collections.Generic.ICollection<T> _RO_deletedObjects;
		readonly System.Collections.Generic.ICollection<T> _RO_modifiedObjects;

		readonly ObjectIndex<T, string> _primaryKey;
		readonly Dictionary<string, IObjectIndex<T>> _indexes;

		C5.MultiHashDictionary<string, CustomSaveAction> _customActions;

		internal ObjectCollection(ObjectContext context, IEqualityComparer<T> comparer)
		{
			if (context == null)
				throw new ArgumentNullException("context");
			if (comparer == null)
				throw new ArgumentNullException("comparer");

			_context = context;

			_objects = new Dictionary<T, T>(comparer);
			_modifiedObjects = new System.Collections.Generic.HashSet<T>(comparer);
			_newObjects = new System.Collections.Generic.HashSet<T>(comparer);
			_deletedObjects = new System.Collections.Generic.HashSet<T>(comparer);

			_RO_deletedObjects = _deletedObjects.AsReadOnly();
			_RO_modifiedObjects = _modifiedObjects.AsReadOnly();
			_RO_newObjects = _newObjects.AsReadOnly();

			//TODO: параметры UniqueKeyIndexer
			_primaryKey = new ObjectIndex<T, string>("PK", new UniqueKeyIndexer<T, string>(), true);

			_indexes = new Dictionary<string, IObjectIndex<T>>(StringComparer.OrdinalIgnoreCase);

			_customActions = new C5.MultiHashDictionary<string, CustomSaveAction>();
		}


		public event EventHandler<ObjectCollectionChangeEventArgs> ObjectAdded;
		public event EventHandler<ObjectCollectionChangeEventArgs> ObjectRemoved;
		public event EventHandler<ObjectCollectionChangeEventArgs> ObjectChanged;


		public System.Collections.Generic.ICollection<T> ModifiedObjects
		{
			get { return _RO_modifiedObjects; }
		}

		public System.Collections.Generic.ICollection<T> DeletedObjects
		{
			get { return _RO_deletedObjects; }
		}

		public System.Collections.Generic.ICollection<T> NewObjects
		{
			get { return _RO_newObjects; }
		}

		IEnumerable IObjectCollection.ModifiedObjects
		{
			get { return _RO_modifiedObjects; }
		}

		IEnumerable IObjectCollection.DeletedObjects
		{
			get { return _RO_deletedObjects; }
		}

		IEnumerable IObjectCollection.NewObjects
		{
			get { return _RO_newObjects; }
		}

		public IEqualityComparer<T> Comparer
		{
			get { return _objects.Comparer; }
		}

		public void AddCustomAction(T obj, CustomSaveAction actionInfo)
		{
			AssertUtils.IsNotNull(obj, "obj");
			AssertUtils.IsNotNull(actionInfo, "actionInfo");

			_customActions.Add(obj.UniqueKey, actionInfo);
		}

		public void MarkAsModified(T obj)
		{
			if (IsExist(obj))
			{
				_modifiedObjects.Add(obj);
				OnObjectChanged(obj, true);
			}
		}

		public void MarkAsNew(T obj)
		{
			if (IsExist(obj) && !IsDeleted(obj))
			{
				_newObjects.Add(obj);
				OnObjectAdded(obj, true);
			}
		}

		public void ClearDeletedObject(object o)
		{
			T obj = (T)o;
			_primaryKey.SetObjectLoaded(obj, false);
			foreach (IObjectIndex<T> index in _indexes.Values)
				index.SetObjectLoaded(obj, false);

			_deletedObjects.Remove(obj);
		}

		void IObjectCollection.MarkAsModified(object obj)
		{
			MarkAsModified((T)obj);
		}

		void IObjectCollection.MarkAsNew(object obj)
		{
			MarkAsNew((T)obj);
		}

		public bool HasChanges
		{
			get { return _modifiedObjects.Count > 0 || _newObjects.Count > 0 || _deletedObjects.Count > 0; }
		}

		public void ResetChanges()
		{
			var notify = _modifiedObjects.Concat(_deletedObjects).Concat(_newObjects)
							.Distinct().ToList();
			_modifiedObjects.Clear();
			_deletedObjects.Clear();
			_newObjects.Clear();
			_customActions.Clear();

			foreach (var o in notify)
				o.__OnStateChanged();
		}

		bool IObjectCollection.IsNew(object obj)
		{
			return IsNew((T)obj);
		}

		public bool IsNew(T obj)
		{
			return _newObjects.Contains(obj);
		}

		bool IObjectCollection.IsModified(object obj, ObjectChangeScope scope)
		{
			return IsModified((T)obj, scope);
		}

		public bool IsModified(T obj, ObjectChangeScope scope)
		{
			bool isModified = IsNew(obj);
			if (!isModified)
			{
				if (scope.IsValueChanged())
					isModified = _modifiedObjects.Contains(obj);
			}
			return isModified;
		}

		public bool IsExist(T obj)
		{
			return _objects.ContainsKey(obj);
		}
		public bool IsDeleted(T obj)
		{
			return _deletedObjects.Contains(obj);
		}

		bool IObjectCollection.IsDeleted(object obj)
		{
			return IsDeleted((T)obj);
		}

		public ObjectContext Context
		{
			get { return _context; }
		}

		public void AddMany(IEnumerable<T> list, bool forInsert)
		{
			foreach (T item in list)
				Add(item, forInsert);
		}

		public bool Add(T obj, bool forInsert)
		{
			lock (this.Context)
			{
				if (obj == null)
					throw new ArgumentNullException("obj");

				if (IsExist(obj))
				{
					T existingObj = _objects[obj];
					if (!ReferenceEquals(existingObj, obj))
						throw new InvalidOperationException("Cannot add new object: collection contains different object with the same key");
					return false;
				}

				if (forInsert && obj is ICheckDepTimeDependency )
				{
					string msg = ( obj as ICheckDepTimeDependency ).CheckTimeParent();
					if (!string.IsNullOrEmpty(msg))
						throw new Exception(msg);
				}

				if (obj.Context != null && !ReferenceEquals(_context, obj.Context))
					throw new InvalidOperationException("Object already belongs to another context");

				using (ContextEventScope scope = new ContextEventScope())
				{
					obj.Context = _context;
					_objects.Add(obj, obj);
					IndexObject(obj);
					StartTrackChanges(obj);
					if (forInsert)
						_newObjects.Add(obj);
					_context.ProcessAddLinks(obj, forInsert);

					obj.OnObjectAdded(forInsert);

					if (ObjectAdded != null)
						scope.EnlistAction(() => ObjectAdded(this, new ObjectCollectionChangeEventArgs(obj, forInsert)));

					scope.Complete();
				}
				return true;
			}
		}

		protected virtual void OnObjectAdded(object obj, bool forSave)
		{
			if (ObjectAdded != null)
				ObjectAdded(this, new ObjectCollectionChangeEventArgs(obj, forSave));
			var co = obj as IContextObject;
			if (co != null)
			{
				co.__OnStateChanged();
			}
		}

		protected virtual void OnObjectChanged(object obj, bool forSave)
		{
			if (ObjectChanged != null)
				ObjectChanged(this, new ObjectCollectionChangeEventArgs(obj, forSave));
			var co = obj as IContextObject;
			if (co != null)
			{
				co.__OnStateChanged();
			}
		}

		object IObjectCollection.FindByPrimaryKey(string key)
		{
			return _primaryKey[key].FirstOrDefault();
		}

		public T FindByPrimaryKey(string key)
		{
			return _primaryKey[key].FirstOrDefault();
		}

		void IndexObject(T obj)
		{
			_primaryKey.Add(obj);
			foreach (IObjectIndex<T> index in _indexes.Values)
				index.Add(obj);
		}

		void RemoveFromIndex(T obj, bool forDelete)
		{
			_primaryKey.Remove(obj, forDelete);
			foreach (IObjectIndex<T> index in _indexes.Values)
				index.Remove(obj, forDelete);
		}

		private void StartTrackChanges(T obj)
		{
			if (obj is INotifyValueChanged)
				((INotifyValueChanged)obj).ValueChanged += ev_ValueChanged;
		}

		void ev_ValueChanged(object sender, ValueChangedEventArgs args)
		{
			if (IsExist((T)args.Owner))
			{
				if (args.ForSave)
					_modifiedObjects.Add((T)args.Owner);
				OnObjectChanged(args.Owner, args.ForSave);
			}
		}


		public bool Remove(T obj, bool forDelete)
		{
			if (obj == null)
				throw new ArgumentNullException("obj");

			if (!IsExist(obj))
				return false;

			obj.OnObjectRemoving(forDelete);
			bool isNew = IsNew(obj);

			using (ContextEventScope scope = new ContextEventScope())
			{
				StopTrackChanges(obj);

				RemoveFromIndex(obj, forDelete);
				_context.ProcessRemoveLinks(obj, isNew, forDelete);

				_objects.Remove(obj);
				_modifiedObjects.Remove(obj);

				if (isNew)
				{
					_newObjects.Remove(obj);
					_deletedObjects.Remove(obj);
				}
				else if (forDelete)
				{
					_deletedObjects.Add(obj);
				}

				obj.OnObjectRemoved(forDelete);

				if (ObjectRemoved != null)
					scope.EnlistAction(() => ObjectRemoved(this, new ObjectCollectionChangeEventArgs(obj, forDelete)));

				scope.Complete();
			}

			if (isNew || !forDelete)
				obj.Context = null;

			return true;
		}

		private void StopTrackChanges(T obj)
		{
			if (obj is INotifyValueChanged)
				((INotifyValueChanged)obj).ValueChanged -= ev_ValueChanged;
		}

		public void RemoveMany(IEnumerable<T> list, bool forDelete)
		{
			foreach (T item in list)
				Remove(item, forDelete);
		}

		public void RemoveAll(bool forDelete)
		{
			T[] array = new T[_objects.Count];
			_objects.Keys.CopyTo(array, 0);

			foreach (T item in array)
				Remove(item, forDelete);
		}

		public ObjectChangeSet GetChanges()
		{
			var modified = from obj in _modifiedObjects
						   where !IsNew(obj) && !IsDeleted(obj) && obj.HasChanges()
						   select obj;

			T[] deleted = new T[_deletedObjects.Count];
			_deletedObjects.CopyTo(deleted, 0);

			T[] added = new T[_newObjects.Count];
			_newObjects.CopyTo(added, 0);

			ObjectChangeSet changeSet = new ObjectChangeSet();
			changeSet.ModifiedObjects = new List<T>(modified);
			changeSet.DeletedObjects = deleted;
			changeSet.AddedObjects = added;
			changeSet.CustomActions = _customActions;

			return changeSet;
		}


		bool IObjectCollection.Add(object obj, bool forInsert)
		{
			return Add((T)obj, forInsert);
		}


		void IObjectCollection.AddMany(IEnumerable list, bool forInsert)
		{
			foreach (T obj in list)
				Add(obj, forInsert);
		}

		void IObjectCollection.Remove(object obj, bool forDelete)
		{
			Remove((T)obj, forDelete);
		}

		void IObjectCollection.RemoveMany(IEnumerable list, bool forDelete)
		{
			foreach (T obj in list)
				Remove(obj, forDelete);
		}

		public void AddIndex<K>(ObjectIndex<T, K> index)
		{
			if (index == null)
				throw new ArgumentNullException("index");
			if (_indexes.ContainsKey(index.Name))
				return;

			_indexes.Add(index.Name, index);
			IndexObjects(index);
		}

		public void RemoveIndex(string name)
		{
			IObjectIndex<T> index;
			if (_indexes.TryGetValue(name, out index))
			{
				index.Clear();
				_indexes.Remove(name);
			}
		}

		public ObjectIndex<T, K> GetIndex<K>(string name)
		{
			IObjectIndex<T> index;
			if (_indexes.TryGetValue(name, out index))
				return (ObjectIndex<T, K>)index;
			return null;
		}

		public IObjectLookup<K> GetIndexCollection<K>(string name)
		{
			return GetIndex<K>(name);
		}

		private void IndexObjects(IObjectIndex<T> index)
		{
			foreach (T obj in _objects.Values)
				index.Add(obj);
		}

		#region ICollection

		public void Add(T item)
		{
			Add(item, false);
		}

		public void Clear()
		{
			RemoveAll(false);
		}

		bool IObjectCollection.Contains(object item)
		{
			return Contains((T)item);
		}

		public bool Contains(T item)
		{
			return _objects.ContainsKey(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			_objects.Keys.CopyTo(array, arrayIndex);
		}

		public bool Remove(T item)
		{
			return Remove(item, false);
		}

		public int Count
		{
			get { return _objects.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}


		public IEnumerator<T> GetEnumerator()
		{
			return _objects.Keys.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}

	public class ObjectChangeSet
	{
		public ICollection DeletedObjects
		{
			get;
			set;
		}
		public ICollection AddedObjects
		{
			get;
			set;
		}
		public ICollection ModifiedObjects
		{
			get;
			set;
		}
		public C5.MultiHashDictionary<string, CustomSaveAction> CustomActions
		{
			get;
			set;
		}
	}
}