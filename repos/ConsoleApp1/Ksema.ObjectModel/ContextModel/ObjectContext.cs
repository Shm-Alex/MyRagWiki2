using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Framework.Utils;
using System.Collections.ObjectModel;
using System.Threading;
using Ksema.Framework;
using Ksema.Framework.Navigation;
using Ksema.Framework.Reactive;
using Ksema.Framework.ModelView;

namespace Ksema.ObjectModel.ContextModel
{
	public class OperationResultEventArgs : EventArgs
	{
		readonly bool _success;

		public OperationResultEventArgs(bool aSuccess)
		{
			_success = aSuccess;
		}

		public bool Success
		{
			get { return _success; }
		}
	}
	public class ActualDateChangedEventArgs : EventArgs
	{
		public DateTime OldActualDate { get; set; }
		public DateTime NewActualDate { get; set; }
	}

	public class ContextEventScope : UnitOfWorkScope<ObjectContext>
	{
	}

	public abstract class ObjectContext : IUnitOfWorkHost<ObjectContext>
	{
		Ksema.Framework.Reactive.IEventPublisher eventPublisher;
		public Ksema.Framework.Reactive.IEventPublisher EventPublisher
		{
			get
			{
				if (eventPublisher == null)
				{
					eventPublisher = new DefaultEventPublisher();
				}
				return eventPublisher;
			}
		}

		public bool ValidationCorrect()
		{
			return !ValidationErrors.Any();
		}
		public bool Validate()
		{
			var error = ValidationErrors.FirstOrDefault();
			//if (Assembly.GetEntryAssembly()?.GetName().Name == "Nbrb.Client" && ValidationErrors != null && ValidationErrors.Any())
			//{
			//		this.Resolve<IDialogService>()
			//			.ErrorMessage("Ошибки :\r\n" + string.Join("\r\n", ValidationErrors.Select(x => x.ErrorContent.ToString())));
				
			//}

			if (error != null)
			{
				string uri = null;
				var provider = error.Uri as IObjectURIProvider;
				if (provider != null)
					uri = provider.CreateURI().ToString();
				else
				    uri = error.Uri as string;
				if (!String.IsNullOrEmpty(uri))
				{
					var nav = ObjectContainer.Instance.Resolve<INavigationManager>();
					if (nav != null)
					{
						nav.Open(uri);
					}
				}
				return false;
			}
			return true;
		}

		public IEnumerable<IValidationError> ValidationErrors
		{
			get
			{
				return ValidationErrorProviders.SelectMany(vp => vp.Errors);
			}
		}

		ObservableCollection<IValidationErrorProvider> _errorProviders;

		public ObservableCollection<IValidationErrorProvider> ValidationErrorProviders
		{
			get
			{
				if (_errorProviders == null)
				{
					_errorProviders = new ObservableCollection<IValidationErrorProvider>();
				}
				return _errorProviders;
			}
		}

		private readonly ICSObjectPropertyManager _propMgr;
		readonly Dictionary<Type, IObjectCollection> _objects;
	    private static Dictionary<string, Dictionary<int, bool>> _updateObjects = new Dictionary<string, Dictionary<int, bool>>();
        private static Dictionary<string, Dictionary<int, bool>> _saveUpdateObjects = new Dictionary<string, Dictionary<int, bool>>();
	    private object _locker_UpdateNodes = new object();
        readonly Dictionary<Type, Dictionary<string, IObjectRelation>> _relations;
		readonly C5.MultiHashDictionary<Type, IObjectRelation> _forwardTypeRelations;
		readonly C5.MultiHashDictionary<Type, IObjectRelation> _backwardTypeRelations;

		protected ObjectContext(ICSObjectPropertyManager propMgr)
		{
			_propMgr = propMgr;
			_objects = new Dictionary<Type, IObjectCollection>();
			_relations = new Dictionary<Type, Dictionary<string, IObjectRelation>>();
			_forwardTypeRelations = new C5.MultiHashDictionary<Type, IObjectRelation>();
			_backwardTypeRelations = new C5.MultiHashDictionary<Type, IObjectRelation>();
		}

