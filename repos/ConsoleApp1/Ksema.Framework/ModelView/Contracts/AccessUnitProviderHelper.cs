using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ksema.Framework.ModelView
{
    public static class AccessUnitProviderHelper
    {
        public static AccessUnitMode And(this AccessUnitMode x, AccessUnitMode y)
        {
            switch (x)
            {
                case AccessUnitMode.Edit:
                    // Любой доступ разрешен
                    return y;
                case AccessUnitMode.ReadOnly:
                    // Разрешено так же Hidden
                    switch (y)
                    {
                        case AccessUnitMode.Edit:
                            return x;
                        case AccessUnitMode.ReadOnly:
                        case AccessUnitMode.Hidden:
                            return y;
                    }
                    break;
                case AccessUnitMode.Hidden:
                    // Hidden это Hidden
                    return x;
            }
            throw new Exception("Unknown FieldMode values");
        }

        public static readonly IAccessUnit AllUnits = null;//new AllAccessUnitsDummy();
    }

    //internal sealed class AllAccessUnitsDummy : IAccessUnit
    //{
    //    #region IAccessUnit Members

    //    public AccessUnitMode Mode
    //    {
    //        get
    //        {
    //            throw new NotImplementedException();
    //        }
    //        set
    //        {
    //            throw new NotImplementedException();
    //        }
    //    }

    //    #endregion
    //}
}
