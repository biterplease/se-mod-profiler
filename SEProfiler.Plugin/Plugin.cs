using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Sandbox.Graphics.GUI;
using SEProfiler.Settings;
using SEProfiler.Sinks;
using VRage.Plugins;

#if !DEV_BUILD
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
#endif

namespace SEProfiler
{
    // ReSharper disable once UnusedType.Global
    public class Plugin : IPlugin
    {
        public const string Name = "SEModProfiler";
        public static Plugin Instance { get; private set; }

        private Harmony          _harmony;
        private AggregateSink    _sink;
        private CommandListener  _commandListener;
        private ModResolver      _modResolver;
        private RuntimeEventListener _etw;
        private JsonlWriter      _jsonlWriter;

        private ProfilerSettingsGenerator _settingsGenerator;

        private string _currentModId;
        private string _currentOutputPath;
        private int    _tick;

        private const int JsonlFlushInterval = 60; // ~1s at 60 UPS

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Init(object gameInstance)
        {
            Instance = this;

            _harmony     = new Harmony(Name);
            _sink        = new AggregateSink();
            _modResolver = new ModResolver();
            _jsonlWriter = new JsonlWriter(_sink);
            _etw         = new RuntimeEventListener(_sink);

            _commandListener           = new CommandListener();
            _commandListener.OnScope   = HandleScope;
            _commandListener.OnUnscope = HandleUnscope;
            _commandListener.Start();

            // SettingsGenerator is created after CommandListener.Start() so the
            // watch directory (needed for Config path) already exists.
            _settingsGenerator = new ProfilerSettingsGenerator(_commandListener.WatchDir);

            // Patches are always applied; recording only happens when Sink is set.
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public void Update()
        {
            _tick++;

            if (_currentModId != null
                && HarmonyPatches.TargetAssembly == null
                && _modResolver.IsSessionReady)
            {
                HarmonyPatches.TargetAssembly = _modResolver.Resolve(_currentModId);
            }

            if (_currentOutputPath != null && _tick % JsonlFlushInterval == 0)
                _jsonlWriter.Flush();
        }

        public void Dispose()
        {
            // Do NOT call harmony.UnpatchAll() — it would break other loaded plugins.
            HandleUnscope();
            _etw?.Dispose();
            _commandListener?.Dispose();
            Instance = null;
        }

        // Called by Pulsar when the user clicks the config button in the plugin list.
        // ReSharper disable once UnusedMember.Global
        public void OpenConfigDialog()
        {
            MyGuiSandbox.AddScreen(_settingsGenerator.Dialog);
        }

        private void HandleScope(string modId, string outputPath)
        {
            HandleUnscope();

            _currentModId      = modId;
            _currentOutputPath = string.IsNullOrEmpty(outputPath)
                ? Path.Combine(_commandListener.WatchDir, "session")
                : outputPath;

            HarmonyPatches.TargetAssembly = null; // resolved lazily in Update()
            HarmonyPatches.Sink           = _sink;
            Profiler.Sink                 = _sink;

            _jsonlWriter.Open(_currentOutputPath);
        }

        private void HandleUnscope()
        {
            HarmonyPatches.Sink           = null;
            HarmonyPatches.TargetAssembly = null;
            Profiler.Sink                 = null;

            _jsonlWriter.Close();
            _currentModId      = null;
            _currentOutputPath = null;
        }
    }
}
