using System;
using System.Collections;
using System.Collections.Generic;
using Framework.Collections;
using Framework.Utils;


namespace Ksema.ObjectModel
{
    [Serializable]
    public class CSAttValue<TVal> : ICSValue, ICSValue<TVal>, INotifyValueChanged
    {
        TVal _value;
        string _def;
        ITypeDef _typeDef;
        object _owner;
        private bool required;

        public CSAttValue(string def, object owner)
        {
            if (def == null || owner == null)
                throw new ArgumentNullException();

            _def = def;
            _owner = owner;
            _typeDef = new SystemTypeDef(typeof(TVal));

            if (_owner is ITrackChanges)
                ((ITrackChanges)_owner).SetupValueChanges(this);
        }

        void ICSValue.setValue(object new_val)
        {
            this.Value = (TVal)new_val;
        }

        object ICSValue.getValue()
        {
            return this.Value;
        }

        public string Name
        {
            get { return _def; }
        }
        public ITypeDef TypeDef
        {
            get
            {
                return _typeDef;
            }
        }
        public bool Required
        {
            get { return required; }
            set { required = value; }
        }

        public TVal Value
        {
            get
            {
                //                if (_value == null)
                //                    setDefaultValue();
                return _value;
            }

            set
            {
                if (Object.Equals(_value, value))
                    return;
                TVal old_val = _value;
                _value = value;
                OnValueSet(old_val, value);
            }
        }

        void setDefaultValue()
        {
            _value = (TVal)_typeDef.createDefaultValue();
        }


        public event ValueChangedEventHandler ValueChanged;

        protected void OnValueSet(object old_val, object new_val)
        {
            if (ValueChanged != null)
                ValueChanged(this, new ValueChangedEventArgs(_owner, _def, old_val, new_val, true));
        }

    }

}
