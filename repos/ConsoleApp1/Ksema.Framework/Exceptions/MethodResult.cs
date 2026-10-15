using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Framework.Utils;
using System.Runtime.Serialization;
using Ksema.Framework.Diagnostics;
using Framework.Compress;
using Framework.Exceptions;

namespace Framework
{
    public enum Severity { Message, Warning, Error };

	[Serializable]
	[DataContract]
    public class MethodResult
    {
        [DataMember]
        protected List<MethodResult> loLevelResults = new List<MethodResult>();

        [DataMember]
        protected string m_detailedMessage, m_message;

        [IgnoreDataMember]
        protected Exception m_error;

        [DataMember]
        protected string ErrorString
        {
            get
            {
                if (m_error != null)
                {
                    try
                    {
                        return Serializer.serializeToString(m_error);
                    }
                    catch (Exception error)
                    {
                        // Ничего нельзя поделать - исключение будет передаваться текстом 
                        // и воспроизводиться как MethodResultException. Но если какой 
                        // добрый пан поймал это место, то очень хочется знать, из-за чего
                        if (System.Diagnostics.Debugger.IsAttached)
                            System.Diagnostics.Debugger.Break();
                        Tracer.Error(error);
                        return String.Empty;
                    }
                }
                else
                    return String.Empty;
            }
            set
            {
                if (String.IsNullOrEmpty(value))
                    m_error = null;
                else
                {
                    try
                    {
                        m_error = (Exception)Serializer.deserializeFromString(value);
                    }
                    catch (Exception error)
                    {
                        if (System.Diagnostics.Debugger.IsAttached)
                            System.Diagnostics.Debugger.Break();
                        Tracer.Error(error);
                        m_error = null;
                    }
                }
            }
        }

        [DataMember]
        protected Severity m_severity = Severity.Message;

        [DataMember]
        public bool ShouldBeStored = true;

        public void CopyMainFields(MethodResult mr)
        {
            mr.m_detailedMessage = m_detailedMessage;
            mr.m_error = m_error;
            mr.m_message = m_message;
            mr.m_severity = m_severity;
            mr.ShouldBeStored = ShouldBeStored;
        }

        public IEnumerable<MethodResult> LowLevelResults
        {
            get { return loLevelResults; }
        }

        public void Add(MethodResult mr)
        {
            if (mr != null)
                loLevelResults.Add(mr);
        }

        public void Add(Exception ex, Severity sev)
        {
            MethodResult n_mr = new MethodResult();
            n_mr.Severity = sev;
            n_mr.Error = ex;
            Add(n_mr);
        }

        public void Add(Exception ex)
        {
            Add(ex, Severity.Error);
        }

        public bool IsEmpty
        {
            get
            {
                bool empty = String.IsNullOrEmpty(m_detailedMessage) && String.IsNullOrEmpty(m_message);
                if (empty)
                {
                    foreach (var inner in loLevelResults)
                        if (!inner.IsEmpty)
                            return false;
                }
                return empty;
            }
        }

        public Severity Severity
        {
            get
            {
                bool hasUnderlyingWarning = false;
                foreach (MethodResult mr in loLevelResults)
                {
                    if (mr.m_severity == Severity.Error)
                        return Severity.Error;
                    if (mr.m_severity == Severity.Warning)
                        hasUnderlyingWarning = true;
                }
                if (hasUnderlyingWarning && m_severity == Severity.Message)
                    return Severity.Warning;

                return m_severity;
            }
            set
            {
                m_severity = value;
            }
        }
        public string Message
        {
            get
            {
                StringBuilder sb = new StringBuilder(m_message);
                foreach (MethodResult mr in loLevelResults)
                {
                    var innerMessage = mr.Message;
                    if (!String.IsNullOrEmpty(innerMessage))
                        sb.AppendLine(" " + innerMessage);
                }
                return sb.ToString();
            }
            set
            {
                m_message = value;
                m_error = null;
            }
        }
        public string DetailedMessage
        {
            get
            {
                StringBuilder sb = new StringBuilder(m_detailedMessage);
                foreach (MethodResult mr in loLevelResults)
                {
                    var innerMessage = mr.m_detailedMessage;
                    if (!String.IsNullOrEmpty(innerMessage))
                        sb.AppendLine(" " + innerMessage);
                }
                return sb.ToString();
            }
            set
            {
                m_detailedMessage = value;
                m_error = null;
            }
        }
        public Exception Error
        {
            get
            {
                return m_error;
            }
            set
            {
                m_error = value;
                m_message = m_error.Message;
                m_detailedMessage = m_error.ToString();
            }
        }

        public CompositeException ComposeException()
        {
            CompositeException result = new CompositeException();
            if (m_error != null)
                result.Add(m_error);
            else if (!String.IsNullOrEmpty(m_detailedMessage) || !String.IsNullOrEmpty(m_message))
                result.Add(new MethodResultException(m_message, m_detailedMessage));

            foreach (var subMethodResult in loLevelResults)
                foreach (var innerError in subMethodResult.ComposeException().InnerExceptions)
                    result.Add(innerError);

            return result;
        }
    }
}
