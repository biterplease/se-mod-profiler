using Microsoft.VisualStudio.TestTools.UnitTesting;
using SEProfiler.Metrics;

namespace SEProfiler.Tests.Metrics
{
    [TestClass]
    public class HistogramTests
    {
        [TestMethod]
        public void BoundaryCount_MatchesDefaultBuckets()
        {
            Assert.AreEqual(Histogram.DefaultBuckets.Length, new Histogram().BoundaryCount);
        }

        [TestMethod]
        public void Observe_BelowFirstBucket_GoesToBucket0()
        {
            var h = new Histogram();
            h.Observe(0.05); // < 0.1
            Assert.AreEqual(1L, h.GetBucketCount(0));
        }

        [TestMethod]
        public void Observe_ExactBoundary_GoesToThatBucket()
        {
            var h = new Histogram();
            h.Observe(0.5); // exactly the second boundary (index 1)
            Assert.AreEqual(0L, h.GetBucketCount(0));
            Assert.AreEqual(1L, h.GetBucketCount(1));
        }

        [TestMethod]
        public void Observe_AboveAllBuckets_GoesToInfBucket()
        {
            var h = new Histogram();
            h.Observe(9999.0); // > 1000.0 (max boundary)
            Assert.AreEqual(1L, h.GetBucketCount(h.BoundaryCount)); // +Inf
        }

        [TestMethod]
        public void Count_Accumulates()
        {
            var h = new Histogram();
            h.Observe(1.0);
            h.Observe(2.0);
            h.Observe(3.0);
            Assert.AreEqual(3L, h.GetCount());
        }

        [TestMethod]
        public void Sum_Accumulates()
        {
            var h = new Histogram();
            h.Observe(1.0);
            h.Observe(2.0);
            h.Observe(3.0);
            Assert.AreEqual(6.0, h.GetSum(), 1e-9);
        }

        [TestMethod]
        public void CustomBuckets_Respected()
        {
            var h = new Histogram(new double[] { 1.0, 2.0 });
            h.Observe(0.5); // <= 1.0 → bucket 0
            h.Observe(1.5); // <= 2.0 → bucket 1
            h.Observe(3.0); // > 2.0 → +Inf (index 2)

            Assert.AreEqual(1L, h.GetBucketCount(0));
            Assert.AreEqual(1L, h.GetBucketCount(1));
            Assert.AreEqual(1L, h.GetBucketCount(2));
        }

        [TestMethod]
        public void BucketBoundary_MatchesInput()
        {
            var h = new Histogram(new double[] { 0.5, 2.5 });
            Assert.AreEqual(0.5, h.GetBucketBoundary(0), 1e-15);
            Assert.AreEqual(2.5, h.GetBucketBoundary(1), 1e-15);
        }
    }
}
