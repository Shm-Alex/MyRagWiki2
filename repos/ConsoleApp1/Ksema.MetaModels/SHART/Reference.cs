using System;
using Ksema.ObjectModel;
using Microsoft.Practices.EnterpriseLibrary.Validation.Validators;
using Framework.Macro;
using System.Collections.Generic;

namespace Shart.ModelDef
{
	[Serializable]
	public class ReferenceMetaData : FieldMetaData, IReferenceMetaData//, ICloneable
	{
		[CSAttribute( "IsIdentifying" )]
		private bool _isIdentifying = false;

		[CSReference( "Target", false )]
		private CSSingleLink<ReferenceMetaData, DimensionMetaData> _target = null;

        [CSAttribute("BackRefTemplate")]
        private string _backRefTemplate = string.Empty;

        string _refPropName, _backRefPropName;

	    public ReferenceMetaData()
		{
			CSPropMgr.Instance.setupObject( this, false );
		}

        IMacroManager MacroMgr
        {
            get { return Owner.Model.MacroMgr; }
        }

        protected override void OnNameTemplateChanged()
        {
            base.OnNameTemplateChanged();
            _refPropName = null;
        }

	    public string RefPropertyName
	    {
	        get
	        {
                if (string.IsNullOrEmpty(_refPropName))
                {
                    string macro = this.NameTemplate;
                    if (string.IsNullOrEmpty(macro))
                        macro = Owner.Model.DefaultRefName;
                    _refPropName = (string)MacroMgr.ParseString(macro, "ctx", this);
                }
                return _refPropName;
	        }
	    }

	    public bool HasBackRef
        {
            get { return !string.IsNullOrEmpty(this.BackRefTemplate); }
        }

	    public string BackRefPropertyName
	    {
	        get
	        {
	            if (!HasBackRef)
	                return string.Empty;

                if (string.IsNullOrEmpty(_backRefPropName))
                {
                    string macro = this.BackRefTemplate;
                    if (string.IsNullOrEmpty(macro))
                        macro = Owner.Model.DefaultBackRefName;
                    _backRefPropName = (string)MacroMgr.ParseString(macro, "ctx", this);
                }
                return _backRefPropName;
	        }
	    }

	    public string TargetName
	    {
            get { return Target.Name; }
	    }

	    [MetaDataPropertyAttribute]
	    public string BackRefTemplate
	    {
	        get { return _backRefTemplate; }
            set 
            { 
                _backRefTemplate = value;
                _backRefPropName = null;
            }
	    }

	    public bool IsIdentifying
		{
			get { return _isIdentifying; }
			set { _isIdentifying = value; }
		}

        [NotNullValidator]
		public DimensionMetaData Target
		{
			get { return _target.LinkedObject; }
			set { _target.LinkedObject = value; }
		}

	    IDimensionMetaData IReferenceMetaData.Target
        {
            get { return _target.LinkedObject; }
        }

        public override int DataSize
        {
            get
            {
                return 4;
            }
            set
            {
                
            }
        }

        public override DataType DataType
        {
            get
            {
                return DataType.Integer;
            }
            set
            {
               
            }
        }

        public override string ToString()
        {
            if( Owner != null )
                return Owner.Name + '.' + Name;
            else
                return Name;
        }

		internal ReferenceMetaData Clone()
		{
			var result = (ReferenceMetaData)MemberwiseClone();
			result.GUID = Guid.NewGuid().ToString();
			CSPropMgr.Instance.setupObject( result, true );
			result.Target = Target;

			// Свойства
			foreach (var property in Properties.Values)
			{
				result.AddProperty(property.Clone());
			}

			return result;
		}

		//object ICloneable.Clone()
		//{
		//    return Clone();
		//}
	}
}
