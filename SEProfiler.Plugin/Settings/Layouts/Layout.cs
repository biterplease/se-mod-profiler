using System;
using System.Collections.Generic;
using Sandbox.Graphics.GUI;
using SEProfiler.Settings.Elements;
using VRageMath;

namespace SEProfiler.Settings.Layouts
{
    internal abstract class Layout
    {
        public abstract Vector2 SettingsPanelSize { get; }

        protected readonly Func<List<List<Control>>> GetControls;

        protected Layout(Func<List<List<Control>>> getControls)
        {
            GetControls = getControls;
        }

        public abstract List<MyGuiControlBase> RecreateControls();

        public abstract void LayoutControls();
    }
}
