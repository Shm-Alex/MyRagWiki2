using System.Runtime.Serialization;

namespace Ksema.ObjectModel
{
	public interface IObjectViewProxy
	{
		void UpdateViewData();

		string Label { get; }
		string Icon { get; }
		string NavigationString { get; }
	}

	public interface IObjectViewProxyProvider
	{
		IObjectViewProxy CreateViewProxy();
	}

	[DataContract]
	public class ObjectViewProxyData : IObjectViewProxy
	{
		public void UpdateViewData()
		{
			//do nothing here
		}

		[DataMember]
		public string Label { get; set; }

		[DataMember]
		public string Icon { get; set; }

		[DataMember]
		public string NavigationString { get; set; }
	}

	public interface IObjectProxy
	{
		object GetObject();
	}

	public interface INamedServiceProvider
	{
		T GetService<T>(string name);
	}

	public interface IPropertyValue
	{
		object Evaluate(object target);
	}

}
