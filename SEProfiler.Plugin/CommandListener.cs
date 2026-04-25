using System;
using System.IO;
using System.Text.RegularExpressions;
using VRage.FileSystem;

namespace SEProfiler
{
    public sealed class CommandListener : IDisposable
    {
        // Resolved in Start() via MyFileSystem.UserDataPath rather than a hardcoded
        // %APPDATA% path — Pulsar may cache the plugin DLL in a temporary location
        // that differs from the SE user data directory.
        public string WatchDir { get; private set; }

        private string _cmdFile;

        public Action<string, string> OnScope;    // (modId, outputPath)
        public Action               OnUnscope;
        public Action<string>       OnMode;       // (mode)

        private FileSystemWatcher _watcher;
        private DateTime _lastHandled = DateTime.MinValue;

        public void Start()
        {
            StartCore(Path.Combine(MyFileSystem.UserDataPath, "SEModProfiler"));
        }

        // Used by tests to bypass MyFileSystem dependency.
        internal void Start(string watchDir)
        {
            StartCore(watchDir);
        }

        private void StartCore(string watchDir)
        {
            WatchDir = watchDir;
            _cmdFile = Path.Combine(WatchDir, "cmd.json");

            Directory.CreateDirectory(WatchDir);

            _watcher = new FileSystemWatcher(WatchDir, "cmd.json")
            {
                NotifyFilter = NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };
            _watcher.Changed += OnFileChanged;
            _watcher.Created += OnFileChanged;
        }

        public void Dispose()
        {
            if (_watcher == null)
                return;
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _watcher = null;
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            // FileSystemWatcher fires multiple events per save — debounce to 500ms
            DateTime now = DateTime.UtcNow;
            if ((now - _lastHandled).TotalMilliseconds < 500)
                return;
            _lastHandled = now;

            string json = TryReadFile(_cmdFile);
            if (json == null)
                return;

            string cmd = ExtractField(json, "cmd");
            if (cmd == null)
                return;

            switch (cmd)
            {
                case "scope":
                    string modId      = ExtractField(json, "modId")      ?? string.Empty;
                    string outputPath = ExtractField(json, "outputPath") ?? string.Empty;
                    outputPath = Environment.ExpandEnvironmentVariables(outputPath);
                    if (OnScope != null)
                        OnScope(modId, outputPath);
                    break;

                case "unscope":
                    if (OnUnscope != null)
                        OnUnscope();
                    break;

                case "mode":
                    string mode = ExtractField(json, "mode");
                    if (mode != null && OnMode != null)
                        OnMode(mode);
                    break;
            }
        }

        private static string TryReadFile(string path)
        {
            // Retry once — the writer may still have the file open
            for (int attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    return File.ReadAllText(path);
                }
                catch (IOException)
                {
                    System.Threading.Thread.Sleep(50);
                }
            }
            return null;
        }

        private static readonly Regex _fieldPattern =
            new Regex("\"([^\"]+)\"\\s*:\\s*\"([^\"]*)\"", RegexOptions.Compiled);

        private static string ExtractField(string json, string field)
        {
            foreach (Match m in _fieldPattern.Matches(json))
            {
                if (m.Groups[1].Value == field)
                    return m.Groups[2].Value;
            }
            return null;
        }
    }
}
