using System;
using System.Threading;

namespace SEProfiler.Metrics
{
    public sealed class Histogram
    {
        public static readonly double[] DefaultBuckets = { 0.1, 0.5, 1.0, 5.0, 10.0, 50.0, 100.0, 500.0, 1000.0 };

        private readonly double[] _boundaries;
        private readonly long[] _counts;   // [0..BoundaryCount-1] = labelled buckets, [BoundaryCount] = +Inf
        private long _sumBits;
        private long _count;

        public int BoundaryCount { get { return _boundaries.Length; } }

        public Histogram() : this(null) { }

        public Histogram(double[] buckets)
        {
            _boundaries = buckets ?? DefaultBuckets;
            _counts = new long[_boundaries.Length + 1];
        }

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

        public long GetCount()
        {
            return Volatile.Read(ref _count);
        }

        public double GetSum()
        {
            return BitConverter.Int64BitsToDouble(Volatile.Read(ref _sumBits));
        }

        public long GetBucketCount(int index)
        {
            return Volatile.Read(ref _counts[index]);
        }

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
