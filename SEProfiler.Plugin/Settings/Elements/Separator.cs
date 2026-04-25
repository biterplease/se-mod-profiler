using System.Collections.Generic;
using Sandbox.Graphics.GUI;
using VRageMath;

namespace SEProfiler.Settings.Elements
{
    internal class Separator
    {
        public readonly string Caption;

        public Separator(string caption = null)
        {
            Caption = caption;
        }

        public List<Control> GetControls()
        {
            var label = new MyGuiControlLabel(text: Caption ?? "")
            {
                ColorMask = Color.Orange,
            };

            var lineColor = Color.LightCyan;
            lineColor.A = 0x22;

            var line = new MyGuiControlLabel
            {
                Size          = new Vector2(0.5f, 0f),
                BorderEnabled = true,
                BorderSize    = 1,
                BorderColor   = lineColor,
            };

            return new List<Control>
            {
                new Control(label, rightMargin: 0.005f),
                new Control(line, fillFactor: 1f, offset: new Vector2(0f, 0.003f)),
            };
        }
    }
}