		public event EventHandler<ObjectCollectionChangeEventArgs> ObjectAdded;
		public event EventHandler<ObjectCollectionChangeEventArgs> ObjectRemoved;
		public event EventHandler<ObjectCollectionChangeEventArgs> ObjectChanged;
	    public event EventHandler ObjectUpdate;
        public event EventHandler<CancelEventArgs> BeforeSave;
		public event EventHandler AfterSave;
		public event EventHandler<ObjectRelationChangedArgs> ObjectRelationChanged;
		protected abstract IObjectStorage CreateStorage();

		public bool HasChanges
		{
			get
			{
				try
				{
					foreach (IObjectCollection coll in _objects.Values)
					{
						if (coll != null && coll.HasChanges)
							return true;
					}
				}
				catch (InvalidOperationException)
				{
					return HasChanges;
				}
				return false;
			}
		}

		public ICSObjectPropertyManager PropertyManager
		{
			get { return _propMgr; }
		}


		public bool Save()
		{
			return Save(true);
		}

		public void WaitWhileSaveBiFinished()
		{
			try
			{
				var intf = ObjectContainer.Instance.Resolve<IIsSavingAllowed>();
				intf.WaitWhileSaveBiFinished();
				
			}
			catch
			{
				;
			}
		}
		public bool IsSavingAllowed(out string runningUserName)
		{
			try
			{
				var intf = ObjectContainer.Instance.Resolve<IIsSavingAllowed>();
				runningUserName= intf.IsImportRunning();
				if (string.IsNullOrEmpty(runningUserName))
					return true;
				return false;
			}
			catch
			{
				runningUserName = null;
				return true;
			}
		}

		public bool LockIsImposible = true;
		public void EnterImportRuningMode()
		{
			try
			{
				var intf = ObjectContainer.Instance.Resolve<IIsSavingAllowed>();
				intf.EnterImportRuningMode();
				//LockIsImposible = false;
			}
			catch
			{
				;
			}
		}

		public void ExitImportRuningMode ()
		{
			try
			{
				LockIsImposible = true;
				var intf = ObjectContainer.Instance.Resolve<IIsSavingAllowed>();
				intf.ExitImportRuningMode();
			}
			catch 
			{
				;
			}
		}

        // Коллекция хранящая элементы измененые другими пользователями
	    public void MarkAsUpdate(string name, int id)
	    {
	        if (!_updateObjects.ContainsKey(name))
	        {
	            lock (_locker_UpdateNodes)
	                _updateObjects[name] = new Dictionary<int, bool>();
	        }

	        if (!_updateObjects[name].ContainsKey(id))
	        {
	            lock (_locker_UpdateNodes)
	                _updateObjects[name][id] = true;                               
	            
	        }
	    }


	    public void MarkAsClear(string name, int id)
	    {
	        if (_updateObjects.ContainsKey(name))
	        {
	            if (_updateObjects[name].ContainsKey(id))
	            {
	                lock (_locker_UpdateNodes)
	                {
	                    _updateObjects[name].Remove(id);
	                }
	            }
	        }
	    }

	    public bool IsUpdate(string name, int id)
	    {
	        if (_updateObjects.ContainsKey(name))
	        {
	            if (_updateObjects[name].ContainsKey(id))
	                return true;
	            return false;
	        }

	        return false;
	    }	    

        public void ClearUpdateDictionary(string name)
	    {
            if(_updateObjects.ContainsKey(name))
            {
                lock (_locker_UpdateNodes)
                    _updateObjects.Remove(name);
            }
	    }

	    // Коллекция хранящая элементы сохраненные другими пользователями
        public void MarkAsSaveUpdate(string name, int id)
	    {
	        if (!_saveUpdateObjects.ContainsKey(name))
	        {
	            lock (_locker_UpdateNodes)
	                _saveUpdateObjects[name] = new Dictionary<int, bool>();
	        }

	        if (!_saveUpdateObjects[name].ContainsKey(id))
	        {
	            lock (_locker_UpdateNodes)
	                _saveUpdateObjects[name][id] = true;
	        }
	    }

