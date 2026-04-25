using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SEProfiler
{
    /// <summary>
    /// Receives profiling metrics emitted by instrumented code.
    /// </summary>
    public interface IProfilerSink
    {
        /// <summary>
        /// Records a completed timed scope sample.
        /// </summary>
        /// <example>
        /// Typical usage through <c>Profiler.Scope</c>:
        /// <code>
        /// using (Profiler.Scope("MyMod.Update"))
        /// {
        ///     // Work to measure.
        /// }
        /// </code>
        /// </example>
        /// <param name="name">Scope name.</param>
        /// <param name="elapsedMs">Elapsed time in milliseconds.</param>
        /// <param name="gc0Delta">Gen0 collection delta during the scope.</param>
        void RecordScope(string name, double elapsedMs, int gc0Delta);

        /// <summary>
        /// Records a counter increment.
        /// </summary>
        /// <param name="name">Counter name.</param>
        /// <param name="delta">Increment amount.</param>
        void RecordCounter(string name, long delta);

        /// <summary>
        /// Records a gauge value update.
        /// </summary>
        /// <param name="name">Gauge name.</param>
        /// <param name="value">Current gauge value.</param>
        void RecordGauge(string name, double value);

        /// <summary>
        /// Records an event with optional payload data.
        /// </summary>
        /// <param name="name">Event name.</param>
        /// <param name="data">Optional event payload.</param>
        void RecordEvent(string name, string data);
    }

     /// <summary>
    /// Global entry point for emitting profiling metrics.
    /// </summary>
    public static class Profiler
    {
        private static volatile IProfilerSink _sink;

        // modId → modName. Populated at runtime by mods calling Register().
        public static readonly Dictionary<string, string> RegisteredMods = new Dictionary<string, string>();

        private static readonly object _registryLock = new object();

        /// <summary>
        /// Gets or sets the active profiler sink.
        /// </summary>
        public static IProfilerSink Sink
        {
            get { return _sink; }
            set { _sink = value; }
        }

        /// <summary>
        /// Gets whether profiling is currently enabled.
        /// </summary>
        public static bool IsActive { get { return _sink != null; } }

        /// <summary>
        /// Registers an instrumented mod for discovery in the plugin UI.
        /// Mod authors call this during Init to declare their mod as instrumented.
        /// modId is the Steam Workshop ID string; modName is the display name shown in the
        /// plugin config dialog. If modId is empty the call is ignored.
        /// </summary>
        /// <param name="modName">Display name of the mod.</param>
        /// <param name="modId">Workshop ID of the mod.</param>
        public static void Register(string modName, string modId)
        {
            if (string.IsNullOrEmpty(modId)) return;
            lock (_registryLock)
                RegisteredMods[modId] = modName ?? string.Empty;
        }

        /// <summary>
        /// // Returns a point-in-time snapshot of registered mods (Key=modId, Value=modName).
        /// </summary>
        /// <returns>Array of mod ID/name pairs.</returns>
        public static KeyValuePair<string, string>[] GetRegisteredModsSnapshot()
        {
            lock (_registryLock)
            {
                var result = new KeyValuePair<string, string>[RegisteredMods.Count];
                int i = 0;
                foreach (var kv in RegisteredMods)
                    result[i++] = kv;
                return result;
            }
        }

        /// <summary>
        /// Creates a disposable profiling scope for a named operation.
        /// </summary>
        /// <param name="name">Scope name.</param>
        /// <returns>A scope that reports metrics when disposed.</returns>
        public static ProfileScope Scope(string name)
        {
            return new ProfileScope(name, _sink);
        }

        /// <summary>
        /// Records a counter increment if profiling is active.
        /// </summary>
        /// <param name="name">Counter name.</param>
        /// <param name="delta">Increment amount.</param>
        public static void Counter(string name, long delta = 1)
        {
            var sink = _sink;
            if (sink == null) return;
            sink.RecordCounter(name, delta);
        }

        /// <summary>
        /// Records a gauge value if profiling is active.
        /// </summary>
        /// <param name="name">Gauge name.</param>
        /// <param name="value">Gauge value.</param>
        public static void Gauge(string name, double value)
        {
            var sink = _sink;
            if (sink == null) return;
            sink.RecordGauge(name, value);
        }

        /// <summary>
        /// Records an event if profiling is active.
        /// </summary>
        /// <param name="name">Event name.</param>
        /// <param name="data">Optional event payload.</param>
        public static void Event(string name, string data = null)
        {
            var sink = _sink;
            if (sink == null) return;
            sink.RecordEvent(name, data);
        }
    }
     /// <summary>
    /// Marks a method for automatic scope instrumentation.
    /// Tooling can transform the method body to wrap execution in Profiler.Scope(Name).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class ScopeAttribute : Attribute
    {
        public ScopeAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }
    }

    /// <summary>
    /// Marks a method for automatic counter instrumentation.
    /// Tooling can inject Profiler.Counter(Name, Delta) at method entry.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class CounterAttribute : Attribute
    {
        public CounterAttribute(string name)
            : this(name, 1)
        {
        }

        public CounterAttribute(string name, long delta)
        {
            Name = name;
            Delta = delta;
        }

        public string Name { get; private set; }

        public long Delta { get; private set; }
    }

    /// <summary>
    /// Marks a method for automatic gauge instrumentation using a compile-time constant value.
    /// Dynamic runtime values should use explicit Profiler.Gauge calls inside the method body.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class GaugeAttribute : Attribute
    {
        public GaugeAttribute(string name, double value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; private set; }

        public double Value { get; private set; }
    }

    /// <summary>
    /// Marks a method for automatic event instrumentation.
    /// Tooling can inject Profiler.Event(Name, Data) at method entry.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class EventAttribute : Attribute
    {
        public EventAttribute(string name)
            : this(name, null)
        {
        }

        public EventAttribute(string name, string data)
        {
            Name = name;
            Data = data;
        }

        public string Name { get; private set; }

        public string Data { get; private set; }
    }
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
