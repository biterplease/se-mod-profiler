# SEProfiler

A two-component profiling system for Space Engineers mods.

| Component | Purpose |
|---|---|
| **SEProfiler.Plugin** | Client plugin loaded by [Pulsar](https://github.com/SpaceGT/Pulsar). Observes a target mod at SE framework boundaries using Harmony patches. Outputs JSONL events. |
| **SEProfiler.Lib** | Zero-dependency library mods reference for internal instrumentation. All calls are no-ops when the plugin is absent. |

---

## Requirements

- Space Engineers (Steam)
- [Pulsar](https://github.com/SpaceGT/Pulsar) — the plugin loader that replaced the discontinued SpaceEngineersLauncher

---

## Installation

1. Build the solution in **Release** configuration.
2. Copy `SEModProfiler.dll` to Pulsar's plugin directory:
   ```
   %AppData%\Pulsar\Legacy\Local\
   ```
   The post-build `Deploy.bat` does this automatically on a successful build.
3. Launch Pulsar, enable **SEModProfiler** in the Plugins tab, and start SE.

The plugin is now active. It records nothing until a mod is scoped (see below).

---

## Selecting a Mod to Profile

Open the plugin config dialog from Pulsar's plugin list. The dialog shows all mods that
have called `Profiler.Register()` at runtime. Check the mod you want to profile and click
**Save** — the plugin starts recording immediately.

Alternatively, drop a `cmd.json` file into:
```
%APPDATA%\SpaceEngineers\SEModProfiler\
```

The directory is created automatically on first plugin load.

### Start profiling

```json
{
  "cmd": "scope",
  "modId": "2938471234",
  "outputPath": "%APPDATA%\\SpaceEngineers\\SEModProfiler\\session"
}
```

| Field | Description |
|---|---|
| `modId` | Steam Workshop ID of the target mod. Leave empty (`""`) to observe all mods. |
| `outputPath` | Base path for output files (without extension). Omit to use the default. |

### Stop profiling

```json
{ "cmd": "unscope" }
```

## Output Files

- `session.jsonl`: one JSON object per line for detailed tracing.

### JSONL format

Scope events carry `ms` (elapsed milliseconds) and `gc0_delta` (gen-0 collections during the call).

Example lines:

```json
{"t":1714000000123,"src":"framework","type":"scope","name":"UpdateAfterSimulation","ms":0.412,"gc0_delta":0}
{"t":1714000000124,"src":"etw","type":"event","name":"GCStart","data":"gen=0"}
{"t":1714000000125,"src":"mod","type":"counter","name":"tasks_dispatched","delta":1}
```

### Instrument code

```csharp
using SEProfiler;

// Time a block of code
using (Profiler.Scope("BuildQueue.Process"))
{
    // ... expensive work ...
}

// Increment a counter
Profiler.Counter("jobs_submitted");
Profiler.Counter("bytes_transferred", payload.Length);

// Set a gauge
Profiler.Gauge("queue_depth", _queue.Count);

// Fire a named event
Profiler.Event("phase_change", "idle→active");
```

All four methods are **unconditional no-ops** when the plugin is absent. The only overhead is a single null check on `Profiler.Sink`. No allocation occurs on the no-op path.

### Declare your mod as instrumented

Call `Profiler.Register` during your mod's `Init`. This makes the mod appear in the plugin's
config dialog so the user can select it without knowing the Steam Workshop ID.

```csharp
public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
{
    Profiler.Register("My Mod Display Name", ModContext.ModId);
}
```

`ModContext.ModId` is the Steam Workshop ID string. When the plugin is absent the call is a no-op.

### Conditional instrumentation in update loops

If you want to skip even the null check in a tight loop, use `Profiler.IsActive` — but check it **lazily** (inside your update method, not at `Init` time):

```csharp
// CORRECT — checked each frame; plugin may set Sink after Init
public override void UpdateAfterSimulation()
{
    if (Profiler.IsActive)
        Profiler.Gauge("entity_count", _entities.Count);
}

// WRONG — load order is not guaranteed; Sink may be null at Init time
public override void Init(MyObjectBuilder_EntityBase builder)
{
    _profilingEnabled = Profiler.IsActive; // may always be false
}
```

The plugin sets `Profiler.Sink` during its own `Init`. Because SE's plugin load order is not guaranteed, your mod's `Init` may run before the profiler's `Init`. Always read `Profiler.IsActive` at the call site.

---

## Output Mode Reference

| `cmd` | Required fields | Effect |
|---|---|---|
| `scope` | `modId`, `outputPath` | Start recording. Opens JSONL file. Resolves mod assembly lazily after world load. |
| `unscope` | — | Stop recording. Flushes and closes output files. |

---

## Building from Source

Prerequisites: .NET SDK, Visual Studio 2022 or Rider, Space Engineers (Steam).

If SE is not installed in the default Steam library path, create `user.props` in the repository root (it is gitignored):

```xml
<Project>
  <PropertyGroup>
    <Bin64>C:\YourSteamLibrary\steamapps\common\SpaceEngineers\Bin64</Bin64>
  </PropertyGroup>
</Project>
```

Then build:

```
dotnet build SEProfiler.sln -c Release
```

Run unit tests (no SE installation required at test runtime):

```
dotnet test SEProfiler.Tests --filter "TestCategory!=Integration"
```

Run the manual integration test suite after a live SE session:

```
set SEMOD_PROFILER_OUTPUT=%APPDATA%\SpaceEngineers\SEModProfiler
dotnet test SEProfiler.Tests --filter "TestCategory=Integration"
```

---

## Notes

- Harmony patches are applied once at `Init` and are never removed. When unscoped, patches are silent (`Sink == null` gates all recording). Calling `UnpatchAll` in `Dispose` is explicitly avoided to avoid breaking other loaded plugins.
- The profiler is passive — it does not modify any game logic, only wraps framework boundary methods.
- `SEProfiler.Lib.dll` carries no SE or Harmony dependencies and is safe to redistribute with a mod.
