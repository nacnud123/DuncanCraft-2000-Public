using ImGuiNET;
using System;
using System.Numerics;
using VoxelGame.Utils;


namespace VoxelGame.UI
{
    public class LoadingScreen
    {
        public void Render()
        {
            long currentTick = VoxelGame.init._ChunkManager?.GetCurrentTick() ?? 0;

            var io = ImGui.GetIO();
            var displaySize = io.DisplaySize;

            ImGui.SetNextWindowPos(Vector2.Zero);
            ImGui.SetNextWindowSize(displaySize);

            ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoTitleBar |
                                             ImGuiWindowFlags.NoResize |
                                             ImGuiWindowFlags.NoMove |
                                             ImGuiWindowFlags.NoCollapse |
                                             ImGuiWindowFlags.NoScrollbar |
                                             ImGuiWindowFlags.NoScrollWithMouse |
                                             ImGuiWindowFlags.NoBringToFrontOnFocus |
                                             ImGuiWindowFlags.NoFocusOnAppearing;

            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.0f, 0.0f, 0.0f, .5f));

            ImGui.Begin("LoadingMenu", windowFlags);

            var windowSize = ImGui.GetWindowSize();
            var centerX = windowSize.X * 0.5f;
            var centerY = windowSize.Y * 0.5f;

            // Title
            var titleText = "Loading World";
            ImGui.SetWindowFontScale(2.0f);

            var titleSize = ImGui.CalcTextSize(titleText);
            ImGui.SetCursorPos(new Vector2(centerX - titleSize.X * 0.5f, centerY - 120f));
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.2f, 0.8f, 0.2f, 1.0f));
            ImGui.Text(titleText);
            ImGui.PopStyleColor();

            ImGui.SetWindowFontScale(1.0f);

            // Progress bar
            float progress = Math.Min(1.0f, (float)currentTick / GameConstants.LOADING_TICKS_REQUIRED);
            int progressPercent = (int)(progress * 100);

            string statusMessage = "Doing stuff";
            var statusSize = ImGui.CalcTextSize(statusMessage);
            ImGui.SetCursorPos(new Vector2(centerX - statusSize.X * 0.5f, centerY - 40f));
            ImGui.Text(statusMessage);

            // Progress bar
            ImGui.SetCursorPos(new Vector2(centerX - 200f, centerY));
            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, new Vector4(0.2f, 0.8f, 0.2f, 1.0f));
            ImGui.ProgressBar(progress, new Vector2(400f, 30f), $"{progressPercent}%");
            ImGui.PopStyleColor();

            ImGui.End();
            ImGui.PopStyleColor();
        }
    }
}