	    public void ClearSaveUpdateDictionary(string name)
	    {
	        if (_saveUpdateObjects.ContainsKey(name))
	        {
	            lock (_locker_UpdateNodes)
	                _saveUpdateObjects.Remove(name);
	        }
        }

	    public bool IsSaveUpdate(string name, int id)
	    {
	        if (_saveUpdateObjects.ContainsKey(name))
	        {
	            if (_saveUpdateObjects[name].ContainsKey(id))
	                return true;
	            return false;
	        }

	        return false;
	    }

        public bool Save(bool withReset)
		{
            if (!LockIsImposible && IsSavingAllowed(out string userName))
                throw new Exception("NowImortIsRunning.NoSave".Localize(userName));

            if (!OnBeforeSave())
				return false;

			if (!Validate())
				return false;

			IObjectStorage storage = CreateStorage();
			SubscribeToStorageEvents(storage);
			try
			{
				storage.Save();

				if (withReset)
				{
					ResetChanges();
					OnAfterSave();
				}

				return true;
			}
			finally
			{
				UnsubscribeStorageEvents(storage);
			}
		}

		void SubscribeToStorageEvents(IObjectStorage stg)
		{
			stg.AfterCreate += stg_AfterCreate;
			stg.AfterDelete += stg_AfterDelete;
			stg.AfterUpdate += stg_AfterUpdate;
			stg.BeforeCreate += stg_BeforeCreate;
			stg.BeforeDelete += stg_BeforeDelete;
			stg.BeforeUpdate += stg_BeforeUpdate;
		}

		void UnsubscribeStorageEvents(IObjectStorage stg)
		{
			stg.AfterCreate -= stg_AfterCreate;
			stg.AfterDelete -= stg_AfterDelete;
			stg.AfterUpdate -= stg_AfterUpdate;
			stg.BeforeCreate -= stg_BeforeCreate;
			stg.BeforeDelete -= stg_BeforeDelete;
			stg.BeforeUpdate -= stg_BeforeUpdate;
		}

		void stg_BeforeUpdate(object sender, EventArgs e)
		{
			if (BeforeUpdate != null)
				BeforeUpdate(sender, e);
		}

		void stg_BeforeDelete(object sender, EventArgs e)
		{
			if (BeforeDelete != null)
				BeforeDelete(sender, e);
		}

		void stg_BeforeCreate(object sender, EventArgs e)
		{
			if (BeforeCreate != null)
				BeforeCreate(sender, e);
		}

		void stg_AfterUpdate(object sender, EventArgs e)
		{
			if (AfterUpdate != null)
				AfterUpdate(sender, e);
		}

		void stg_AfterDelete(object sender, EventArgs e)
		{
			if (AfterDelete != null)
				AfterDelete(sender, e);
		}

		void stg_AfterCreate(object sender, EventArgs e)
		{
			if (AfterCreate != null)
				AfterCreate(sender, e);
		}

		public event EventHandler BeforeCreate;
		public event EventHandler AfterCreate;
		public event EventHandler BeforeUpdate;
		public event EventHandler AfterUpdate;
		public event EventHandler BeforeDelete;
		public event EventHandler AfterDelete;


		private void OnAfterSave()
		{
			if (AfterSave != null)
				AfterSave(this, EventArgs.Empty);
		}

		private bool OnBeforeSave()
		{
			CancelEventArgs e = new CancelEventArgs(false);
			if (BeforeSave != null)
			{
				BeforeSave(this, e);
				return !e.Cancel;
			}
			return true;
		}

		public event EventHandler ResetContextChanges;

		public void ResetChanges()
		{
			foreach (KeyValuePair<Type, IObjectCollection> objEntry in _objects)
			{
				foreach (KeyValuePair<string, IObjectRelation> relEntry in GetRelations(objEntry.Key))
					relEntry.Value.ResetChanges();
				objEntry.Value.ResetChanges();
			}
			if (ResetContextChanges != null)
			{
				ResetContextChanges(this, EventArgs.Empty);
			}
		}

