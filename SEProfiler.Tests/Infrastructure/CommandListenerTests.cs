using System;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SEProfiler;

namespace SEProfiler.Tests.Infrastructure
{
    [TestClass]
    public class CommandListenerTests
    {
        private string _tempDir;
        private CommandListener _listener;
        private string _cmdFile;

        [TestInitialize]
        public void Setup()
        {
            _tempDir  = Path.Combine(Path.GetTempPath(), "SEProfilerCmdTests_" + Guid.NewGuid().ToString("N"));
            _listener = new CommandListener();
            _listener.Start(_tempDir); // internal overload — no MyFileSystem dependency
            _cmdFile  = Path.Combine(_tempDir, "cmd.json");
        }

        [TestCleanup]
        public void Cleanup()
        {
            _listener.Dispose();
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        [TestMethod]
        public void WatchDir_SetToSuppliedPath()
        {
            Assert.AreEqual(_tempDir, _listener.WatchDir);
        }

        [TestMethod]
        public void Scope_Command_FiresCallback()
        {
            string receivedModId      = null;
            string receivedOutputPath = null;
            _listener.OnScope = (modId, path) =>
            {
                receivedModId      = modId;
                receivedOutputPath = path;
            };

            // Use a path without backslashes — the regex reader does not unescape JSON \\ sequences.
            File.WriteAllText(_cmdFile, "{\"cmd\":\"scope\",\"modId\":\"1234\",\"outputPath\":\"C:/out\"}");
            Thread.Sleep(700); // watcher + debounce (500ms) + slack

            Assert.AreEqual("1234", receivedModId);
            Assert.AreEqual("C:/out", receivedOutputPath);
        }

        [TestMethod]
        public void Unscope_Command_FiresCallback()
        {
            bool fired = false;
            _listener.OnUnscope = () => fired = true;

            File.WriteAllText(_cmdFile, "{\"cmd\":\"unscope\"}");
            Thread.Sleep(700);

            Assert.IsTrue(fired, "OnUnscope callback should fire");
        }

        [TestMethod]
        public void Mode_Command_FiresCallback()
        {
            string receivedMode = null;
            _listener.OnMode = mode => receivedMode = mode;

            File.WriteAllText(_cmdFile, "{\"cmd\":\"mode\",\"mode\":\"prometheus\"}");
            Thread.Sleep(700);

            Assert.AreEqual("prometheus", receivedMode);
        }

        [TestMethod]
        public void Mode_Jsonl_FiresCallback()
        {
            string receivedMode = null;
            _listener.OnMode = mode => receivedMode = mode;

            File.WriteAllText(_cmdFile, "{\"cmd\":\"mode\",\"mode\":\"jsonl\"}");
            Thread.Sleep(700);

            Assert.AreEqual("jsonl", receivedMode);
        }

        [TestMethod]
        public void Scope_EmptyModId_PassedThrough()
        {
            string receivedModId = "UNSET";
            _listener.OnScope = (modId, path) => receivedModId = modId;

            File.WriteAllText(_cmdFile, "{\"cmd\":\"scope\",\"modId\":\"\",\"outputPath\":\"\"}");
            Thread.Sleep(700);

            Assert.AreEqual(string.Empty, receivedModId);
        }
    }
}
