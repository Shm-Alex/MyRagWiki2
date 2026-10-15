using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Framework.Utils;

namespace Ksema.ObjectModel
{
	public interface ICSPropertyProvider
	{
		ICSValue GetAttribute(string attName);
		ICSLink GetLink(string refName);
		ICollection<ICSValue> GetAttributeList();
		ICollection<ICSLink> GetLinkList();
	}

    public interface ICSObjectPropertyManager
    {
        ICSValue getAttribute(string attName, object obj);
        ICSLink getLink(string refName, object obj);
        ICollection<ICSValue> getAttributeList(object obj);
        ICollection<ICSLink> getLinkList(object obj);
        void setupObject(object obj, bool initNotNull);
        void setupObject(object obj, bool initNotNull, ICSPropTypeProvider typeProvider);

        bool containsProperty(string propName, CSPropType propType, Type objType);
    }

    public static class CSPropMgr
    {
        static ICSObjectPropertyManager __inst = new CSObjectManagerReflect();
        public static ICSObjectPropertyManager Instance
        {
            get
            {
                return __inst;
            }
            set
            {
                __inst = value;
            }
        }
    }

    public class PropertyInfoEntry
	{
		public MemberInfo MemberInfo;
		public ICSPropertyInfo PropertyInfo;

		public PropertyInfoEntry(MemberInfo member, ICSPropertyInfo propertyInfo)
		{
			MemberInfo = member;
			PropertyInfo = propertyInfo;
		}

		public Type MemberType
		{
			get 
			{  
				Type miType;
	
				if(MemberInfo is FieldInfo)
				{
					miType = ((FieldInfo)MemberInfo).FieldType;
				}
				else if(MemberInfo is PropertyInfo)
				{
					miType = ((PropertyInfo)MemberInfo).PropertyType;
				}
				else
					throw new NotSupportedException("member type is not supported: " + MemberInfo.GetType().FullName);

				return miType;
			}
		}

		public object GetMemberValue(object obj)
		{
		    if(MemberInfo is FieldInfo)
			{
				return ((FieldInfo)MemberInfo).GetValue(obj);
			}
		    if(MemberInfo is PropertyInfo)
		    {
		        return ((PropertyInfo)MemberInfo).GetValue(obj, null);
		    }
		    throw new NotSupportedException("member type is not supported: " + MemberInfo.GetType().FullName);
		}

        public void SetMemberValue(object obj, object value)
	    {
            if (MemberInfo is FieldInfo)
            {
                ((FieldInfo)MemberInfo).SetValue(obj, value);
            }
            else if (MemberInfo is PropertyInfo)
            {
                ((PropertyInfo)MemberInfo).SetValue(obj, value, null);
            }
            else
                throw new NotSupportedException("member type is not supported: " + MemberInfo.GetType().FullName);
        }
	}

	internal class TypeInfoEntry
	{
	    readonly Dictionary<string, PropertyInfoEntry> _propList = new Dictionary<string, PropertyInfoEntry>();

		public TypeInfoEntry(Type objectType, Type attType)
		{
			// Bolick: Поиск по иерархии
			while (objectType != null)
			{
				indexProperties(objectType, attType);
				objectType = objectType.BaseType;
			} 
			// indexProperties(objectType, attType);
		}

		void indexProperties(Type t, Type attType)
		{
			//TODO: Сделать нормальный поиск по иерархии для поддержки "унаследованных" private полей
			MemberInfo[] miList = t.GetMembers(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);// | BindingFlags.FlattenHierarchy);

			foreach(MemberInfo mi in miList)
			{
				object[] attList = mi.GetCustomAttributes(attType, false);
				if(attList != null && attList.GetLength(0) > 0)
				{
					ICSPropertyInfo csp = (ICSPropertyInfo) attList[0];
					_propList[csp.Name] = new PropertyInfoEntry(mi, csp);
				}
			}
		}

		public PropertyInfoEntry getPropertyInfo(string propName)
		{
            PropertyInfoEntry entry;
            _propList.TryGetValue(propName, out entry);
            return entry;
		}


		public ICollection<PropertyInfoEntry> getPropertyInfos()
		{
			return _propList.Values;
		}
	}

	public enum CSPropType {Attribute, Reference};

    public interface IInitializableMember
    {
        void Initialize(MemberInfo info);
    }

    public interface ICSPropTypeProvider
    {
        Type GetSingleLinkType();
        Type GetMultiLinkType();
        Type GetKeyMultiLinkType();
        Type GetAttValueType();
    }