		public event EventHandler BeforeContextReset;
		public event EventHandler AfterContextReset;

		private void OnBeforeContextReset()
		{
			if (BeforeContextReset != null)
				BeforeContextReset(this, EventArgs.Empty);
		}

		private void OnAfterContextReset()
		{
			if (AfterContextReset != null)
				AfterContextReset(this, EventArgs.Empty);
		}

		public void Reset()
		{
			OnBeforeContextReset();
			ValidationErrorProviders.Clear();
			RemoveAll(false);
			ResetChanges();		    
            OnAfterContextReset();           
		}


		void RemoveAll(bool forDelete)
		{
			foreach (var n in _objects)
			{
				n.Value.RemoveAll(forDelete);
			}
		}

		static readonly KeyValuePair<string, IObjectRelation>[] s_empty = new KeyValuePair<string, IObjectRelation>[0];
		public ICollection<KeyValuePair<string, IObjectRelation>> GetRelations(Type ownerType)
		{
			Dictionary<string, IObjectRelation> result;
			if (_relations.TryGetValue(ownerType, out result))
				return result;
			return s_empty;
		}

		public ICollection<Type> AllTypes
		{
			get { return _objects.Keys; }
		}

		public IObjectCollection SetupForType(Type objType, IEqualityComparer cmp)
		{
			MethodInfo mi = typeof(ObjectContext).GetMethod("SetupForTypeInternal", BindingFlags.NonPublic | BindingFlags.Instance);
			mi = mi.MakeGenericMethod(objType);
			return (IObjectCollection)mi.Invoke(this, new object[] { cmp });
		}

		public IObjectCollection SetupForType(Type objType)
		{
			return SetupForType(objType, null);
		}

		ObjectCollection<T> SetupForTypeInternal<T>(IEqualityComparer<T> comparer)
			where T : class, IUniqueKeyProvider<string>, IContextObject
		{
			IObjectCollection existingColl;
			if (_objects.TryGetValue(typeof(T), out existingColl))
				return (ObjectCollection<T>)existingColl;

			if (comparer == null)
				comparer = EqualityComparer<T>.Default;
			var coll = new ObjectCollection<T>(this, comparer);

			coll.ObjectAdded += coll_ObjectAdded;
			coll.ObjectRemoved += coll_ObjectRemoved;
			coll.ObjectChanged += coll_ObjectChanged;

			_objects[typeof(T)] = coll;

			OnObjectCollectionCreated(coll);
			return coll;
		}

		protected virtual void OnObjectCollectionCreated<T>(ObjectCollection<T> coll)
			where T : class, IUniqueKeyProvider<string>, IContextObject
		{
		}

		protected virtual void OnObjectRelationCreated<TOwner, TLinked>(ObjectRelation<TOwner, TLinked> rel)
			where TOwner : class, IUniqueKeyProvider<string>, IContextObject
			where TLinked : class, IUniqueKeyProvider<string>, IContextObject
		{
		}

		void coll_ObjectChanged(object sender, ObjectCollectionChangeEventArgs e)
		{
			if (ObjectChanged != null)
				ObjectChanged(sender, e);
		}

		void coll_ObjectRemoved(object sender, ObjectCollectionChangeEventArgs e)
		{
			if (ObjectRemoved != null)
				ObjectRemoved(sender, e);
		}

		void coll_ObjectAdded(object sender, ObjectCollectionChangeEventArgs e)
		{
			if (ObjectAdded != null)
				ObjectAdded(sender, e);
		}

		public ObjectCollection<T> SetupForType<T>(IEqualityComparer<T> comparer)
			where T : class, IUniqueKeyProvider<string>, IContextObject
		{
			return SetupForTypeInternal(comparer);
		}

		public ObjectCollection<T> SetupForType<T>()
			where T : class, IUniqueKeyProvider<string>, IContextObject
		{
			return SetupForTypeInternal<T>(null);
		}

