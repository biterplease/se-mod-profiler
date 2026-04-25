using System.Threading;

namespace SEProfiler.Metrics
{
    /// <summary>
    /// Thread-safe integer counter metric.
    /// </summary>
    public sealed class Counter
    {
        private long _value;

        /// <summary>
        /// Gets the current counter value.
        /// </summary>
        public long Value { get { return Volatile.Read(ref _value); } }

        /// <summary>
        /// Increments the counter by a delta.
        /// </summary>
        /// <param name="delta">Amount to add.</param>
        public void Increment(long delta = 1)
        {
            Interlocked.Add(ref _value, delta);
        }

        /// <summary>
        /// Resets the counter to zero.
        /// </summary>
        public void Reset()
        {
            Interlocked.Exchange(ref _value, 0L);
        }
    }
}
