using Ksema.ObjectModel;
using Microsoft.Practices.EnterpriseLibrary.Validation.Validators;
using System;
using Ksema.Framework;

namespace Shart.ModelDef
{
	/// <summary>
	/// Скрипт обновления модели и данных который выполняется адресно для заданного объекта
	/// </summary>
	[Serializable]
	public class UpdateScriptMetaData: BaseMetaData, IObjectURIProvider
    {
		[CSReference( "Target", false )]
		private CSSingleLink<UpdateScriptMetaData, BaseMetaData> _target;

		[CSAttribute( "Text" )]
		private string _text = "";

		[CSAttribute( "Event" )]
		private ScriptEvent _event;

		[CSAttribute( "Timing" )]
		private ScriptTiming _timing;

		public UpdateScriptMetaData()
		{
			CSPropMgr.Instance.setupObject( this, false );
		}

		public BaseMetaData Target
		{
			get
			{
				return _target.LinkedObject;
			}
			set
			{
				_target.LinkedObject = value;
			}
		}

		public string Text
		{
			get
			{
				return _text;
			}
			set
			{
				_text = value;
			}
		}

		public ScriptEvent Event
		{
			get
			{
				return _event;
			}
			set
			{
				_event = value;
			}
		}

		public ScriptTiming Timing
		{
			get
			{
				return _timing;
			}
			set
			{
				_timing = value;
			}
		}

		#region IObjectURIProvider Members

		public ObjectURI CreateURI()
		{
			ObjectURI result = new ObjectURI( "script/" + GUID );
			return result;
		}

		#endregion
	}

	/// <summary>
	/// В каком случае запускается скрипт:
	/// - при создании объекта
	/// - при изменении
	/// - при удалении
	/// </summary>
	public enum ScriptEvent
	{
		Add,
		Update,
		Delete
	}

	/// <summary>
	/// Момент запуска скрипт: 
	/// до действия, вместо действия, после действия
	/// </summary>
	public enum ScriptTiming
	{
		Before, //Перед действие
		Instead, // Вместо действия
		After // После действия
	}
}
