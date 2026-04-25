using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SEProfiler.Sinks;

namespace SEProfiler.Tests.Sinks
{
    [TestClass]
    public class JsonlWriterTests
    {
        private string _tempDir;
        private AggregateSink _sink;
        private JsonlWriter _writer;

        [TestInitialize]
        public void Setup()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "SEProfilerJsonlTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _sink   = new AggregateSink();
            _writer = new JsonlWriter(_sink);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _writer.Close();
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        [TestMethod]
        public void Open_CreatesFile_AndEnablesJsonl()
        {
            string outputBase = Path.Combine(_tempDir, "session");
            _writer.Open(outputBase);

            Assert.IsTrue(File.Exists(outputBase + ".jsonl"), "file should be created on Open");
            Assert.IsTrue(_sink.JsonlEnabled, "JsonlEnabled should be true after Open");
        }

        [TestMethod]
        public void Close_DisablesJsonl()
        {
            _writer.Open(Path.Combine(_tempDir, "session"));
            _writer.Close();

            Assert.IsFalse(_sink.JsonlEnabled);
        }

        [TestMethod]
        public void Flush_WritesQueuedLines()
        {
            string outputBase = Path.Combine(_tempDir, "session");
            _writer.Open(outputBase);

            _sink.RecordScope("op", 1.0, 0);
            _writer.Flush();
            _writer.Close();

            string[] lines = File.ReadAllLines(outputBase + ".jsonl");
            Assert.AreEqual(1, lines.Length);
        }

        [TestMethod]
        public void Lines_HaveRequiredFields()
        {
            string outputBase = Path.Combine(_tempDir, "session");
            _writer.Open(outputBase);
            _sink.RecordScope("myOp", 2.5, 1);
            _writer.Close(); // Close flushes remainder

            string content = File.ReadAllText(outputBase + ".jsonl");
            StringAssert.Contains(content, "\"t\":");
            StringAssert.Contains(content, "\"src\":");
            StringAssert.Contains(content, "\"name\":\"myOp\"");
        }

        [TestMethod]
        public void Close_Idempotent()
        {
            _writer.Open(Path.Combine(_tempDir, "session"));
            _writer.Close();
            _writer.Close(); // must not throw
        }

        [TestMethod]
        public void MultipleScopes_WriteMultipleLines()
        {
            string outputBase = Path.Combine(_tempDir, "session");
            _writer.Open(outputBase);

            _sink.RecordScope("a", 1.0, 0);
            _sink.RecordScope("b", 2.0, 0);
            _sink.RecordScope("c", 3.0, 0);
            _writer.Close();

            string[] lines = File.ReadAllLines(outputBase + ".jsonl");
            Assert.AreEqual(3, lines.Length);
        }
    }
}
