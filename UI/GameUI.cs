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

            ImGui.SetNextWindowPos(Vector2.Zero);
            ImGui.SetNextWindowSize(displaySize);
            ImGui.SetNextWindowBgAlpha(0.0f);
            
            ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoTitleBar |
                                           ImGuiWindowFlags.NoResize |
                                           ImGuiWindowFlags.NoMove |
                                           ImGuiWindowFlags.NoCollapse |
                                           ImGuiWindowFlags.NoScrollbar |
                                           ImGuiWindowFlags.NoScrollWithMouse |
                                           ImGuiWindowFlags.NoBringToFrontOnFocus |
                                           ImGuiWindowFlags.NoFocusOnAppearing |
                                           ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoMouseInputs;
            
            if (ImGui.Begin("GameUI", windowFlags))
            {
                // World and block info
                ImGui.TextColored(new System.Numerics.Vector4(1.0f, 1.0f, 1.0f, 1.0f), 
                    UIText);
            }
            
            ImGui.End();
            
            var drawList = ImGui.GetForegroundDrawList();
            var center = new System.Numerics.Vector2(displaySize.X / 2.0f, displaySize.Y / 2.0f);
            var crosshairSize = 5.0f;
            var crosshairColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(1.0f, 1.0f, 1.0f, 1.0f));
            
            // Draw crosshair lines
            drawList.AddLine(
                new System.Numerics.Vector2(center.X - crosshairSize, center.Y),
                new System.Numerics.Vector2(center.X + crosshairSize, center.Y),
                crosshairColor, 2.0f);
            drawList.AddLine(
                new System.Numerics.Vector2(center.X, center.Y - crosshairSize),
                new System.Numerics.Vector2(center.X, center.Y + crosshairSize),
                crosshairColor, 2.0f);
        }
        
        
        public void Dispose()
        {
        }
    }
}