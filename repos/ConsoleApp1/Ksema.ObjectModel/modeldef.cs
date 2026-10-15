using System.Collections;
using System.Collections.Generic;
using Framework.Collections;
using System.ComponentModel;


namespace Ksema.ObjectModel
{
    public interface ISimpleObject
	{
		object Key{get;}
	}

    public interface ICSObject : ISimpleObject, INotifyLinkChanged, INotifyValueChanged, ITrackChanges
	{
	}

	public interface ICSPropertyInfo
	{
		string Name{get;}
		bool Required { get;}
	}

	public interface ICSValue : ICSPropertyInfo
	{
		ITypeDef TypeDef{get;}
		void setValue(object val);
		object getValue();
	}

    public interface ICSValue<TVal>
    {
        TVal Value { get;set;}
    }


	public interface ICSLinkInfo : ICSPropertyInfo
	{
		bool IsComposite{get;}
		string BackRefName { get;}
	}

	public interface ICSLink : ICSLinkInfo
	{
		object Owner{get;}
        ICSLink getBackLink(object linkedObj);
	    ICollection getLinkedObjects();
	}

    public interface ICSLink<TOwner, TLinked> : ICSLinkInfo
        where TOwner: class 
        where TLinked: class
    {
        TOwner Owner { get;}
        void processBackRefAdd(ICSLink<TLinked, TOwner> origLink);
        void processBackRefRemove(ICSLink<TLinked, TOwner> origLink);
        ICSLink<TLinked, TOwner> getBackLink(TLinked linkedObj);

    }

	public interface ICSSingleLink : ICSLink
	{
		bool setObject(object obj);
		object getObject();
	}

    public interface ICSSingleLink<TOwner, TLinked> : ICSLink<TOwner, TLinked>
        where TOwner : class
        where TLinked : class
    {
        TLinked LinkedObject { get;set;}
    }

    public interface ICSMultiLink : ICSLink
	{
		void addLinkedObject(object obj);
		void removeLinkedObject(object obj);
		void removeAll();
	}

    public interface ICSMultiLink<TOwner, TLinked> : ICSLink<TOwner, TLinked>, IList<TLinked>, IBindingList
        where TOwner : class
        where TLinked : class
    {
        void addLinkedObject(TLinked obj);
        void removeLinkedObject(TLinked obj);
    }

    public interface ICSKeyMultiLink<TOwner, TLinked, TKey, TIndexer> : ICSLink<TOwner, TLinked>, IDictionary<TKey, TLinked>
        where TOwner : class
        where TLinked : class
        where TIndexer : IIndexer<TLinked, TKey>, new()
	{
        void addLinkedObject(TLinked obj);
        void removeLinkedObject(TLinked obj);
        void removeObjectByKey(TKey key);
    }

}
