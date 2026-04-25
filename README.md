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

### Pulsar UI

You should be able to find the plugin in the in-game pulsar UI.

### Local

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
## Usage

### Declare your mod as instrumented

Any mod calling `Profiler.Register` will show up in the plugin's config dialog. It's recommended to call `Profiler.Register` on your mod's init.

```csharp
public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
{
    Profiler.Register("My Mod Display Name", ModContext.ModId);
}
```

`ModContext.ModId` is the Steam Workshop ID string. When the plugin is absent the call is a no-op.

### Instrument code

```csharp
using SEProfiler;
using Sandbox.ModAPI;
using VRage.Utils;
using VRage.Game.Components;

// Minimal SessionComponent-style example.
[MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
public sealed class ProfilerExampleSession : MySessionComponentBase
{
    public int MyModValue = 0;
    public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
    {
        // Optional but recommended: shows your mod in the profiler UI.
        Profiler.Register("Profiler Example Mod", ModContext.ModId);
    }

    [SEProfiler.Counter("ProfilerExample.Ticks")] // increment by 1
    [SEProfiler.Counter("ProfilerExample.Ticks", 10)] // increment by 10
    [SEProfiler.Scope("ProfilerExample.UpdateTick")] // scope output of entire function
    [SEProfiler.Gauge("profiler_example.fixed_budget_ms", 16.67)] // compile-time constant only
    [SEProfiler.Event("profiler_example.phase", "before_sim")] // compile-time constant only
    public override void UpdateBeforeSimulation()
    {
        RecordScopeExample();
        RecordCounterExample();
        RecordGaugeExample();
        RecordEventExample();
    }

    private void RecordScopeExample()
    {
        using (Profiler.Scope("ProfilerExample.UpdateTick"))
        {
            // Work you want to time.
        }
    }

    private void RecordCounterExample()
    {
        Profiler.Counter("profiler_example.MyModValue_tick", MyModValue);
        MyModValue++;
    }

    private void RecordGaugeExample()
    {
        // Runtime values (VRage/SE API state) cannot be used in attributes.
        Profiler.Gauge("profiler_example.players_online", MyAPIGateway.Multiplayer.PlayerCount);
    }

    private void RecordEventExample()
    {
        // Runtime payload from VRage should be sent explicitly.
        Profiler.Event("profiler_example.phase", MyGameTimer.SessionTimeSpan.TotalSeconds.ToString("F2"));
    }
}
```

Attribute arguments are limited to compile-time constants. In practice:

- Works in attributes: string literals, numeric literals, `const` values, `nameof(...)`, `typeof(...)`.
- Does not work in attributes: runtime values like `MyAPIGateway.Multiplayer.PlayerCount`, `DateTime.UtcNow`, non-const fields/properties, method calls.
- For runtime values from VRage/SE APIs, keep explicit `Profiler.*` calls inside the method body.

All four methods are **unconditional no-ops** when the plugin is absent. The only overhead is a single null check on `Profiler.Sink`. No allocation occurs on the no-op path.



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

### Strip instrumentation utility

The repository includes a small CLI project, `SEProfiler.InstrumentationStripper.Cli`, that removes profiler instrumentation from C# source files.

Default mode is preview-only (prints transformed source to stdout and does not modify files):

```
dotnet run --project SEProfiler.InstrumentationStripper.Cli -- strip-instrumentation path/to/mod
```

Overwrite files in place:

```
dotnet run --project SEProfiler.InstrumentationStripper.Cli -- --inplace path/to/mod
```

The tool strips:

- `[SEProfiler.Scope|Counter|Gauge|Event(...)]` (and unqualified forms)
- `Profiler.Counter(...)`, `Profiler.Gauge(...)`, `Profiler.Event(...)` statements
- `using (Profiler.Scope(...)) { ... }` wrappers (keeps inner body)
- `using var _ = Profiler.Scope(...);` statements

You can include the stripper in a .NET build/publish pipeline. For example, in a mod `.csproj` you can invoke it before your release publish target:

```xml
<Target Name="StripProfilerInstrumentationForRelease" BeforeTargets="Publish" Condition="'$(Configuration)' == 'Release'">
  <Exec Command="dotnet run --project ..\SEProfiler.InstrumentationStripper.Cli -- --inplace $(MSBuildProjectDirectory)\Data\Scripts" />
</Target>
```

In CI you can run the same command as a step before packaging artifacts.

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


## Contributing

- Submit an Issue/PR
- AI assisted/generated code is allowed, but try to keep the comments concise.