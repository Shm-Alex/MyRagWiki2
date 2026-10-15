using System;

namespace Ksema.ObjectModel
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class CSAttributeAttribute : Attribute, ICSPropertyInfo
    {
    	public CSAttributeAttribute(string name)
        {
            Name = name;
        }

    	public string Name { get; private set; }
    	public bool Required { get; set; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class CSReferenceAttribute : Attribute, ICSLinkInfo
    {
    	public CSReferenceAttribute(string name)
    	{
    		IsComposite = false;
    		Name = name;
    	}

    	public CSReferenceAttribute(string name, bool isComposite)
        {
            Name = name;
            IsComposite = isComposite;
        }
        public CSReferenceAttribute(string name, bool isComposite, string backRefName)
        {
            Name = name;
            IsComposite = isComposite;
            BackRefName = backRefName;
        }

    	public string Name { get; private set; }
    	public bool IsComposite { get; private set; }
    	public string BackRefName { get; set; }
    	public bool Required { get; set; }
    }

}
