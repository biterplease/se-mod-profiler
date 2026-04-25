using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SEProfiler;

namespace SEProfiler.Tests.Lib
{
    [TestClass]
    public class ProfileScopeTests
    {
        [TestMethod]
        public void NullSink_Dispose_IsNoop()
        {
            var scope = new ProfileScope("test", null);
            scope.Dispose();
        }

        [TestMethod]
        public void ElapsedMs_IsNonNegative()
        {
            var mock = new Mock<IProfilerSink>();
            double recorded = -1;
            mock.Setup(s => s.RecordScope(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<int>()))
                .Callback<string, double, int>((n, ms, gc) => recorded = ms);

            var scope = new ProfileScope("x", mock.Object);
            Thread.Sleep(5);
            scope.Dispose();

            Assert.IsTrue(recorded >= 0, "elapsed ms should be non-negative");
        }

        [TestMethod]
        public void Gc0Delta_IsNonNegative()
        {
            var mock = new Mock<IProfilerSink>();
            int recordedGc = -999;
            mock.Setup(s => s.RecordScope(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<int>()))
                .Callback<string, double, int>((n, ms, gc) => recordedGc = gc);

            var scope = new ProfileScope("x", mock.Object);
            GC.Collect(0);
            scope.Dispose();

            Assert.IsTrue(recordedGc >= 0, "gc0 delta should be non-negative");
        }

        [TestMethod]
        public void Name_PassedThrough()
        {
            var mock = new Mock<IProfilerSink>();
            string recordedName = null;
            mock.Setup(s => s.RecordScope(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<int>()))
                .Callback<string, double, int>((n, ms, gc) => recordedName = n);

            using (new ProfileScope("MyOp", mock.Object)) { }

            Assert.AreEqual("MyOp", recordedName);
        }
    }
}
