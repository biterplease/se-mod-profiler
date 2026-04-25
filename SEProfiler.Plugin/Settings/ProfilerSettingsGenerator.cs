using System.Collections.Generic;
using System.IO;
using System.Text;
using Sandbox.Graphics.GUI;
using SEProfiler.Settings.Elements;
using SEProfiler.Settings.Layouts;
using VRage.Utils;

namespace SEProfiler.Settings
{
    internal class ProfilerSettingsGenerator
    {
        private readonly string _watchDir;
        private readonly Simple _layout;

        private List<List<Control>> _controls;

        // Local dialog state — committed to Config only when Save is clicked.
        private string _pendingModId;

        // Parallel lists built during BuildControls; used by WireExclusivity.
        private MyGuiControlCheckbox[] _checkboxes;
        private string[]               _modIds;

        // Guard against re-entrant IsCheckedChanged callbacks.
        private bool _radioUpdating;

        public SettingsScreen Dialog { get; private set; }

        public ProfilerSettingsGenerator(string watchDir)
        {
            _watchDir = watchDir;
            _layout   = new Simple(() => _controls);
            Dialog    = new SettingsScreen(
                "SE Mod Profiler",
                OnRecreateControls,
                size: _layout.SettingsPanelSize);
        }

        // ── Screen callback ──────────────────────────────────────────────────

        private List<MyGuiControlBase> OnRecreateControls()
        {
            BuildControls();
            var result = _layout.RecreateControls();
            _layout.LayoutControls();
            return result;
        }

        // ── Control construction ─────────────────────────────────────────────

        private void BuildControls()
        {
            _controls = new List<List<Control>>();

            // Section header
            _controls.Add(new Separator("Instrumented Mods").GetControls());

            // Initialise pending state from last committed selection.
            _pendingModId = Config.Current.SelectedModId ?? "";

            // Collect all entries: Default Framework first, then registered mods.
            var modIds   = new List<string>();
            var modNames = new List<string>();

            modIds.Add("");
            modNames.Add("Default Framework  (observe all mods)");

            foreach (var kv in Profiler.GetRegisteredModsSnapshot())
            {
                modIds.Add(kv.Key);
                modNames.Add(kv.Value);
            }

            _checkboxes = new MyGuiControlCheckbox[modIds.Count];
            _modIds     = modIds.ToArray();

            for (int i = 0; i < modIds.Count; i++)
            {
                bool isChecked = modIds[i] == _pendingModId
                    || (string.IsNullOrEmpty(modIds[i]) && string.IsNullOrEmpty(_pendingModId));

                var cb = new MyGuiControlCheckbox { IsChecked = isChecked };
                var lbl = new MyGuiControlLabel(text: "Export data for:  " + modNames[i]);

                _controls.Add(new List<Control>
                {
                    new Control(cb, fixedWidth: 0.04f),
                    new Control(lbl, fillFactor: 1f),
                });

                _checkboxes[i] = cb;
            }

            WireExclusivity();

            // Save button — aligned right of label column, same as template's Button element.
            var saveBtn = new MyGuiControlButton(text: new StringBuilder("Save"));
            saveBtn.ButtonClicked += OnSave;
            _controls.Add(new List<Control>
            {
                new Control(new MyGuiControlLabel(text: ""), fixedWidth: Control.LabelMinWidth),
                new Control(saveBtn, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP),
            });
        }

        // ── Radio-button exclusivity ─────────────────────────────────────────

        private void WireExclusivity()
        {
            // Capture arrays by reference so closures share the same instances.
            var cbs  = _checkboxes;
            var ids  = _modIds;

            for (int i = 0; i < cbs.Length; i++)
            {
                int   capturedIdx = i;
                string capturedId = ids[i];

                cbs[i].IsCheckedChanged = (cb) =>
                {
                    if (_radioUpdating) return;
                    _radioUpdating = true;

                    if (cb.IsChecked)
                    {
                        // Update local pending state only — Config is not touched yet.
                        _pendingModId = capturedId;

                        for (int j = 0; j < cbs.Length; j++)
                            if (j != capturedIdx)
                                cbs[j].IsChecked = false;
                    }
                    else
                    {
                        // Prevent deselection: radio buttons must always have a selection.
                        cb.IsChecked = true;
                    }

                    _radioUpdating = false;
                };
            }
        }

        // ── Save ─────────────────────────────────────────────────────────────

        private void OnSave(MyGuiControlButton _)
        {
            // Commit pending selection to config and persist.
            Config.Current.SelectedModId = _pendingModId ?? "";
            ConfigStorage.Save(Config.Current);

            // Write cmd.json — empty modId means "observe all mods" (Default Framework).
            string modId = Config.Current.SelectedModId;
            string json  = string.Format(
                "{{\"cmd\":\"scope\",\"modId\":\"{0}\",\"outputPath\":\"\"}}",
                modId);

            File.WriteAllText(Path.Combine(_watchDir, "cmd.json"), json);
            Dialog.CloseScreen();
        }
    }
}