    class DefaultCSPropTypeProvider : ICSPropTypeProvider
    {
        public Type GetSingleLinkType()
        {
            return typeof(CSSingleLink<,>);
        }

        public Type GetMultiLinkType()
        {
            return typeof(CSMultiLink<,>);
        }

        public Type GetKeyMultiLinkType()
        {
            return typeof(CSKeyMultiLink<,,,>);
        }

        public Type GetAttValueType()
        {
            return typeof(CSAttValue<>);
        }
    }

	public class CSObjectManagerReflect : ICSObjectPropertyManager
	{
	    readonly Dictionary<Type, TypeInfoEntry> _refCache = new Dictionary<Type, TypeInfoEntry>();
	    readonly Dictionary<Type, TypeInfoEntry> _attCache = new Dictionary<Type, TypeInfoEntry>();


        public void setupObject(object obj, bool initNotNull)
        {
            setupObject(obj, initNotNull, new DefaultCSPropTypeProvider());
        }

	    public void setupObject(object obj, bool initNotNull, ICSPropTypeProvider typeProvider)
        {
            ICollection<PropertyInfoEntry> propInfoList = GetPropertyInfoList(obj, CSPropType.Reference);
            foreach (PropertyInfoEntry entry in propInfoList)
            {
                if (initNotNull || entry.GetMemberValue(obj) == null)
                {
                    if (ProcessMultiLink(obj, entry, typeProvider))
                        continue;
                    if (ProcessKeyMultiLink(obj, entry, typeProvider))
                        continue;
                    if (ProcessSingleLink(obj, entry, typeProvider))
                        continue;
                }
            }

            propInfoList = GetPropertyInfoList(obj, CSPropType.Attribute);
            foreach (PropertyInfoEntry entry in propInfoList)
            {
                if (initNotNull || entry.GetMemberValue(obj) == null)
                {
                    ProcessValue(obj, entry, typeProvider);
                }
            }

        }


        private static void ProcessValue(object obj, PropertyInfoEntry entry, ICSPropTypeProvider prv)
        {
            Type memberType = GetInterface(entry.MemberType, "ICSValue`1");

            if (memberType != null)
            {
                Type[] args = memberType.GetGenericArguments();
                Type valueGenericType = prv.GetAttValueType();//typeof(CSAttValue<>);
                Type linkType = valueGenericType.MakeGenericType(args);
                object value = Activator.CreateInstance(linkType, entry.PropertyInfo.Name, obj);
                entry.SetMemberValue(obj, value);
            }
        }

	    private static bool ProcessGenericLink(object obj, PropertyInfoEntry entry, string linkInterfaceType, Type linkGenericType)
        {
            Type memberType = GetInterface(entry.MemberType, linkInterfaceType);

            if (memberType != null)
            {
                Type[] args = memberType.GetGenericArguments();
                Type linkType = linkGenericType.MakeGenericType(args);
                ICSLinkInfo linkInfo = (ICSLinkInfo)entry.PropertyInfo;
                object link = Activator.CreateInstance(linkType, linkInfo.Name, linkInfo.BackRefName, linkInfo.IsComposite, obj);
                entry.SetMemberValue(obj, link);
                if (link is IInitializableMember)
                    ((IInitializableMember)link).Initialize(entry.MemberInfo);
                return true;
            }
	        return false;
        }

	    private static bool ProcessKeyMultiLink(object obj, PropertyInfoEntry entry, ICSPropTypeProvider prv)
	    {
            return ProcessGenericLink(obj, entry, "ICSKeyMultiLink`4", prv.GetKeyMultiLinkType());//typeof(CSKeyMultiLink<,,,>));
	    }

	    private static bool ProcessMultiLink(object obj, PropertyInfoEntry entry, ICSPropTypeProvider prv)
	    {
            return ProcessGenericLink(obj, entry, "ICSMultiLink`2", prv.GetMultiLinkType());//typeof(CSMultiLink<,>));
        }

        private static bool ProcessSingleLink(object obj, PropertyInfoEntry entry, ICSPropTypeProvider prv)
        {
            return ProcessGenericLink(obj, entry, "ICSSingleLink`2", prv.GetSingleLinkType());//typeof(CSSingleLink<,>));
        }

        private static Type GetInterface(Type type, string ifName)
        {
            Type ifType = type.Name == ifName ? type : type.GetInterface(ifName);
            return ifType;
        }

	    ICollection<PropertyInfoEntry> GetPropertyInfoList(object obj, CSPropType pt)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            TypeInfoEntry entry = getTypeEntry(obj.GetType(), pt);

