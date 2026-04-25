using System;
using System.Diagnostics;

namespace SEProfiler
{
    public struct ProfileScope : IDisposable
    {
        private readonly string _name;
        private readonly IProfilerSink _sink;
        private readonly long _startTimestamp;
        private readonly int _gc0Before;

        public ProfileScope(string name, IProfilerSink sink)
        {
            _name = name;
            _sink = sink;
            if (sink != null)
            {
                _startTimestamp = Stopwatch.GetTimestamp();
                _gc0Before = GC.CollectionCount(0);
            }
            else
            {
                _startTimestamp = 0;
                _gc0Before = 0;
            }
        }

        public void Dispose()
        {
            if (_sink == null)
                return;

            double ms = (Stopwatch.GetTimestamp() - _startTimestamp) * 1000.0 / Stopwatch.Frequency;
            int gc0Delta = GC.CollectionCount(0) - _gc0Before;
            _sink.RecordScope(_name, ms, gc0Delta);
        }
    }
}
