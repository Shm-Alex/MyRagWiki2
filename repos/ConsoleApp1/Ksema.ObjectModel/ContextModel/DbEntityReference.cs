using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Framework.Utils;

namespace Ksema.ObjectModel.ContextModel
{
    public class DbEntityCSTypeProvider : ICSPropTypeProvider
    {
        public Type GetSingleLinkType()
        {
            return typeof(DbEntityReference<,>);
        }

        public Type GetMultiLinkType()
        {
            return typeof(DbEntitySet<,>);
        }

        public Type GetKeyMultiLinkType()
        {
            return typeof(CSKeyMultiLink<,,,>);
        }

        public Type GetAttValueType()
        {
            return typeof(CSAttValue<>);
        }

        static readonly DbEntityCSTypeProvider s_instance = new DbEntityCSTypeProvider();
        public static ICSPropTypeProvider Instance
        {
            get { return s_instance;  }
        }
    }

    public class DbEntitySet<TOwner, TLinked> : CSMultiLink<TOwner, TLinked>, IInitializableMember
        where TOwner : class, IContextObject, IUniqueKeyProvider<string>
        where TLinked : class, IContextObject, IUniqueKeyProvider<string>
    {
        bool _inLoad;
        public bool IsLoaded { get; private set; }
        public string BackRefEntityName { get; set; }

        public DbEntitySet(string refName, string backRefName, bool isComposite, TOwner owner)
            : base(refName, backRefName, isComposite, owner)
        {
            IsLoaded = false;
        }

        public DbEntitySet(string refName, string backRefName, TOwner owner)
            : base(refName, backRefName, owner)
        {
            IsLoaded = false;
        }

        public IList<TLinked> Load()
        {
            DateTime dt = Owner.Context.ActualDate ?? DateTime.Now;
            return Load(dt);
        }

        public IList<TLinked> Load(DateTime dt)
        {
            AssertUtils.IsNotNull(Owner.Context, "Owner.Context");
            AssertUtils.IsNotEmpty(BackRefName, "BackRef Name");
            AssertUtils.IsNotEmpty(BackRefEntityName, "BackRef Entity Name");

            if (_inLoad)
                throw new InvalidOperationException("Object is already in load state");

            _inLoad = true;
            try
            {
                ICollection result = Owner.Context.Storage.LoadReferencesToObject(
                    Owner, BackRefName, BackRefEntityName, dt);
                List<TLinked> loadedList = new List<TLinked>(result.Count);
                using (Owner.Context.StartLoad())
                    foreach (TLinked linked in result)
                    {
                        loadedList.Add(linked);
                        Add(linked);
                    }

                IsLoaded = true;

                return loadedList;
            }
            finally
            {
                _inLoad = false;
            }
        }

        public void Initialize(MemberInfo info)
        {
            object[] attList = info.GetCustomAttributes(typeof(DbEntityNameAttribute), false);
            if (attList != null && attList.Length > 0)
            {
                DbEntityNameAttribute att = (DbEntityNameAttribute) attList[0];
                BackRefEntityName = att.EntityName;
            }
        }
    }
    public class DbEntityReference<TOwner,TLinked> : CSSingleLink<TOwner,TLinked>
        where TOwner : class, IContextObject, IUniqueKeyProvider<string>
        where TLinked : class, IContextObject,IUniqueKeyProvider<string>
    {
        bool _inLoad;
        public bool IsLoaded { get; private set; }

        public DbEntityReference(string refName, string backRefName, bool isComposite, TOwner owner)
            : base(refName, backRefName, isComposite, owner)
        {
            IsLoaded = false;
        }

        public DbEntityReference(string refName, string backRefName, TOwner owner) : base(refName, backRefName, owner)
        {
            IsLoaded = false;
        }

        public TLinked Load()
        {
            DateTime dt = Owner.Context.ActualDate ?? DateTime.Now;
            return Load(dt);
        }

        public TLinked Load(DateTime dt)
        {
            AssertUtils.IsNotNull(Owner.Context, "Owner.Context");
            if (_inLoad)
                throw new InvalidOperationException("Object is already in loading state");

            _inLoad = true;
            try
            {
                TLinked result = (TLinked)Owner.Context.Storage.LoadReference(Owner, Name, dt);

                using (Owner.Context.StartLoad())
                    LinkedObject = result;

                IsLoaded = true;

                return result;
            }
            finally
            {
                _inLoad = false;
            }
        }

        //public override TLinked LinkedObject
        //{
        //    get
        //    {
        //        TLinked linked = base.LinkedObject;
        //        if (!_inLoad && !IsLoaded && linked == null && Owner.Context != null)
        //            return Load();
        //        return linked;
        //    }
        //    set
        //    {
        //        base.LinkedObject = value;
        //    }
        //}
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class DbEntityNameAttribute : Attribute
    {
        public string EntityName { get; private set; }

        public DbEntityNameAttribute(string entityName)
        {
            EntityName = entityName;
        }
    }

    //TODO: в дальнейшем можно сделать объединение 
    //IInitializableMember & DbNameEntityAttribute & CSReferenceAttribute
}