		public ObjectRelation<TOwner, TLinked> SetupForRelation<TOwner, TLinked>(ICSLinkInfo linkInfo)
			where TOwner : class, IUniqueKeyProvider<string>, IContextObject
			where TLinked : class, IUniqueKeyProvider<string>, IContextObject
		{
			return SetupForRelationInternal<TOwner, TLinked>(linkInfo);
		}

		private ObjectRelation<TOwner, TLinked> SetupForRelationInternal<TOwner, TLinked>(ICSLinkInfo linkInfo)
			where TOwner : class, IUniqueKeyProvider<string>, IContextObject
			where TLinked : class, IUniqueKeyProvider<string>, IContextObject
		{
			IDictionary<string, IObjectRelation> links = GetLinksDictionary(typeof(TOwner));
			IObjectRelation existingRel;
			if (links.TryGetValue(linkInfo.Name, out existingRel))
				return (ObjectRelation<TOwner, TLinked>)existingRel;

			ObjectCollection<TOwner> ownerColl = (ObjectCollection<TOwner>)_objects[typeof(TOwner)];
			ObjectCollection<TLinked> linkedColl = (ObjectCollection<TLinked>)_objects[typeof(TLinked)];

			ObjectRelation<TOwner, TLinked> rel = new ObjectRelation<TOwner, TLinked>(linkInfo, ownerColl.Comparer, linkedColl.Comparer);
			rel.ObjectRelationChanged += rel_ObjectRelationChanged;
			links[linkInfo.Name] = rel;

			_forwardTypeRelations.Add(typeof(TOwner), rel);
			_backwardTypeRelations.Add(typeof(TLinked), rel);

			OnObjectRelationCreated(rel);

			return rel;
		}

		private void rel_ObjectRelationChanged(object sender, ObjectRelationChangedArgs e)
		{
			if (ObjectRelationChanged != null)
				ObjectRelationChanged(sender, e);
		}

		private IDictionary<string, IObjectRelation> GetLinksDictionary(Type ownerType)
		{
			Dictionary<string, IObjectRelation> links;
			if (!_relations.TryGetValue(ownerType, out links))
			{
				links = new Dictionary<string, IObjectRelation>();
				_relations[ownerType] = links;
			}
			return links;
		}

		public IObjectRelation SetupForRelation(Type ownerType, Type linkedType, ICSLinkInfo info)
		{
			MethodInfo mi =
				typeof(ObjectContext).GetMethod("SetupForRelationInternal",
				BindingFlags.NonPublic | BindingFlags.Instance);
			mi = mi.MakeGenericMethod(ownerType, linkedType);
			return (IObjectRelation)mi.Invoke(this, new object[] { info });
		}

		public ObjectCollection<T> GetCollection<T>()
			where T : class, IUniqueKeyProvider<string>, IContextObject
		{
			return GetCollection<T>(true);
		}

		public ObjectCollection<T> GetCollection<T>(bool createIfNotFound)
			where T : class, IUniqueKeyProvider<string>, IContextObject
		{
			IObjectCollection coll;
			if (!_objects.TryGetValue(typeof(T), out coll) && createIfNotFound)
				coll = SetupForType<T>();

			return (ObjectCollection<T>)coll;
		}

		public bool IsNew(Type tp, object t)
		{
			IObjectCollection coll;
			if ( _objects.TryGetValue(tp, out coll) )
			{
				return coll.IsNew(t);
			}
			return false;
		}


	    public bool IsModify(object obj)
	    {
	        IObjectCollection coll = GetCollection(obj.GetType());
	        if (coll != null)
	            return coll.IsModified(obj, ObjectChangeScope.All);
	        return false;
	    }


        public ObjectRelation<TOwner, TLinked> GetRelation<TOwner, TLinked>(string refName)
			where TOwner : class, IContextObject
			where TLinked : class, IContextObject
		{
			return (ObjectRelation<TOwner, TLinked>)GetRelation(typeof(TOwner), refName);
		}


