using System;
using System.Linq;
using System.Data.SqlClient;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Text;
using Ksema.Framework.Diagnostics;
namespace Framework.Utils
{

	/*exceptions*/
	[Serializable] public class NotFoundException : ApplicationException
	{
		public NotFoundException(string descr) : base(descr) {}
		public NotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) {}
	}
	[Serializable] public class SyntaxErrorException : ApplicationException
	{
		public SyntaxErrorException(string errText) : base("Syntax error in: " + errText){}
		public SyntaxErrorException(SerializationInfo info, StreamingContext context) : base(info, context) {}
	}

	[Serializable]
	public class CompositeException : ApplicationException
    {
		private int _maxLength = 32000;
		private string _stackTrace = null;
		private List<Exception> _innerExceptions = new List<Exception>();

        public CompositeException()
        {
        }

        public CompositeException(Exception inner)
        {
            Add(inner);
        }

        public CompositeException(IEnumerable<Exception> list)
        {
			if (list != null)
				list.ForEach(item => Add(item));
        }

		public CompositeException(params Exception[] exceptions)
		{
			exceptions.ForEach(item => Add(item));
		}

		public CompositeException(SerializationInfo si, StreamingContext sc) : base(si, sc)
        {
			_innerExceptions = (List<Exception>)si.GetValue("mess", typeof(List<Exception>));
        }

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("mess", _innerExceptions, typeof(List<Exception>));
		}

        public IList<Exception> InnerExceptions
        {
            get { return _innerExceptions.AsReadOnly(); }
        }

		public void Add(Exception error)
		{
			if (error == null)
				return;
			if (error is CompositeException)
			{
				var composite = error as CompositeException;
				if (String.IsNullOrEmpty(_stackTrace) && !String.IsNullOrEmpty(composite._stackTrace))
					_stackTrace = composite._stackTrace;
				foreach (var subError in composite._innerExceptions)
					Add(subError);
			}
			else
				_innerExceptions.Add(error);
		}

		public int MaxLength
		{
			get
			{
				return _maxLength;
			}
			set
			{
				_maxLength = value;
			}
		}

		public override string Message
		{
			get
			{
				StringBuilder sb = new StringBuilder();
				foreach (Exception error in _innerExceptions)
				{
					var innerMessage = error.Message;
					if (!String.IsNullOrEmpty(innerMessage))
						sb.AppendLine(innerMessage);
				}
				return sb.ToString(0, _maxLength > 0 && sb.Length > _maxLength ? _maxLength : sb.Length);
			}
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			foreach (Exception error in _innerExceptions)
			{
				if (!(error is CompositeException))
					sb.AppendLine("===================== Exception ======================");
				var innerMessage = error.ToString();
				if (!String.IsNullOrEmpty(innerMessage))
					sb.AppendLine(innerMessage);
			}
			sb.AppendLine("------------ Exception rethrown at ------------");
			sb.AppendLine(StackTrace);
			return sb.ToString(0, _maxLength > 0 && sb.Length > _maxLength ? _maxLength : sb.Length);
		}

		public override string StackTrace
		{
			get
			{
				return _stackTrace ?? base.StackTrace;
			}
		}

		public CompositeException SetStackTrace(string newStackTrace)
		{
			_stackTrace = newStackTrace;
			return this;
		}

    	public bool IsEmpty
    	{
    		get
    		{
				return _innerExceptions.Count == 0;
    		}
    	}
    }

	public static class KnownTypesHelper
	{
		private static Type[] _types;
		public static Type[] GetKnownTypes()
		{
			if (_types == null)
			{
				var result = new List<Type>();
				foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
				{
					try
					{
						result.AddRange(assembly.GetTypes().Where(t =>
							(t.IsSubclassOf(typeof(Exception)) || t.Equals(typeof(Exception)))
							&& (t.IsSerializable || Attribute.IsDefined(t, typeof(DataContractAttribute)))
							&& t.IsPublic && !t.IsGenericType && !t.IsAbstract && !t.IsNested && t.IsVisible
						));
					}
					catch (Exception error)
					{
						Tracer.Error(error);
					}
				}
				result.Add(Type.GetType("System.Collections.ListDictionaryInternal"));
				result.Add(typeof(SqlErrorCollection));
				result.Add(typeof(SqlError));

				_types = result.Distinct().ToArray();
			}
			return _types;
		}
	}

	[Serializable]
	public class DeadlockException : Exception
	{
		public DeadlockException(string message, Exception realException) : base("¬о врем€ выполнени€ операции возникла взаимоблокировка. ¬еро€тно, кто-то еще (или сервер задач) одновременно выполн€ет эту же операцию. ѕопробуйте повторить операцию через несколько секунд." + Environment.NewLine + message, realException)
		{
		}

		public DeadlockException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
		}

		public override string Message
		{
			get
			{
				return base.Message;
			}
		}

		public override string ToString()
		{
			return this.Message + Environment.NewLine + this.InnerException.ToString();
		}

		public override string StackTrace
		{
			get
			{
				return this.InnerException.StackTrace;
			}
		}
	}
}