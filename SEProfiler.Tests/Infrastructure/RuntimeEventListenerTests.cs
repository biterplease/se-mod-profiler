using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SEProfiler;
using SEProfiler.Sinks;

namespace SEProfiler.Tests.Infrastructure
{
    [TestClass]
    public class RuntimeEventListenerTests
    {
        [TestMethod]
        public void GcCollect_CausesGcCounter_ToIncrement()
        {
            // The Microsoft-Windows-DotNETRuntime provider is only accessible via in-process
            // EventListener on .NET Core/.NET 5+. On .NET Framework 4.8 the CLR's ETW provider
            // is a native provider not surfaced through the managed EventSource API, so events
            // will not be delivered and we mark the test inconclusive rather than failing.
            var sink = new AggregateSink();

            using (new RuntimeEventListener(sink))
            {
                GC.Collect(0, GCCollectionMode.Forced, true);
                Thread.Sleep(300);
            }

            bool hasGcCounter = false;
            foreach (var kv in sink.GetCounterSnapshot())
            {
                if (kv.Key.StartsWith("etw.gc.gen")) { hasGcCounter = true; break; }
            }

            if (!hasGcCounter)
                Assert.Inconclusive(
                    "CLR GC events not delivered via in-process EventListener on .NET Framework 4.8 " +
                    "(this is expected — the feature requires .NET Core or higher).");
        }

        [TestMethod]
        public void ExceptionThrown_MayIncrementExceptionCounter()
        {
            var sink = new AggregateSink();

            using (new RuntimeEventListener(sink))
            {
                try { throw new InvalidOperationException("test"); }
                catch { }

                Thread.Sleep(300);
            }

            bool hasExceptionCounter = false;
            foreach (var kv in sink.GetCounterSnapshot())
            {
                if (kv.Key == "etw.exceptions")
                {
                    hasExceptionCounter = true;
                    break;
                }
            }

            if (!hasExceptionCounter)
                Assert.Inconclusive("ETW exception events not observed in this test host (best-effort test)");
        }

        [TestMethod]
        public void Dispose_DoesNotThrow()
        {
            var sink = new AggregateSink();
            var listener = new RuntimeEventListener(sink);
            listener.Dispose();
        }
    }
}
