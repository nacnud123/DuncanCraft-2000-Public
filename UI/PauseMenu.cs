﻿// Makes the pause menu UI. | DA | 8/1/25
using ImGuiNET;
using System;
using System.Numerics;

namespace VoxelGame.UI
{
    public class PauseMenu
    {
        public event Action OnPauseQuitGame;
        public event Action OnResumeGame;

        public void Render()
        {
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

            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.0f, 0.0f, 0.0f, 0.5f));

            ImGui.Begin("PauseGame", windowFlags);

            var windowSize = ImGui.GetWindowSize();
            var contentWidth = 400f;

            var centerX = windowSize.X * 0.5f;
            var centerY = windowSize.Y * 0.5f;

            // Title
            var titleText = "Pause";
            ImGui.SetWindowFontScale(2.0f); // 2X size

            var titleSize = ImGui.CalcTextSize(titleText);
            ImGui.SetCursorPos(new Vector2(centerX - titleSize.X * 0.5f, centerY - 120f));
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.2f, 0.8f, 0.2f, 1.0f));
            ImGui.Text(titleText);
            ImGui.PopStyleColor();

            ImGui.SetWindowFontScale(1.0f); // Set back to default

            // Buttons
            var buttonWidth = 150f;
            var buttonHeight = 40f;
            var buttonSpacing = 20f;
            var totalButtonWidth = (buttonWidth * 2) + buttonSpacing;
            var buttonStartX = centerX - totalButtonWidth * 0.5f;
            var buttonY = centerY + 30f;

            // Resume button
            ImGui.SetCursorPos(new Vector2(buttonStartX, buttonY));

            if (ImGui.Button("Resume Game", new Vector2(buttonWidth, buttonHeight)))
            {
                OnResumeGame?.Invoke();
            }

            // Quit button
            ImGui.SetCursorPos(new Vector2(buttonStartX + buttonWidth + buttonSpacing, buttonY));
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.2f, 0.2f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.9f, 0.3f, 0.3f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.7f, 0.1f, 0.1f, 1.0f));

            if (ImGui.Button("Quit", new Vector2(buttonWidth, buttonHeight)))
            {
                OnPauseQuitGame?.Invoke();
            }

            ImGui.PopStyleColor(3);

            ImGui.End();

            ImGui.PopStyleColor();
        }
    }


}
