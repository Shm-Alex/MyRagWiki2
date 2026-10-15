using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Framework.Utils;
using System.ServiceModel;
using System.Reflection;
using System.Data.SqlClient;
using System.Threading;

namespace Framework.Exceptions
{
	public static class ExceptionHelper
	{
		private const int DEADLOCK_MESSAGE_NUMBER = 1205;

		public static Exception Untwine(this Exception error)
		{
			if (error == null)
				return new Exception("Unknown error");
			else if (error is OutOfMemoryException)
				return error;
			Exception newError = null;
			try
			{
				var trace = new StringBuilder();
				newError = UntwineException(error, trace);
				var stackTrace = (error.StackTrace == null || error.StackTrace.IndexOf(trace.ToString()) == -1)
					? trace.Append(error.StackTrace).ToString()
					: error.StackTrace;
				if (!String.IsNullOrEmpty(stackTrace) && newError.ToString().IndexOf(stackTrace) == -1)
					newError = new CompositeException(newError).SetStackTrace(stackTrace);
				return newError;
			}
			catch
			{
				if (newError == null)
					return error;
				else
					return newError;
			}
		}

		public static Exception UntwineException(Exception error, StringBuilder trace)
		{
			try
			{
				Exception untwinedError = null;
				string deadlockText;

				if (error is FaultException<MethodResult> && (error as FaultException<MethodResult>).Detail != null)
				{
					untwinedError = (error as FaultException<MethodResult>).Detail.ComposeException();
					trace.Insert(0, error.StackTrace + Environment.NewLine);
				}
				else if (error is TargetInvocationException && error.InnerException != null)
				{
					untwinedError = error.InnerException;
					trace.Insert(0, untwinedError.StackTrace + Environment.NewLine);
				}
				else if (error is AggregateException)
				{
					untwinedError = new CompositeException((error as AggregateException).Flatten().InnerExceptions);
					trace.Insert(0, error.StackTrace + Environment.NewLine);
				}
				else if (error is CompositeException)
				{
					bool changed = false;
					List<Exception> newList = new List<Exception>();
					foreach (var innerError in (error as CompositeException).InnerExceptions)
					{
						var newError = UntwineException(innerError, trace);
						newList.Add(newError);
						if (newError != innerError)
							changed = true;
					}
					if (newList.Count == 1)
						untwinedError = newList[0];
					else if (changed)
						untwinedError = new CompositeException(newList);
				}
				else if (!(error is DeadlockException) && error.IsDeadlockException(out deadlockText))
				{
					untwinedError = new DeadlockException(deadlockText, error);
				}

				return untwinedError != null ? UntwineException(untwinedError, trace) : error;
			}
			catch
			{
				return error;
			}
		}

		public static bool IsDeadlockException(this Exception error, out string deadlockError)
		{
			while (!(error is SqlException) && !(error is DeadlockException) && error.InnerException != null)
				error = error.InnerException;
			deadlockError = null;
			bool deadlock = false;
			if (error is DeadlockException)
			{
				deadlockError = (error as DeadlockException).Message;
				deadlock = true;
			}
			else if (error is SqlException)
			{
				foreach (SqlError sqlError in (error as SqlException).Errors)
				{
					if (sqlError.Number == DEADLOCK_MESSAGE_NUMBER)
					{
						deadlockError = sqlError.Message;
						deadlock = true;
						break;
					}
				}
			}
			return deadlock;
		}

		public static bool IsThreadAbort(this Exception exception)
		{
			while (!(exception is ThreadAbortException) && exception.InnerException != null)
				exception = exception.InnerException;
			return exception is ThreadAbortException;
		}

		public static IEnumerable<Exception> FlattenInner(this Exception error)
		{
			return FlattenInner(error, false);
		}

		private static IEnumerable<Exception> FlattenInner(this Exception initialError, bool simple)
		{
			var error = simple ? initialError : initialError.Untwine();
			if (error is CompositeException)
			{
				foreach (var innerError in (error as CompositeException).InnerExceptions)
					foreach (var innerFlattenError in innerError.FlattenInner(innerError == initialError))
						yield return innerFlattenError;
			}
			while (error != null)
			{
				yield return error;
				error = error.InnerException;
			}
		}
	}
}
