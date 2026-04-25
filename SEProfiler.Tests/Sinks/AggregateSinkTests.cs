using Microsoft.VisualStudio.TestTools.UnitTesting;
using SEProfiler.Sinks;

namespace SEProfiler.Tests.Sinks
{
    [TestClass]
    public class AggregateSinkTests
    {
        private AggregateSink _sink;

        [TestInitialize]
        public void Setup()
        {
            _sink = new AggregateSink();
        }

        [TestMethod]
        public void RecordScope_RoutesToHistogram()
        {
            _sink.RecordScope("op", 1.5, 0);

            var scopes = _sink.GetScopeSnapshot();
            Assert.AreEqual(1, scopes.Length);
            Assert.AreEqual("op", scopes[0].Key);
            Assert.AreEqual(1L, scopes[0].Value.GetCount());
        }

        [TestMethod]
        public void RecordCounter_RoutesToCounter()
        {
            _sink.RecordCounter("hits", 3);

            var counters = _sink.GetCounterSnapshot();
            Assert.AreEqual(1, counters.Length);
            Assert.AreEqual("hits", counters[0].Key);
            Assert.AreEqual(3L, counters[0].Value.Value);
        }

        [TestMethod]
        public void RecordGauge_RoutesToGauge()
        {
            _sink.RecordGauge("cpu", 0.75);

            var gauges = _sink.GetGaugeSnapshot();
            Assert.AreEqual(1, gauges.Length);
            Assert.AreEqual("cpu", gauges[0].Key);
            Assert.AreEqual(0.75, gauges[0].Value.Value, 1e-10);
        }

        [TestMethod]
        public void RecordFrameworkScope_RoutesToHistogram()
        {
            _sink.RecordFrameworkScope("UpdateAfterSimulation", 2.0, 0);

            var scopes = _sink.GetScopeSnapshot();
            Assert.AreEqual(1, scopes.Length);
            Assert.AreEqual(1L, scopes[0].Value.GetCount());
        }

        [TestMethod]
        public void JsonlEnabled_False_NothingEnqueued()
        {
            _sink.JsonlEnabled = false;
            _sink.RecordScope("op", 1.0, 0);

            string line;
            Assert.IsFalse(_sink.TryDequeueEvent(out line));
        }

        [TestMethod]
        public void JsonlEnabled_True_ScopeEnqueued()
        {
            _sink.JsonlEnabled = true;
            _sink.RecordScope("op", 1.0, 0);

            string line;
            Assert.IsTrue(_sink.TryDequeueEvent(out line));
            Assert.IsNotNull(line);
        }

        [TestMethod]
        public void RecordEvent_Enqueued_WhenEnabled()
        {
            _sink.JsonlEnabled = true;
            _sink.RecordEvent("start", "data");

            string line;
            Assert.IsTrue(_sink.TryDequeueEvent(out line));
            StringAssert.Contains(line, "\"name\":\"start\"");
        }

        [TestMethod]
        public void TryDequeueEvent_False_WhenEmpty()
        {
            string line;
            Assert.IsFalse(_sink.TryDequeueEvent(out line));
        }

        [TestMethod]
        public void MultipleScopes_SameName_Accumulated()
        {
            _sink.RecordScope("op", 1.0, 0);
            _sink.RecordScope("op", 2.0, 0);
            _sink.RecordScope("op", 3.0, 0);

            var scopes = _sink.GetScopeSnapshot();
            Assert.AreEqual(1, scopes.Length);
            Assert.AreEqual(3L, scopes[0].Value.GetCount());
        }

        [TestMethod]
        public void ScopeJson_ContainsRequiredFields()
        {
            _sink.JsonlEnabled = true;
            _sink.RecordScope("testOp", 1.5, 2);

            string line;
            _sink.TryDequeueEvent(out line);

            StringAssert.Contains(line, "\"t\":");
            StringAssert.Contains(line, "\"src\":\"mod\"");
            StringAssert.Contains(line, "\"name\":\"testOp\"");
            StringAssert.Contains(line, "\"ms\":");
            StringAssert.Contains(line, "\"gc0_delta\":2");
        }
    }
}
