// TODO: Get rid of, using ImGuiNET is probably a better idea than this mess.

using ImGuiNET;
using System;
using System.Numerics;

namespace VoxelGame.UI
{
    public class GameUI
    {
        public void Render(string UIText)
        {
            var io = ImGui.GetIO();
            var displaySize = io.DisplaySize;

            var drawList = ImGui.GetForegroundDrawList();
            var textPos = new Vector2(50, 50);
            var textColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
            var shadowColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 0.0f, 0.0f, 0.8f));

            drawList.AddText(new Vector2(textPos.X + 1, textPos.Y + 1), shadowColor, UIText);
            drawList.AddText(textPos, textColor, UIText);

            var center = new Vector2(displaySize.X / 2.0f, displaySize.Y / 2.0f);
            var crosshairSize = 10.0f;
            var crosshairThickness = 3.0f;
            var crosshairColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 1.0f, 0.8f));

            var outlineColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 0.0f, 0.0f, 0.5f));

            drawList.AddLine(
                new Vector2(center.X - crosshairSize, center.Y),
                new Vector2(center.X + crosshairSize, center.Y),
                outlineColor, crosshairThickness + 2);
            drawList.AddLine(
                new Vector2(center.X, center.Y - crosshairSize),
                new Vector2(center.X, center.Y + crosshairSize),
                outlineColor, crosshairThickness + 2);

            // Main crosshair
            drawList.AddLine(
                new Vector2(center.X - crosshairSize, center.Y),
                new Vector2(center.X + crosshairSize, center.Y),
                crosshairColor, crosshairThickness);
            drawList.AddLine(
                new Vector2(center.X, center.Y - crosshairSize),
                new Vector2(center.X, center.Y + crosshairSize),
                crosshairColor, crosshairThickness);
        }

        public void Dispose()
        {
        }
    }
}