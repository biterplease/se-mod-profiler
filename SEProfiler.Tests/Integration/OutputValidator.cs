using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SEProfiler.Tests.Integration
{
    /// <summary>
    /// Validates output files produced by a real SE session with the profiler active.
    ///
    /// Usage (manual):
    ///   set SEMOD_PROFILER_OUTPUT=%APPDATA%\SpaceEngineers\SEModProfiler
    ///   dotnet test --filter TestCategory=Integration
    ///
    /// If the environment variable is not set, the default SE user-data path is used.
    /// </summary>
    [TestClass]
    public class OutputValidator
    {
        private static readonly string OutputDir =
            Environment.GetEnvironmentVariable("SEMOD_PROFILER_OUTPUT")
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SpaceEngineers", "SEModProfiler");

        private string   _jsonlPath;
        private string   _promPath;
        private string[] _jsonlLines;

        [TestInitialize]
        public void Setup()
        {
            _jsonlPath = Path.Combine(OutputDir, "session.jsonl");
            _promPath  = Path.Combine(OutputDir, "session.prom");

            if (!File.Exists(_jsonlPath))
                Assert.Inconclusive("JSONL output file not found: " + _jsonlPath +
                    "\nRun SE with the profiler active for at least 60 s, then re-run this suite.");

            _jsonlLines = File.ReadAllLines(_jsonlPath);
        }

        [TestMethod, TestCategory("Integration")]
        public void Jsonl_NonEmpty()
        {
            Assert.IsTrue(_jsonlLines.Length > 0, "JSONL file is empty");
        }

        [TestMethod, TestCategory("Integration")]
        public void Jsonl_AllLines_AreValidJsonObjects()
        {
            foreach (string line in _jsonlLines)
            {
                string t = line.Trim();
                if (t.Length == 0) continue;
                Assert.IsTrue(t.StartsWith("{") && t.EndsWith("}"),
                    "Line is not a JSON object: " + t);
            }
        }

        [TestMethod, TestCategory("Integration")]
        public void Jsonl_AllLines_HaveRequiredFields()
        {
            var tPat    = new Regex("\"t\"\\s*:");
            var srcPat  = new Regex("\"src\"\\s*:");
            var namePat = new Regex("\"name\"\\s*:");

            foreach (string line in _jsonlLines)
            {
                string t = line.Trim();
                if (t.Length == 0) continue;
                Assert.IsTrue(tPat.IsMatch(t),    "Missing 't' field: "    + t);
                Assert.IsTrue(srcPat.IsMatch(t),  "Missing 'src' field: "  + t);
                Assert.IsTrue(namePat.IsMatch(t), "Missing 'name' field: " + t);
            }
        }

        [TestMethod, TestCategory("Integration")]
        public void Jsonl_Timestamps_AreMonotonicallyNonDecreasing()
        {
            var tPat = new Regex("\"t\"\\s*:\\s*(\\d+)");
            long prev = long.MinValue;

            foreach (string line in _jsonlLines)
            {
                string t = line.Trim();
                if (t.Length == 0) continue;
                var m = tPat.Match(t);
                if (!m.Success) continue;
                long ts = long.Parse(m.Groups[1].Value);
                Assert.IsTrue(ts >= prev,
                    string.Format("Timestamp went backwards: {0} < {1}", ts, prev));
                prev = ts;
            }
        }

        [TestMethod, TestCategory("Integration")]
        public void Jsonl_HasAtLeastOneFrameworkSrcEvent()
        {
            bool found = false;
            foreach (string line in _jsonlLines)
            {
                if (line.Contains("\"src\":\"framework\"")) { found = true; break; }
            }
            Assert.IsTrue(found, "No framework-source events — Harmony patches may not have fired");
        }

        [TestMethod, TestCategory("Integration")]
        public void Jsonl_MsValues_AreNonNegative()
        {
            var msPat = new Regex("\"ms\"\\s*:\\s*(-?[\\d.]+)");
            foreach (string line in _jsonlLines)
            {
                var m = msPat.Match(line);
                if (!m.Success) continue;
                double ms = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                Assert.IsTrue(ms >= 0, "Negative ms on line: " + line);
            }
        }

        [TestMethod, TestCategory("Integration")]
        public void Jsonl_MsValues_AreBelowSanityBound()
        {
            const double maxMs = 10000.0;
            var msPat = new Regex("\"ms\"\\s*:\\s*(-?[\\d.]+)");
            foreach (string line in _jsonlLines)
            {
                var m = msPat.Match(line);
                if (!m.Success) continue;
                double ms = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                Assert.IsTrue(ms <= maxMs,
                    string.Format("ms={0} exceeds sanity bound {1} on line: {2}", ms, maxMs, line));
            }
        }

        [TestMethod, TestCategory("Integration")]
        public void Jsonl_HasAtLeastOneEtwGcEvent()
        {
            bool found = false;
            foreach (string line in _jsonlLines)
            {
                if (line.Contains("\"src\":\"etw\"") && line.Contains("GC")) { found = true; break; }
            }
            Assert.IsTrue(found, "No ETW GC events — RuntimeEventListener may not be active");
        }

        [TestMethod, TestCategory("Integration")]
        public void Prom_BucketLines_HaveLeLabel()
        {
            if (!File.Exists(_promPath))
                Assert.Inconclusive("Prometheus output not found (prometheus mode was not enabled)");

            string content = File.ReadAllText(_promPath);
            foreach (string line in content.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.Contains("_bucket{"))
                    Assert.IsTrue(line.Contains("le="), "Bucket line missing 'le' label: " + line);
            }
        }

        [TestMethod, TestCategory("Integration")]
        public void Prom_HistogramCountMatchesPlusInfBucket()
        {
            if (!File.Exists(_promPath))
                Assert.Inconclusive("Prometheus output not found");

            string content = File.ReadAllText(_promPath);
            var countPat = new Regex("se_scope_duration_ms_count\\{[^}]+\\}\\s+(\\d+)");
            var infPat   = new Regex("se_scope_duration_ms_bucket\\{[^}]+le=\"\\+Inf\"\\}\\s+(\\d+)");

            var cm = countPat.Match(content);
            var im = infPat.Match(content);
            if (!cm.Success || !im.Success) return;

            Assert.AreEqual(long.Parse(cm.Groups[1].Value), long.Parse(im.Groups[1].Value),
                "+Inf bucket count should equal histogram _count");
        }
    }
}
