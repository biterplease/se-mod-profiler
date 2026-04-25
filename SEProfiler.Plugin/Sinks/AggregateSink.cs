using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using SEProfiler.Metrics;

namespace SEProfiler.Sinks
{
    public sealed class AggregateSink : IProfilerSink
    {
        private readonly Dictionary<string, Histogram> _scopes   = new Dictionary<string, Histogram>();
        private readonly Dictionary<string, Counter>   _counters = new Dictionary<string, Counter>();
        private readonly Dictionary<string, Gauge>     _gauges   = new Dictionary<string, Gauge>();
        private readonly object _scopeLock   = new object();
        private readonly object _counterLock = new object();
        private readonly object _gaugeLock   = new object();

        private readonly ConcurrentQueue<string> _eventQueue = new ConcurrentQueue<string>();
        private volatile bool _jsonlEnabled;

        public bool JsonlEnabled
        {
            get { return _jsonlEnabled; }
            set { _jsonlEnabled = value; }
        }

        // ── IProfilerSink (mod instrumentation) ─────────────────────────────

        public void RecordScope(string name, double elapsedMs, int gc0Delta)
        {
            RecordScopeInternal("mod", name, elapsedMs, gc0Delta);
        }

        public void RecordCounter(string name, long delta)
        {
            GetOrCreate(_counters, _counterLock, name).Increment(delta);
            if (_jsonlEnabled)
                _eventQueue.Enqueue(FormatCounterJson("mod", name, delta));
        }

        public void RecordGauge(string name, double value)
        {
            GetOrCreate(_gauges, _gaugeLock, name).Set(value);
            if (_jsonlEnabled)
                _eventQueue.Enqueue(FormatGaugeJson("mod", name, value));
        }

        public void RecordEvent(string name, string data)
        {
            if (_jsonlEnabled)
                _eventQueue.Enqueue(FormatEventJson("mod", name, data));
        }

        // ── Internal paths (Harmony patches, ETW listener) ──────────────────

        internal void RecordFrameworkScope(string name, double elapsedMs, int gc0Delta)
        {
            RecordScopeInternal("framework", name, elapsedMs, gc0Delta);
        }

        internal void RecordEtwEvent(string name, string data)
        {
            if (_jsonlEnabled)
                _eventQueue.Enqueue(FormatEventJson("etw", name, data));
        }

        internal void RecordEtwCounter(string name, long delta = 1)
        {
            GetOrCreate(_counters, _counterLock, name).Increment(delta);
            if (_jsonlEnabled)
                _eventQueue.Enqueue(FormatCounterJson("etw", name, delta));
        }

        // ── Snapshot API (for writers) ───────────────────────────────────────

        public KeyValuePair<string, Histogram>[] GetScopeSnapshot()
        {
            return Snapshot(_scopes, _scopeLock);
        }

        public KeyValuePair<string, Counter>[] GetCounterSnapshot()
        {
            return Snapshot(_counters, _counterLock);
        }

        public KeyValuePair<string, Gauge>[] GetGaugeSnapshot()
        {
            return Snapshot(_gauges, _gaugeLock);
        }

        public bool TryDequeueEvent(out string line)
        {
            return _eventQueue.TryDequeue(out line);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void RecordScopeInternal(string src, string name, double elapsedMs, int gc0Delta)
        {
            GetOrCreate(_scopes, _scopeLock, name).Observe(elapsedMs);
            if (_jsonlEnabled)
                _eventQueue.Enqueue(FormatScopeJson(src, name, elapsedMs, gc0Delta));
        }

        private static T GetOrCreate<T>(Dictionary<string, T> dict, object lockObj, string key)
            where T : new()
        {
            lock (lockObj)
            {
                T value;
                if (!dict.TryGetValue(key, out value))
                {
                    value = new T();
                    dict[key] = value;
                }
                return value;
            }
        }

        private static KeyValuePair<string, T>[] Snapshot<T>(Dictionary<string, T> dict, object lockObj)
        {
            lock (lockObj)
            {
                var result = new KeyValuePair<string, T>[dict.Count];
                int i = 0;
                foreach (var kv in dict)
                    result[i++] = kv;
                return result;
            }
        }

        private static long NowMs()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private static string Escape(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string FormatScopeJson(string src, string name, double ms, int gc0Delta)
        {
            return string.Format(
                "{{\"t\":{0},\"src\":\"{1}\",\"type\":\"scope\",\"name\":\"{2}\",\"ms\":{3:F3},\"gc0_delta\":{4}}}",
                NowMs(), src, Escape(name), ms, gc0Delta);
        }

        private static string FormatCounterJson(string src, string name, long delta)
        {
            return string.Format(
                "{{\"t\":{0},\"src\":\"{1}\",\"type\":\"counter\",\"name\":\"{2}\",\"delta\":{3}}}",
                NowMs(), src, Escape(name), delta);
        }

        private static string FormatGaugeJson(string src, string name, double value)
        {
            return string.Format(
                "{{\"t\":{0},\"src\":\"{1}\",\"type\":\"gauge\",\"name\":\"{2}\",\"value\":{3:F6}}}",
                NowMs(), src, Escape(name), value);
        }

        private static string FormatEventJson(string src, string name, string data)
        {
            string escapedData = data != null ? "\"" + Escape(data) + "\"" : "null";
            return string.Format(
                "{{\"t\":{0},\"src\":\"{1}\",\"type\":\"event\",\"name\":\"{2}\",\"data\":{3}}}",
                NowMs(), src, Escape(name), escapedData);
        }
    }
}
