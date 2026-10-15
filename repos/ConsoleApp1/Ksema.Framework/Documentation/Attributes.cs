using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ksema.Framework.Documentation
{
	[Serializable]
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public class DocumentationAttribute : Attribute
	{
		public DocumentationAttribute(string shortName)
		{
			UseLocalization = true;
			ShortName = shortName;
		}
		public DocumentationAttribute(string shortName, string description) : this(shortName)
		{
			Description = description;
		}
		public DocumentationAttribute(string shortName, string description, string usageExample) : this(shortName, description)
		{
			UsageExample = usageExample;
		}

		public string ShortName
		{
			get;
			private set;
		}
		public string Description
		{
			get;
			private set;
		}
		public string UsageExample
		{
			get;
			private set;
		}

		public bool UseLocalization
		{
			get;
			set;
		}
	}
}
