using System;

namespace SEProfiler
{
    /// <summary>
    /// Marks a method for automatic scope instrumentation.
    /// Tooling can transform the method body to wrap execution in Profiler.Scope(Name).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class ScopeAttribute : Attribute
    {
        public ScopeAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }
    }

    /// <summary>
    /// Marks a method for automatic counter instrumentation.
    /// Tooling can inject Profiler.Counter(Name, Delta) at method entry.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class CounterAttribute : Attribute
    {
        public CounterAttribute(string name)
            : this(name, 1)
        {
        }

        public CounterAttribute(string name, long delta)
        {
            Name = name;
            Delta = delta;
        }

        public string Name { get; private set; }

        public long Delta { get; private set; }
    }

    /// <summary>
    /// Marks a method for automatic gauge instrumentation using a compile-time constant value.
    /// Dynamic runtime values should use explicit Profiler.Gauge calls inside the method body.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class GaugeAttribute : Attribute
    {
        public GaugeAttribute(string name, double value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; private set; }

        public double Value { get; private set; }
    }

    /// <summary>
    /// Marks a method for automatic event instrumentation.
    /// Tooling can inject Profiler.Event(Name, Data) at method entry.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class EventAttribute : Attribute
    {
        public EventAttribute(string name)
            : this(name, null)
        {
        }

        public EventAttribute(string name, string data)
        {
            Name = name;
            Data = data;
        }

        public string Name { get; private set; }

        public string Data { get; private set; }
    }
}
