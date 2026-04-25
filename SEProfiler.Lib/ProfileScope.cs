using System;
using System.Diagnostics;

namespace SEProfiler
{
    /// <summary>
    /// Measures scope duration and GC activity, then reports on dispose.
    /// </summary>
    public struct ProfileScope : IDisposable
    {
        private readonly string _name;
        private readonly IProfilerSink _sink;
        private readonly long _startTimestamp;
        private readonly int _gc0Before;

        /// <summary>
        /// Starts a profiling scope for a named operation.
        /// </summary>
        /// <param name="name">Scope name to report.</param>
        /// <param name="sink">Sink that receives scope metrics.</param>
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

        /// <summary>
        /// Ends the scope and reports elapsed time and Gen0 GC delta.
        /// </summary>
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
