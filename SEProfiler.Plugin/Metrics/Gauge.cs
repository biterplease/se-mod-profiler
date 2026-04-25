using System;
using System.Threading;

namespace SEProfiler.Metrics
{
    /// <summary>
    /// Thread-safe floating-point metric that stores the latest value.
    /// </summary>
    public sealed class Gauge
    {
        private long _bits;

        /// <summary>
        /// Gets the current gauge value.
        /// </summary>
        public double Value
        {
            get { return BitConverter.Int64BitsToDouble(Volatile.Read(ref _bits)); }
        }

        /// <summary>
        /// Sets the gauge to a new value.
        /// </summary>
        /// <param name="value">Value to store.</param>
        public void Set(double value)
        {
            Interlocked.Exchange(ref _bits, BitConverter.DoubleToInt64Bits(value));
        }
    }
}
