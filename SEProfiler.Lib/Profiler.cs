using System.Collections.Generic;

namespace SEProfiler
{
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
}
