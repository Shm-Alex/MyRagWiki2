using System;

namespace Ksema.Framework.Diagnostics
{
    public class TraceOpScope : IDisposable
    {
        readonly string _opFormat;
        readonly object[] _args;
        readonly ITraceSource _ts;

        public TraceOpScope(string operationFormat, params object[] args)
        {
            _opFormat = operationFormat;
            _args = args;
            _ts = Tracer.GetTraceSource();
            _ts.StartOp(_opFormat, _args);
        }

        public void Dispose()
        {
            _ts.StopOp(_opFormat, _args);
        }

    }
}
