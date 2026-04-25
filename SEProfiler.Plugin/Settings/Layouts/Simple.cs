using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Graphics.GUI;
using SEProfiler.Settings.Elements;
using VRage.Utils;
using VRageMath;

namespace SEProfiler.Settings.Layouts
{
    internal class Simple : Layout
    {
        private MyGuiControlParent _parent;
        private MyGuiControlScrollablePanel _scrollPanel;

        public override Vector2 SettingsPanelSize { get { return new Vector2(0.5f, 0.7f); } }

        private const float ElementPadding = 0.01f;

        public Simple(Func<List<List<Control>>> getControls) : base(getControls) { }

        public override List<MyGuiControlBase> RecreateControls()
        {
            _parent = new MyGuiControlParent
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP,
                Position    = Vector2.Zero,
                Size        = new Vector2(SettingsPanelSize.X - 0.01f, SettingsPanelSize.Y - 0.09f),
            };

            _scrollPanel = new MyGuiControlScrollablePanel(_parent)
            {
                BackgroundTexture      = null,
                BorderHighlightEnabled = false,
                BorderEnabled          = false,
                OriginAlign            = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
                Position               = new Vector2(0f, 0.03f),
                Size                   = _parent.Size,
                ScrollbarVEnabled      = true,
                CanFocusChildren       = true,
                ScrolledAreaPadding    = new MyGuiBorderThickness(0.005f),
                DrawScrollBarSeparator = true,
            };

            foreach (var row in GetControls())
                foreach (var control in row)
                    _parent.Controls.Add(control.GuiControl);

            return new List<MyGuiControlBase> { _scrollPanel };
        }

        public override void LayoutControls()
        {
            var totalWidth = _scrollPanel.ScrolledAreaSize.X - 2 * ElementPadding;

            var controls    = GetControls();
            var totalHeight = ElementPadding + controls.Select(
                row => row.Max(c => c.GuiControl.Size.Y) + ElementPadding).Sum();

            _parent.Size = new Vector2(_parent.Size.X, totalHeight);

            var rowY = -0.5f * totalHeight + ElementPadding;
            foreach (var row in controls)
            {
                var rowHeight = row.Max(c => c.GuiControl.Size.Y);
                var controlY  = rowY + 0.5f * rowHeight;
                rowY += rowHeight + ElementPadding;

                var totalMinWidth  = row.Select(c => (c.FixedWidth ?? c.MinWidth) + c.RightMargin).Sum();
                var remainingWidth = Math.Max(0f, totalWidth - totalMinWidth);
                var sumFillFactors = row.Select(
                    c => c.FixedWidth.HasValue ? 0f : c.FillFactor ?? 0f).Sum();
                var unitWidth = sumFillFactors > 0f ? remainingWidth / sumFillFactors : 0f;

                var controlX = -0.5f * _parent.Size.X + ElementPadding;
                foreach (var control in row)
                {
                    var guiControl = control.GuiControl;
                    guiControl.Position    = new Vector2(controlX, controlY) + control.Offset;
                    guiControl.OriginAlign = control.OriginAlign;

                    var sizeY = guiControl.Size.Y;
                    if (control.FixedWidth.HasValue)
                    {
                        guiControl.Size = new Vector2(control.FixedWidth.Value, sizeY);
                        guiControl.SetMaxWidth(control.FixedWidth.Value);
                    }
                    else if (control.FillFactor.HasValue)
                    {
                        guiControl.Size = new Vector2(
                            Math.Max(control.MinWidth, unitWidth * control.FillFactor.Value), sizeY);
                    }
                    else
                    {
                        guiControl.Size = new Vector2(Math.Max(guiControl.Size.X, control.MinWidth), sizeY);
                    }

                    controlX += guiControl.Size.X + control.RightMargin;
                }
            }

            _scrollPanel.RefreshInternals();
        }
    }
}
