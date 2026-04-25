using System.Threading;

namespace SEProfiler.Metrics
{
    public sealed class Counter
    {
        private long _value;

        public long Value { get { return Volatile.Read(ref _value); } }

        public void Increment(long delta = 1)
        {
            Interlocked.Add(ref _value, delta);
        }

        public void Reset()
        {
            Interlocked.Exchange(ref _value, 0L);
        }
    }
}
