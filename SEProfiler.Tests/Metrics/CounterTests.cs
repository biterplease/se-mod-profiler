using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SEProfiler.Metrics;

namespace SEProfiler.Tests.Metrics
{
    [TestClass]
    public class CounterTests
    {
        [TestMethod]
        public void InitialValue_IsZero()
        {
            Assert.AreEqual(0L, new Counter().Value);
        }

        [TestMethod]
        public void Increment_ByOne()
        {
            var c = new Counter();
            c.Increment();
            Assert.AreEqual(1L, c.Value);
        }

        [TestMethod]
        public void Increment_ByN()
        {
            var c = new Counter();
            c.Increment(7);
            Assert.AreEqual(7L, c.Value);
        }

        [TestMethod]
        public void Reset_SetsToZero()
        {
            var c = new Counter();
            c.Increment(10);
            c.Reset();
            Assert.AreEqual(0L, c.Value);
        }

        [TestMethod]
        public void ThreadSafety_ConcurrentIncrements()
        {
            var c = new Counter();
            const int threads = 8;
            const int ops     = 1000;
            var barrier = new Barrier(threads);
            var workers = new Thread[threads];

            for (int i = 0; i < threads; i++)
            {
                workers[i] = new Thread(() =>
                {
                    barrier.SignalAndWait();
                    for (int j = 0; j < ops; j++)
                        c.Increment();
                });
                workers[i].Start();
            }
            foreach (var t in workers)
                t.Join();

            Assert.AreEqual((long)threads * ops, c.Value);
        }
    }
}
