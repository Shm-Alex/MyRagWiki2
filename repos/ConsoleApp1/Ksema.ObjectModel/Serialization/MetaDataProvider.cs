using System;
using System.Collections.Generic;
using System.Text;
using Serializer;

namespace Ksema.ObjectModel.Serialization
{
    public interface IMetaDataProvider
    {
        MetaData GetMetaData();
		CSSerializer GetSerializer();
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class CSMetaDataAttribute : Attribute
    {
        Type _mdPrvType;


        public CSMetaDataAttribute(Type aMetaDataProviderType)
        {
            _mdPrvType = aMetaDataProviderType;
        }

        public IMetaDataProvider CreateMetaDataProvider()
        {
            return (IMetaDataProvider) Activator.CreateInstance(_mdPrvType);
        }
    }
}
