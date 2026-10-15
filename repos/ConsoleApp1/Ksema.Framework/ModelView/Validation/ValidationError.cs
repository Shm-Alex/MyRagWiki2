using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ksema.Framework.ModelView
{
    class ValidationErrorImpl : IValidationError
    {
        public ValidationErrorImpl(object uri, object errorContent)
        {
            Uri = uri;
            ErrorContent = errorContent;
        }

        public object ErrorContent
        {
            get;
            private set;
        }

        public object Uri
        {
            get;
            private set;
        }
    }
}
