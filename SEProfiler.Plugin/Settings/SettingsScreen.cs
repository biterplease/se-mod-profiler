using System;
using System.Collections.Generic;
using Sandbox;
using Sandbox.Graphics.GUI;
using VRageMath;

namespace SEProfiler.Settings
{
    internal class SettingsScreen : MyGuiScreenBase
    {
        public readonly string FriendlyName;
        public Func<List<MyGuiControlBase>> GetControls;

        public override string GetFriendlyName() { return FriendlyName; }

        public SettingsScreen(
            string friendlyName,
            Func<List<MyGuiControlBase>> getControls,
            Vector2? position = null,
            Vector2? size     = null)
            : base(
                position ?? new Vector2(0.5f, 0.5f),
                MyGuiConstants.SCREEN_BACKGROUND_COLOR,
                size ?? new Vector2(0.5f, 0.7f),
                false,
                null,
                MySandboxGame.Config.UIBkOpacity,
                MySandboxGame.Config.UIOpacity)
        {
            FriendlyName = friendlyName;
            GetControls  = getControls;

            EnabledBackgroundFade  = true;
            m_closeOnEsc           = true;
            m_drawEvenWithoutFocus = true;
            CanHideOthers          = true;
            CanBeHidden            = true;
            CloseButtonEnabled     = true;
        }

        public void UpdateSize(Vector2 screenSize)
        {
            Size = screenSize;
            CloseButtonEnabled = CloseButtonEnabled; // force close-button position update
        }

        public override void LoadContent()
        {
            base.LoadContent();
            RecreateControls(true);
        }

        public override void OnRemoved()
        {
            // Persist whatever was last committed via the Save button.
            ConfigStorage.Save(Config.Current);
            base.OnRemoved();
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);
            AddCaption(FriendlyName);

            foreach (var item in GetControls())
                Controls.Add(item);
        }
    }
}
