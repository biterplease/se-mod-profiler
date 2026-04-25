using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using SEProfiler.Sinks;

namespace SEProfiler
{
    public sealed class RuntimeEventListener : EventListener
    {
        // CLR runtime ETW event IDs (.NET 4.x)
        private const int EventId_GCStart          = 1;
        private const int EventId_GCEnd            = 2;
        private const int EventId_GCAllocationTick = 10;
        private const int EventId_ExceptionThrown  = 80;

        // Keyword flags for Microsoft-Windows-DotNETRuntime
        private const EventKeywords GCKeyword        = (EventKeywords)0x1L;
        private const EventKeywords ExceptionKeyword = (EventKeywords)0x8000L;

        private readonly AggregateSink _sink;

        // Tracks GC start timestamps keyed by GC count to compute duration
        private long _gcStartTimestamp;

        public RuntimeEventListener(AggregateSink sink)
        {
            _sink = sink;
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == "Microsoft-Windows-DotNETRuntime")
                EnableEvents(eventSource, EventLevel.Informational, GCKeyword | ExceptionKeyword);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (_sink == null)
                return;

            switch (eventData.EventId)
            {
                case EventId_GCStart:
                    HandleGCStart(eventData);
                    break;
                case EventId_GCEnd:
                    HandleGCEnd(eventData);
                    break;
                case EventId_GCAllocationTick:
                    HandleAllocationTick(eventData);
                    break;
                case EventId_ExceptionThrown:
                    HandleException(eventData);
                    break;
            }
        }

        private void HandleGCStart(EventWrittenEventArgs e)
        {
            _gcStartTimestamp = Stopwatch.GetTimestamp();

            int generation = SafePayloadInt(e, 1);
            _sink.RecordEtwCounter("etw.gc.gen" + generation);
            _sink.RecordEtwEvent("GCStart", "gen=" + generation);
        }

        private void HandleGCEnd(EventWrittenEventArgs e)
        {
            double ms = (Stopwatch.GetTimestamp() - _gcStartTimestamp)
                        * 1000.0 / Stopwatch.Frequency;
            int generation = SafePayloadInt(e, 1);
            _sink.RecordEtwEvent("GCEnd", string.Format("gen={0},ms={1:F3}", generation, ms));
        }

        private void HandleAllocationTick(EventWrittenEventArgs e)
        {
            _sink.RecordEtwCounter("etw.gc.alloc_ticks");

            // Payload[0] = AllocationAmount (uint, ~100KB chunks)
            // Payload[6] = TypeName (string) when available
            string typeName = SafePayloadString(e, 6);
            if (typeName != null)
                _sink.RecordEtwEvent("GCAllocationTick", "type=" + typeName);
            else
                _sink.RecordEtwEvent("GCAllocationTick", null);
        }

        private void HandleException(EventWrittenEventArgs e)
        {
            string exType    = SafePayloadString(e, 0);
            string exMessage = SafePayloadString(e, 1);
            string data      = string.Format("type={0},msg={1}",
                exType    ?? "unknown",
                exMessage ?? string.Empty);
            _sink.RecordEtwCounter("etw.exceptions");
            _sink.RecordEtwEvent("ExceptionThrown", data);
        }

        private static int SafePayloadInt(EventWrittenEventArgs e, int index)
        {
            try
            {
                if (e.Payload != null && e.Payload.Count > index && e.Payload[index] != null)
                    return Convert.ToInt32(e.Payload[index]);
            }
            catch { }
            return 0;
        }

        private static string SafePayloadString(EventWrittenEventArgs e, int index)
        {
            try
            {
                if (e.Payload != null && e.Payload.Count > index && e.Payload[index] != null)
                    return e.Payload[index].ToString();
            }
            catch { }
            return null;
        }
    }
}
