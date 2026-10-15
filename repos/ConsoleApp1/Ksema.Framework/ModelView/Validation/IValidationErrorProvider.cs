using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ksema.Framework.ModelView
{
    public interface IValidationError
    {
        object ErrorContent { get; }
        object Uri { get; }
    }

    public interface IValidationErrorProvider
    {
        IEnumerable<IValidationError> Errors { get; }
    }

}
