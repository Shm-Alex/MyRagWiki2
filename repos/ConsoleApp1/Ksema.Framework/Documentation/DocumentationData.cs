using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ksema.Framework.Documentation
{
	[Serializable]
	public abstract class DocumentationBase
	{
		public DocumentationBase()
		{
		}
		public DocumentationBase(DocumentationBase data)
		{
			ShortName = data.ShortName;
			Description = data.Description;
			UsageExample = data.UsageExample;
		}

		public string ShortName
		{
			get;
			set;
		}

		public string Description
		{
			get;
			set;
		}

		public string UsageExample
		{
			get;
			set;
		}

		public void SetDataFromAttribute(DocumentationAttribute data)
		{
			SetDataFromAttribute(data, new object[0]);
		}
		public void SetDataFromAttribute(DocumentationAttribute data, params object[] localizationData)
		{
			ShortName = data.UseLocalization ? data.ShortName.Localize(localizationData) : data.ShortName;
			Description = data.UseLocalization ? data.Description.Localize(localizationData) : data.Description;
			UsageExample = data.UseLocalization ? data.UsageExample.Localize(localizationData) : data.UsageExample;
		}
	}

	[Serializable]
	public class DocumentationBaseData : DocumentationBase
	{
		public DocumentationBaseData()
		{
		}
		public DocumentationBaseData(Type type) : this()
		{
			ObjectType = type;
			ObjectName = type.Name;
		}
		public DocumentationBaseData(DocumentationBaseData data) : base(data)
		{
			ObjectType = data.ObjectType;
			ObjectName = data.ObjectName;
		}

		public Type ObjectType
		{
			get;
			set;
		}

		public string ObjectName
		{
			get;
			set;
		}
	}

	[Serializable]
	public class DocumentationData : DocumentationBaseData
	{
		public static string StubName = ">>>STUB<<<";

		public DocumentationData() : base()
		{
			Init();
		}
		public DocumentationData(Type type) : base(type)
		{
			Init();
		}
		public DocumentationData(DocumentationBaseData data) : base(data)
		{
			Init();
		}

		private void Init()
		{
			Links = new List<DocumentationLinkData>();
			Categories = new List<DocumentationData>();
		}

		public IList<DocumentationLinkData> Links
		{
			get;
			private set;
		}

		public IList<DocumentationData> Categories
		{
			get;
			private set;
		}
	}

	[Serializable]
	public class DocumentationLinkBaseData : DocumentationBase
	{
		public DocumentationLinkBaseData()
		{
		}
		public DocumentationLinkBaseData(DocumentationLinkBaseData data) : base(data)
		{
			LinkName = data.LinkName;
		}

		public string LinkName
		{
			get;
			set;
		}
	}

	[Serializable]
	public class DocumentationLinkData : DocumentationLinkBaseData
	{
		public DocumentationLinkData() : base()
		{
		}
		public DocumentationLinkData(DocumentationLinkBaseData data) : base(data)
		{
		}

		public DocumentationData LinkedObject
		{
			get;
			set;
		}
	}
}
