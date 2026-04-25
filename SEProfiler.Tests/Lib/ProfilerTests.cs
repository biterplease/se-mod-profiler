using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SEProfiler;
using System;

namespace SEProfiler.Tests.Lib
{
    [TestClass]
    public class ProfilerTests
    {
        [TestInitialize]
        public void Setup()   { Profiler.Sink = null; }

        [TestCleanup]
        public void Cleanup() { Profiler.Sink = null; }

        [TestMethod]
        public void IsActive_False_WhenSinkIsNull()
        {
            Assert.IsFalse(Profiler.IsActive);
        }

        [TestMethod]
        public void IsActive_True_WhenSinkIsSet()
        {
            Profiler.Sink = new Mock<IProfilerSink>().Object;
            Assert.IsTrue(Profiler.IsActive);
        }

        [TestMethod]
        public void Counter_NullSink_IsNoop()
        {
            Profiler.Counter("x", 1);
        }

        [TestMethod]
        public void Gauge_NullSink_IsNoop()
        {
            Profiler.Gauge("x", 1.0);
        }

        [TestMethod]
        public void Event_NullSink_IsNoop()
        {
            Profiler.Event("x", "data");
        }

        [TestMethod]
        public void Counter_DelegatesToSink()
        {
            var mock = new Mock<IProfilerSink>();
            Profiler.Sink = mock.Object;

            Profiler.Counter("hits", 3);

            mock.Verify(s => s.RecordCounter("hits", 3), Times.Once());
        }

        [TestMethod]
        public void Gauge_DelegatesToSink()
        {
            var mock = new Mock<IProfilerSink>();
            Profiler.Sink = mock.Object;

            Profiler.Gauge("cpu", 0.42);

            mock.Verify(s => s.RecordGauge("cpu", 0.42), Times.Once());
        }

        [TestMethod]
        public void Event_DelegatesToSink()
        {
            var mock = new Mock<IProfilerSink>();
            Profiler.Sink = mock.Object;

            Profiler.Event("start", "ok");

            mock.Verify(s => s.RecordEvent("start", "ok"), Times.Once());
        }

        [TestMethod]
        public void Scope_WithSink_CallsRecordScope()
        {
            var mock = new Mock<IProfilerSink>();
            Profiler.Sink = mock.Object;

            using (Profiler.Scope("op")) { }

            mock.Verify(s => s.RecordScope("op", It.IsAny<double>(), It.IsAny<int>()), Times.Once());
        }

        [TestMethod]
        public void Scope_NullSink_IsNoop()
        {
            using (Profiler.Scope("op")) { }
        }

        [TestMethod]
        public void ScopeAttribute_HasExpectedUsageAndName()
        {
            var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(typeof(ScopeAttribute), typeof(AttributeUsageAttribute));
            Assert.IsNotNull(usage);
            Assert.AreEqual(AttributeTargets.Method, usage.ValidOn);
            Assert.IsTrue(usage.AllowMultiple);
            Assert.IsFalse(usage.Inherited);

            var attr = new ScopeAttribute("scope.name");
            Assert.AreEqual("scope.name", attr.Name);
        }

        [TestMethod]
        public void CounterAttribute_HasExpectedDefaultsAndProperties()
        {
            var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(typeof(CounterAttribute), typeof(AttributeUsageAttribute));
            Assert.IsNotNull(usage);
            Assert.AreEqual(AttributeTargets.Method, usage.ValidOn);
            Assert.IsTrue(usage.AllowMultiple);
            Assert.IsFalse(usage.Inherited);

            var defaultDelta = new CounterAttribute("counter.name");
            Assert.AreEqual("counter.name", defaultDelta.Name);
            Assert.AreEqual(1L, defaultDelta.Delta);

            var explicitDelta = new CounterAttribute("counter.name", 10L);
            Assert.AreEqual("counter.name", explicitDelta.Name);
            Assert.AreEqual(10L, explicitDelta.Delta);
        }

        [TestMethod]
        public void GaugeAttribute_HasExpectedUsageAndValues()
        {
            var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(typeof(GaugeAttribute), typeof(AttributeUsageAttribute));
            Assert.IsNotNull(usage);
            Assert.AreEqual(AttributeTargets.Method, usage.ValidOn);
            Assert.IsTrue(usage.AllowMultiple);
            Assert.IsFalse(usage.Inherited);

            var attr = new GaugeAttribute("gauge.name", 16.67);
            Assert.AreEqual("gauge.name", attr.Name);
            Assert.AreEqual(16.67, attr.Value);
        }

        [TestMethod]
        public void EventAttribute_HasExpectedDefaultsAndProperties()
        {
            var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(typeof(EventAttribute), typeof(AttributeUsageAttribute));
            Assert.IsNotNull(usage);
            Assert.AreEqual(AttributeTargets.Method, usage.ValidOn);
            Assert.IsTrue(usage.AllowMultiple);
            Assert.IsFalse(usage.Inherited);

            var noData = new EventAttribute("event.name");
            Assert.AreEqual("event.name", noData.Name);
            Assert.IsNull(noData.Data);

            var withData = new EventAttribute("event.name", "payload");
            Assert.AreEqual("event.name", withData.Name);
            Assert.AreEqual("payload", withData.Data);
        }
    }
}
