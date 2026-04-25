using System;
using System.Threading;

namespace SEProfiler.Metrics
{
    public sealed class Gauge
    {
        private long _bits;

        public double Value
        {
            get { return BitConverter.Int64BitsToDouble(Volatile.Read(ref _bits)); }
        }

        public void Set(double value)
        {
            Interlocked.Exchange(ref _bits, BitConverter.DoubleToInt64Bits(value));
        }
    }
}
