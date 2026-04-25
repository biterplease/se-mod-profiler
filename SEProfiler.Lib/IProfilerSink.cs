namespace SEProfiler
{
    public interface IProfilerSink
    {
        void RecordScope(string name, double elapsedMs, int gc0Delta);
        void RecordCounter(string name, long delta);
        void RecordGauge(string name, double value);
        void RecordEvent(string name, string data);
    }
}
