using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ksema.Framework.ModelView
{
    public interface IDataWrapperProvider
    {
        string DefaultUri { get; }
        IDataWrapper GetDataWrapper(string property);
    }

    public interface IValidationWrapperProvider
    {
        string DefaultUri { get; }
        IValidationWrapper GetValidationWrapper(string id);
    }
}
