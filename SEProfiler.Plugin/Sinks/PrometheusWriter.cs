#if PROMETHEUS_ENABLED
using System.IO;
using System.Text;
using SEProfiler.Metrics;

namespace SEProfiler.Sinks
{
    public sealed class PrometheusWriter
    {
        private readonly AggregateSink _sink;
        private readonly StringBuilder _sb = new StringBuilder(4096);

        public PrometheusWriter(AggregateSink sink)
        {
            _sink = sink;
        }

        public void Flush(string outputPath)
        {
            _sb.Clear();

            WriteScopes();
            WriteCounters();
            WriteGauges();

            string dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(outputPath + ".prom", _sb.ToString(), Encoding.UTF8);
        }

        private void WriteScopes()
        {
            var scopes = _sink.GetScopeSnapshot();
            if (scopes.Length == 0)
                return;

            _sb.AppendLine("# HELP se_scope_duration_ms Execution time of named scopes in milliseconds");
            _sb.AppendLine("# TYPE se_scope_duration_ms histogram");

            foreach (var kv in scopes)
            {
                string name = kv.Key;
                Histogram h = kv.Value;

                long cumulative = 0;
                for (int i = 0; i < h.BoundaryCount; i++)
                {
                    cumulative += h.GetBucketCount(i);
                    _sb.AppendFormat(
                        "se_scope_duration_ms_bucket{{name=\"{0}\",le=\"{1}\"}} {2}\n",
                        name, h.GetBucketBoundary(i).ToString("G"), cumulative);
                }

                // +Inf bucket includes everything
                cumulative += h.GetBucketCount(h.BoundaryCount);
                _sb.AppendFormat(
                    "se_scope_duration_ms_bucket{{name=\"{0}\",le=\"+Inf\"}} {1}\n",
                    name, cumulative);

                _sb.AppendFormat("se_scope_duration_ms_sum{{name=\"{0}\"}} {1:F3}\n",   name, h.GetSum());
                _sb.AppendFormat("se_scope_duration_ms_count{{name=\"{0}\"}} {1}\n\n",  name, h.GetCount());
            }
        }

        private void WriteCounters()
        {
            var counters = _sink.GetCounterSnapshot();
            if (counters.Length == 0)
                return;

            foreach (var kv in counters)
            {
                string metricName = SanitizeName(kv.Key);
                _sb.AppendFormat("# HELP {0}_total {1}\n", metricName, kv.Key);
                _sb.AppendFormat("# TYPE {0}_total counter\n", metricName);
                _sb.AppendFormat("{0}_total {1}\n\n", metricName, kv.Value.Value);
            }
        }

        private void WriteGauges()
        {
            var gauges = _sink.GetGaugeSnapshot();
            if (gauges.Length == 0)
                return;

            foreach (var kv in gauges)
            {
                string metricName = SanitizeName(kv.Key);
                _sb.AppendFormat("# HELP {0} {1}\n", metricName, kv.Key);
                _sb.AppendFormat("# TYPE {0} gauge\n", metricName);
                _sb.AppendFormat("{0} {1:F6}\n\n", metricName, kv.Value.Value);
            }
        }

        private static string SanitizeName(string name)
        {
            var sb = new StringBuilder(name.Length + 3);
            sb.Append("se_");
            foreach (char c in name)
                sb.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '_');
            return sb.ToString();
        }
    }
}
#endif
