using System.Collections.Generic;

namespace SEProfiler
{
    public static class Profiler
    {
        private static volatile IProfilerSink _sink;

        // modId → modName. Populated at runtime by mods calling Register().
        public static readonly Dictionary<string, string> RegisteredMods =
            new Dictionary<string, string>();

        private static readonly object _registryLock = new object();

        public static IProfilerSink Sink
        {
            get { return _sink; }
            set { _sink = value; }
        }

        public static bool IsActive { get { return _sink != null; } }

        // Mod authors call this during Init to declare "my mod is instrumented".
        // modId is the Steam Workshop ID string; modName is the display name shown in the
        // plugin config dialog. If modId is empty the call is ignored.
        public static void Register(string modName, string modId)
        {
            if (string.IsNullOrEmpty(modId)) return;
            lock (_registryLock)
                RegisteredMods[modId] = modName ?? string.Empty;
        }

        // Returns a point-in-time snapshot of registered mods (Key=modId, Value=modName).
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

        public static ProfileScope Scope(string name)
        {
            return new ProfileScope(name, _sink);
        }

        public static void Counter(string name, long delta = 1)
        {
            var sink = _sink;
            if (sink == null) return;
            sink.RecordCounter(name, delta);
        }

        public static void Gauge(string name, double value)
        {
            var sink = _sink;
            if (sink == null) return;
            sink.RecordGauge(name, value);
        }

        public static void Event(string name, string data = null)
        {
            var sink = _sink;
            if (sink == null) return;
            sink.RecordEvent(name, data);
        }
    }
}
