namespace SEProfiler
{
    /// <summary>
    /// Receives profiling metrics emitted by instrumented code.
    /// </summary>
    public interface IProfilerSink
    {
        /// <summary>
        /// Records a completed timed scope sample.
        /// </summary>
        /// <example>
        /// Typical usage through <c>Profiler.Scope</c>:
        /// <code>
        /// using (Profiler.Scope("MyMod.Update"))
        /// {
        ///     // Work to measure.
        /// }
        /// </code>
        /// </example>
        /// <param name="name">Scope name.</param>
        /// <param name="elapsedMs">Elapsed time in milliseconds.</param>
        /// <param name="gc0Delta">Gen0 collection delta during the scope.</param>
        void RecordScope(string name, double elapsedMs, int gc0Delta);

        /// <summary>
        /// Records a counter increment.
        /// </summary>
        /// <param name="name">Counter name.</param>
        /// <param name="delta">Increment amount.</param>
        void RecordCounter(string name, long delta);

        /// <summary>
        /// Records a gauge value update.
        /// </summary>
        /// <param name="name">Gauge name.</param>
        /// <param name="value">Current gauge value.</param>
        void RecordGauge(string name, double value);

        /// <summary>
        /// Records an event with optional payload data.
        /// </summary>
        /// <param name="name">Event name.</param>
        /// <param name="data">Optional event payload.</param>
        void RecordEvent(string name, string data);
    }
}
