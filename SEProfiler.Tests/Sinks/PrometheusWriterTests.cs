using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SEProfiler.Sinks;

namespace SEProfiler.Tests.Sinks
{
    [TestClass]
    public class PrometheusWriterTests
    {
        private string _tempDir;
        private AggregateSink _sink;
        private PrometheusWriter _writer;

        [TestInitialize]
        public void Setup()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "SEProfilerPromTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _sink   = new AggregateSink();
            _writer = new PrometheusWriter(_sink);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        private string Flush()
        {
            string outputBase = Path.Combine(_tempDir, "session");
            _writer.Flush(outputBase);
            return File.ReadAllText(outputBase + ".prom");
        }

        [TestMethod]
        public void Flush_CreatesDotPromFile()
        {
            _sink.RecordScope("op", 1.0, 0);
            string outputBase = Path.Combine(_tempDir, "session");
            _writer.Flush(outputBase);

            Assert.IsTrue(File.Exists(outputBase + ".prom"));
        }

        [TestMethod]
        public void ScopeOutput_ContainsHistogramType()
        {
            _sink.RecordScope("op", 1.0, 0);
            StringAssert.Contains(Flush(), "# TYPE se_scope_duration_ms histogram");
        }

        [TestMethod]
        public void ScopeOutput_BucketLines_HaveLeLabel()
        {
            _sink.RecordScope("op", 1.0, 0);
            StringAssert.Contains(Flush(), "le=");
        }

        [TestMethod]
        public void ScopeOutput_HasPlusInfBucket()
        {
            _sink.RecordScope("op", 1.0, 0);
            StringAssert.Contains(Flush(), "le=\"+Inf\"");
        }

        [TestMethod]
        public void CounterOutput_HasTotalSuffix()
        {
            _sink.RecordCounter("events", 5);
            StringAssert.Contains(Flush(), "_total");
        }

        [TestMethod]
        public void GaugeOutput_ContainsGaugeType()
        {
            _sink.RecordGauge("cpu", 0.5);
            string content = Flush();
            StringAssert.Contains(content, "# TYPE");
            StringAssert.Contains(content, "gauge");
        }

        [TestMethod]
        public void HistogramBuckets_AreCumulative()
        {
            // 0.05 <= 0.1 → bucket[0]; 5.5 <= 10.0 → bucket[5]
            _sink.RecordScope("op", 0.05, 0);
            _sink.RecordScope("op", 5.5,  0);

            string content = Flush();

            // cumulative at le=0.1 should be 1; at le=+Inf should be 2
            StringAssert.Contains(content, "le=\"0.1\"} 1");
            StringAssert.Contains(content, "le=\"+Inf\"} 2");
        }

        [TestMethod]
        public void SanitizeName_Prefixed_WithSe()
        {
            _sink.RecordCounter("MyCounter", 1);
            StringAssert.Contains(Flush(), "se_");
        }
    }
}
