using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SEProfiler.Metrics;

namespace SEProfiler.Tests.Metrics
{
    [TestClass]
    public class GaugeTests
    {
        [TestMethod]
        public void InitialValue_IsZero()
        {
            Assert.AreEqual(0.0, new Gauge().Value, 1e-15);
        }

        [TestMethod]
        public void Set_RoundTrips()
        {
            var g = new Gauge();
            g.Set(3.14);
            Assert.AreEqual(3.14, g.Value, 1e-10);
        }

        [TestMethod]
        public void Set_Zero_AfterNonZero()
        {
            var g = new Gauge();
            g.Set(99.0);
            g.Set(0.0);
            Assert.AreEqual(0.0, g.Value, 1e-15);
        }

        [TestMethod]
        public void Set_Negative()
        {
            var g = new Gauge();
            g.Set(-7.5);
            Assert.AreEqual(-7.5, g.Value, 1e-10);
        }

        [TestMethod]
        public void ThreadSafety_ConcurrentSets_ValueIsOneOfInputs()
        {
            var g = new Gauge();
            const int threads = 8;
            var barrier = new Barrier(threads);
            var workers = new Thread[threads];

            for (int i = 0; i < threads; i++)
            {
                double val = i;
                workers[i] = new Thread(() =>
                {
                    barrier.SignalAndWait();
                    for (int j = 0; j < 500; j++)
                        g.Set(val);
                });
                workers[i].Start();
            }
            foreach (var t in workers)
                t.Join();

            double final = g.Value;
            Assert.IsTrue(final >= 0.0 && final <= 7.0,
                string.Format("Final value {0} should be one of the thread values [0,7]", final));
        }
    }
}
