using System;
using System.Collections.Generic;
using Framework.Utils;
using System.Linq;
using System.Text;

namespace Ksema.Framework.Macro.ContextCache
{

	public interface IContextInfoProvider
	{
		void AddRequirementToCollection(IKnowledgeSeeker objWhichWantsInfo);
		object GetNeededData(IKnowledgeSeeker objWhichWantsInfo);
		void InitializeRoot(IContextInfoProvider provider);
		void Reset();
	}

	public interface IKnowledgeSeeker
	{
		string GetInfoProviderType();
		void LinkToProvider(IContextInfoProvider provider);
	}

	public class ContextCacheManager : IContextInfoProvider
	{
		private Dictionary<string, IContextInfoProvider> _providers = new Dictionary<string, IContextInfoProvider>();
		private static string ThisTypeName = typeof (ContextCacheManager).AssemblyQualifiedName;

		public void AddRequirementToCollection(IKnowledgeSeeker objWhichWantsInfo)
		{
			string neededProvider = objWhichWantsInfo.GetInfoProviderType();
			if (neededProvider == ThisTypeName)
				return;
			if (!_providers.ContainsKey(neededProvider))
			{
				IContextInfoProvider provider = TypeUtils.CreateInstance<IContextInfoProvider>(neededProvider);
				provider.InitializeRoot(this);
				_providers[neededProvider] = provider;
			}
			_providers[neededProvider].AddRequirementToCollection(objWhichWantsInfo);
		}

		public object GetNeededData(IKnowledgeSeeker objWhichWantsInfo)
		{
			string neededProvider = objWhichWantsInfo.GetInfoProviderType();
			if (neededProvider == ThisTypeName)
				return this;
			if (_providers.ContainsKey(neededProvider))
			{
				return _providers[neededProvider].GetNeededData(objWhichWantsInfo);
			}
			return null;
		}

		public void InitializeRoot(IContextInfoProvider provider)
		{
			return;
		}

		public void Reset()
		{
			foreach (var provider in _providers.Values)
			{
				provider.Reset();
			}
			_providers.Clear();
		}
	}
}
