// Makes the in-game UI. Draws the text in the top left and a crosshair. | DA | 8/1/25
using ImGuiNET;
using System;
using System.Numerics;

namespace VoxelGame.UI
{
    public class GameUI
    {
        // Text properties
        private uint mTextColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
        private uint mShadowColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 0.0f, 0.0f, 0.8f));
        private Vector2 mTextPos = new Vector2(50, 50);
        // ---------

        // Crosshair properties
        private float mCrosshairSize = 10.0f;
        private float mCrosshairThickness = 3.0f;
        private uint mCrosshairColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 1.0f, 0.8f));
        private uint mOutlineColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 0.0f, 0.0f, 0.5f));
        // ---------

        public void Render(string UIText)
        {
            var io = ImGui.GetIO();
            var displaySize = io.DisplaySize;

            var drawList = ImGui.GetForegroundDrawList();

            drawList.AddText(new Vector2(mTextPos.X + 1, mTextPos.Y + 1), mShadowColor, UIText);
            drawList.AddText(mTextPos, mTextColor, UIText);

            var center = new Vector2(displaySize.X / 2.0f, displaySize.Y / 2.0f);

            // Crosshair outline
            drawList.AddLine(
                new Vector2(center.X - mCrosshairSize, center.Y),
                new Vector2(center.X + mCrosshairSize, center.Y),
                mOutlineColor, mCrosshairThickness + 2);
            drawList.AddLine(
                new Vector2(center.X, center.Y - mCrosshairSize),
                new Vector2(center.X, center.Y + mCrosshairSize),
                mOutlineColor, mCrosshairThickness + 2);

            // Main crosshair
            drawList.AddLine(
                new Vector2(center.X - mCrosshairSize, center.Y),
                new Vector2(center.X + mCrosshairSize, center.Y),
                mCrosshairColor, mCrosshairThickness);
            drawList.AddLine(
                new Vector2(center.X, center.Y - mCrosshairSize),
                new Vector2(center.X, center.Y + mCrosshairSize),
                mCrosshairColor, mCrosshairThickness);
        }

        public void Dispose()
        {
        }
    }
}