            ICollection<PropertyInfoEntry> propInfoList = entry.getPropertyInfos();
            return propInfoList;
        }

	    public ICSValue getAttribute(string attName, object obj)
		{
			return getCSProperty<ICSValue>(obj, attName, CSPropType.Attribute);
		}

		public ICSLink getLink(string refName, object obj)
		{
			return getCSProperty<ICSLink>(obj, refName, CSPropType.Reference);
		}

		public ICollection<ICSValue> getAttributeList(object obj)
		{
			return getCSProperties<ICSValue>(obj, CSPropType.Attribute);
		}

		public ICollection<ICSLink> getLinkList(object obj)
		{
			return getCSProperties<ICSLink>(obj, CSPropType.Reference);
		}

		private ICollection<T> getCSProperties<T>(object obj, CSPropType pt)
		{
            ICollection<PropertyInfoEntry> propInfoList = GetPropertyInfoList(obj, pt);
			List<T> res = new List<T>(propInfoList.Count);
			foreach(PropertyInfoEntry piEntry in propInfoList)
				res.Add(createCSProperty<T>(piEntry, obj, pt));
			return res;
		}

	    static T createCSProperty<T>(PropertyInfoEntry entry, object obj, CSPropType pt)
		{
            object retValue;

			if(typeof(ICSPropertyInfo).IsAssignableFrom(entry.MemberType))
			{
                retValue = entry.GetMemberValue(obj);
                if (retValue == null)
					throw new InvalidOperationException("cs property not set to instance of object");
			}
			else
			{
				if(pt == CSPropType.Attribute)
				{
					if(entry.MemberInfo is FieldInfo)
						retValue = new CSAttValueFieldProxy(obj, entry);
					else
                        retValue = new CSAttValuePropertyProxy(obj, entry);
				}
				else
				{
					if (entry.MemberInfo is FieldInfo)
					{
						if (typeof(IEnumerable).IsAssignableFrom(entry.MemberType))
                            retValue = new CSMultiLinkFieldProxy(obj, entry);
						else
                            retValue = new CSSingleLinkFieldProxy(obj, entry); 
					}
					else
					{
						if (typeof(IEnumerable).IsAssignableFrom(entry.MemberType))
                            retValue = new CSMultiLinkPropertyProxy(obj, entry);
						else
                            retValue = new CSSingleLinkPropertyProxy(obj, entry);
					}
				}
			}
            return (T)retValue;
		}


		private T getCSProperty<T>(object obj, string propName, CSPropType pt)
		{
			if(obj == null)
				throw new ArgumentNullException("obj");
			TypeInfoEntry entry = getTypeEntry(obj.GetType(), pt);
			PropertyInfoEntry pi = entry.getPropertyInfo(propName);
			if(pi == null)
				throw new NotFoundException("cs property not found: " + propName);

			return createCSProperty<T>(pi, obj, pt);
		}

		private TypeInfoEntry getTypeEntry(Type t, CSPropType pt)
		{
			Type attType;
			IDictionary<Type, TypeInfoEntry> cache;

			if(pt == CSPropType.Reference)
			{
				attType = typeof(CSReferenceAttribute);
				cache = _refCache;
			}
			else
			{
				attType = typeof(CSAttributeAttribute);
				cache = _attCache;
			}
			TypeInfoEntry entry;
            if (!cache.TryGetValue(t, out entry))
			{
                lock (cache)
                {
                    if (!cache.TryGetValue(t, out entry))
                    {
                        entry = new TypeInfoEntry(t, attType);
                        cache[t] = entry;
                    }
                }
			}
			return entry;
		}

		public bool containsProperty(string propName, CSPropType propType, Type objType)
		{
			if (objType == null)
				return false;
			TypeInfoEntry entry = getTypeEntry(objType, propType);
			PropertyInfoEntry info = entry.getPropertyInfo(propName);
			return info != null;
		}

        public IEnumerable<ICSPropertyInfo> GetValuesInfo(Type t)
        {
            TypeInfoEntry typeEntry = getTypeEntry(t, CSPropType.Attribute);
            return from pi in typeEntry.getPropertyInfos() select pi.PropertyInfo;
        }

        public IEnumerable<ICSLinkInfo> GetLinksInfo(Type t)
        {
            TypeInfoEntry typeEntry = getTypeEntry(t, CSPropType.Reference);
            return from pi in typeEntry.getPropertyInfos() select (ICSLinkInfo)pi.PropertyInfo;
        }
	}



}