		public IObjectCollection GetCollection(Type objType, bool createIfNotFound)
		{
			IObjectCollection coll;
			if (!_objects.TryGetValue(objType, out coll) && createIfNotFound)
				coll = SetupForType(objType);
			return coll;
		}

		public IObjectCollection GetCollection(Type objType)
		{
			return GetCollection(objType, true);
		}

		public IObjectRelation GetRelation(Type ownerType, string refName)
		{
			Dictionary<string, IObjectRelation> relTable;
			if (_relations.TryGetValue(ownerType, out relTable))
			{
				IObjectRelation rel;
				if (relTable.TryGetValue(refName, out rel))
					return rel;
			}
			return null;
		}

		public IObjectRelation GetRelation(Type ownerType, Type linkedType, ICSLinkInfo link)
		{
			if (link == null)
				return null;
			IObjectRelation rel = GetRelation(ownerType, link.Name) ?? SetupForRelation(ownerType, linkedType, link);
			return rel;
		}

		public void MarkAsModified(object obj)
		{
			IObjectCollection coll = GetCollection(obj.GetType());
			if (coll != null)
				coll.MarkAsModified(obj);
		}

		public bool Add(object obj)
		{
			return Add(obj, false);
		}

		public bool Add(object obj, bool forInsert)
		{
			lock (this)
			{
				IObjectCollection coll = GetCollection(obj.GetType(), true);
				return coll.Add(obj, forInsert);
			}
		}

		public void ClearDeletedObject(object obj)
		{
			AssertUtils.IsNotNull(obj, "object");
			IObjectCollection coll = GetCollection(obj.GetType());
			if (coll.Contains(obj))
				return;

			foreach (IObjectRelation rel in _forwardTypeRelations[obj.GetType()])
				rel.ClearDeletedLinkedObjects(obj);

			foreach (IObjectRelation rel in _backwardTypeRelations[obj.GetType()])
				rel.ClearDeletedBackLinkedObjects(obj);

			coll.ClearDeletedObject(obj);
		}

		public void ClearDeletedObjects(IEnumerable obj)
		{
			foreach (var o in obj)
			{
				ClearDeletedObject(o);
			}
		}

		public void AddCustomAction<T>(T obj, CustomSaveAction action)
			where T : class, IUniqueKeyProvider<string>, IContextObject
		{
			GetCollection<T>(true).AddCustomAction(obj, action);
		}

		public void Remove(object obj)
		{
			Remove(obj, false);
		}

		public void Remove(object obj, bool forDelete)
		{
			IObjectCollection coll = GetCollection(obj.GetType());
			coll.Remove(obj, forDelete);
		}

		public void AddMany(IEnumerable objList)
		{
			AddMany(objList, false);
		}

		public void AddMany(IEnumerable objList, bool forInsert)
		{
			lock (this)
			{
				object item = objList.getFirstEnumerableItem();
				if (item == null)
					return;
				IObjectCollection coll = GetCollection(item.GetType(), true);
				coll.AddMany(objList, forInsert);
			}
		}

		public void RemoveMany(IEnumerable objList)
		{
			RemoveMany(objList, false);
		}

		public void RemoveMany(IEnumerable objList, bool forDelete)
		{
			object item = objList.getFirstEnumerableItem();
			if (item == null)
				return;

			IObjectCollection coll = GetCollection(item.GetType(), false);
			if (coll != null)
				coll.RemoveMany(objList, forDelete);
		}

		internal void ProcessAddLinks(object obj, bool forInsert)
		{
			foreach (ICSLink link in PropertyManager.getLinkList(obj))
				AddLinkedObjects(obj, link.getLinkedObjects(), link, forInsert);
		}

