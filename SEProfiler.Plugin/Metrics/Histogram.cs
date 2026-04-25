using System;
using System.Threading;

namespace SEProfiler.Metrics
{
    /// <summary>
    /// Thread-safe histogram metric with fixed bucket boundaries.
    /// </summary>
    public sealed class Histogram
    {
        /// <summary>
        /// Default bucket upper bounds in milliseconds.
        /// </summary>
        public static readonly double[] DefaultBuckets = { 0.1, 0.5, 1.0, 5.0, 10.0, 50.0, 100.0, 500.0, 1000.0 };

        private readonly double[] _boundaries;
        private readonly long[] _counts;   // [0..BoundaryCount-1] = labelled buckets, [BoundaryCount] = +Inf
        private long _sumBits;
        private long _count;

        /// <summary>
        /// Gets the number of configured finite bucket boundaries.
        /// </summary>
        public int BoundaryCount { get { return _boundaries.Length; } }

        /// <summary>
        /// Creates a histogram with default bucket boundaries.
        /// </summary>
        public Histogram() : this(null) { }

        /// <summary>
        /// Creates a histogram with custom bucket boundaries.
        /// </summary>
        /// <param name="buckets">Ascending bucket upper bounds; null uses defaults.</param>
        public Histogram(double[] buckets)
        {
            _boundaries = buckets ?? DefaultBuckets;
            _counts = new long[_boundaries.Length + 1];
        }

        /// <summary>
        /// Records a sample into the matching bucket and updates totals.
        /// </summary>
        /// <param name="value">Sample value.</param>
        public void Observe(double value)
        {
            int index = _boundaries.Length;
            for (int i = 0; i < _boundaries.Length; i++)
            {
                if (value <= _boundaries[i])
                {
                    index = i;
                    break;
                }
            }

            Interlocked.Increment(ref _counts[index]);
            Interlocked.Increment(ref _count);
            AddToSum(value);
        }

        /// <summary>
        /// Gets the total number of observed samples.
        /// </summary>
        public long GetCount()
        {
            return Volatile.Read(ref _count);
        }

        /// <summary>
        /// Gets the sum of all observed sample values.
        /// </summary>
        public double GetSum()
        {
            return BitConverter.Int64BitsToDouble(Volatile.Read(ref _sumBits));
        }

        /// <summary>
        /// Gets the sample count for a bucket index.
        /// </summary>
        /// <param name="index">Bucket index, where the last index is +Inf.</param>
        /// <returns>Sample count for the bucket.</returns>
        public long GetBucketCount(int index)
        {
            return Volatile.Read(ref _counts[index]);
        }

        /// <summary>
        /// Gets the finite upper boundary for a bucket index.
        /// </summary>
        /// <param name="index">Finite bucket index.</param>
        /// <returns>Bucket upper boundary.</returns>
        public double GetBucketBoundary(int index)
        {
            return _boundaries[index];
        }

        private void AddToSum(double value)
        {
            long original, updated;
            do
            {
                original = Volatile.Read(ref _sumBits);
                updated = BitConverter.DoubleToInt64Bits(
                    BitConverter.Int64BitsToDouble(original) + value);
            }
            while (Interlocked.CompareExchange(ref _sumBits, updated, original) != original);
        }
    }
}
