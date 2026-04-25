using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SEProfiler;

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
    }
}