		internal void ProcessRemoveLinks(object obj, bool isNew, bool forDelete)
		{
			forDelete = isNew ? false : forDelete;

			using (ContextEventScope scope = new ContextEventScope())
			{
				foreach (IObjectRelation rel in _forwardTypeRelations[obj.GetType()])
				{
					ICollection linkedObjects = rel.GetLinkedObjects(obj);

					if (linkedObjects.Count != 0)
					{
						object[] tmpArray = new object[linkedObjects.Count];
						linkedObjects.CopyTo(tmpArray, 0);

						rel.RemoveMany(obj, tmpArray, forDelete);
					}
				}

				foreach (IObjectRelation backRel in _backwardTypeRelations[obj.GetType()])
				{
					ICollection linkedObjects = backRel.GetBackLinkedObjects(obj);

					if (linkedObjects.Count != 0)
					{
						object[] tmpArray = new object[linkedObjects.Count];
						linkedObjects.CopyTo(tmpArray, 0);

						foreach (object linked in tmpArray)
							backRel.Remove(linked, obj, forDelete, isNew);
					}
				}

				scope.Complete();
			}

			//Если что - вернуть этот кусок
			//foreach (ICSLink link in PropertyManager.getLinkList(obj))
			//{
			//    IObjectRelation rel = GetRelation(obj.GetType(), link.Name);
			//    if (rel != null)
			//        RemoveLinkedObjects(obj, rel.GetLinkedObjects(obj), link, forDelete);
			//}

		}

		public void AddLinkedObject(object obj, object linked, string refName, bool forInsert)
		{
			ICSLink link = PropertyManager.getLink(refName, obj);
			AddLinkedObjects(obj, new[] { linked }, link, forInsert);
		}

		public void RemoveLinkedObject(object owner, object linked, string refName, bool forDelete)
		{
			ICSLink link = PropertyManager.getLink(refName, owner);
			RemoveLinkedObjects(owner, new[] { linked }, link, forDelete);
		}

		void RemoveLinkedObjects(object owner, ICollection linkedObjects, ICSLinkInfo link, bool forDelete)
		{
			lock (this)
			{
				if (linkedObjects.Count == 0)
					return;

				object[] tmpArray = new object[linkedObjects.Count];
				linkedObjects.CopyTo(tmpArray, 0);

				using (ContextEventScope scope = new ContextEventScope())
				{
					var firstItem = linkedObjects.getFirstItem();
					Type linkedType = firstItem.GetType();
					IObjectRelation rel = GetRelation(owner.GetType(), linkedType, link);

					rel.RemoveMany(owner, tmpArray, forDelete);

					if (!string.IsNullOrEmpty(link.BackRefName))
					{
						ICSLinkInfo backLink = PropertyManager.getLink(link.BackRefName, firstItem);
						IObjectRelation backRel = GetRelation(linkedType, owner.GetType(), backLink);
						foreach (object linked in tmpArray)
							backRel.Remove(linked, owner, forDelete);
					}
					scope.Complete();
				}
			}
		}

		void AddLinkedObjects(object obj, ICollection linkedObjects, ICSLinkInfo link, bool forInsert)
		{
			lock (this)
			{
				if (linkedObjects.Count == 0)
					return;

				using (ContextEventScope scope = new ContextEventScope())
				{
					var firstItem = linkedObjects.getFirstItem();
					Type linkedType = firstItem.GetType();
					IObjectCollection linkedColl = GetCollection(linkedType);
					IObjectRelation rel = GetRelation(obj.GetType(), linkedType, link);

					rel.AddMany(obj, linkedObjects, forInsert);

					if (!string.IsNullOrEmpty(link.BackRefName))
					{
						ICSLinkInfo backLink = PropertyManager.getLink(link.BackRefName, firstItem);
						IObjectRelation backRel = GetRelation(linkedType, obj.GetType(), backLink);
						foreach (object linkedObj in linkedObjects)
							backRel.Add(linkedObj, obj, forInsert);
					}

					linkedColl.AddMany(linkedObjects, forInsert);
					scope.Complete();
				}
			}
		}

		[Obsolete]
		public IDisposable StartLoad()
		{
			return new ObjectContextScope(this);
		}

		//TODO: rename to Current
		public static ObjectContext LoadingContext
		{
			get
			{
				return ObjectContextScope.Current == null ? null : ObjectContextScope.Current.Context;
			}
		}

		public bool AddAfterSaveEvent { get; set; }
		public Func<string> DoAfterSave;
	}
}