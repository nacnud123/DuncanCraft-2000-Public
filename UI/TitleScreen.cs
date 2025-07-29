using ImGuiNET;
using System;
using System.Numerics;

namespace VoxelGame.UI
{
    public class TitleScreen
    {
        private string worldName = "";
        private readonly byte[] worldNameBuffer = new byte[256];

        public event Action<string> OnStartGame;
        public event Action OnTitleQuitGame;

        public string WorldName { get => worldName; set => worldName = value; }

        public void Render()
        {
            var io = ImGui.GetIO();
            var displaySize = io.DisplaySize;

            var scale = io.DisplayFramebufferScale;

            ImGui.SetNextWindowPos(Vector2.Zero);
            ImGui.SetNextWindowSize(displaySize);

            ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoTitleBar |
                                          ImGuiWindowFlags.NoResize |
                                          ImGuiWindowFlags.NoMove |
                                          ImGuiWindowFlags.NoCollapse |
                                          ImGuiWindowFlags.NoScrollbar |
                                          ImGuiWindowFlags.NoScrollWithMouse |
                                          ImGuiWindowFlags.NoBringToFrontOnFocus |
                                          ImGuiWindowFlags.NoFocusOnAppearing |
                                          ImGuiWindowFlags.NoBackground;

            ImGui.Begin("TitleScreen", windowFlags);

            var windowSize = ImGui.GetWindowSize();
            var contentWidth = 400f;

            var centerX = windowSize.X * 0.5f;
            var centerY = windowSize.Y * 0.5f;

            var titleText = "Duncan Craft 2000";
            var titleSize = ImGui.CalcTextSize(titleText);
            ImGui.SetCursorPos(new Vector2(centerX - titleSize.X * 0.5f, centerY - 120f));

            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.2f, 0.8f, 0.2f, 1.0f));
            ImGui.Text(titleText);
            ImGui.PopStyleColor();

            // World name label
            ImGui.SetCursorPos(new Vector2(centerX - contentWidth * 0.5f, centerY - 50f));
            ImGui.Text("World Name:");

            // World name input
            ImGui.SetCursorPos(new Vector2(centerX - contentWidth * 0.5f, centerY - 20f));
            ImGui.SetNextItemWidth(contentWidth);

            // Convert input
            var bytes = System.Text.Encoding.UTF8.GetBytes(worldName);
            Array.Clear(worldNameBuffer, 0, worldNameBuffer.Length);
            Array.Copy(bytes, worldNameBuffer, Math.Min(bytes.Length, worldNameBuffer.Length - 1));

            if (ImGui.InputText("##worldname", worldNameBuffer, (uint)worldNameBuffer.Length))
            {
                var nullIndex = Array.IndexOf(worldNameBuffer, (byte)0);
                if (nullIndex == -1) nullIndex = worldNameBuffer.Length;
                worldName = System.Text.Encoding.UTF8.GetString(worldNameBuffer, 0, nullIndex);
            }

            // Buttons
            var buttonWidth = 150f;
            var buttonHeight = 40f;
            var buttonSpacing = 20f;
            var totalButtonWidth = (buttonWidth * 2) + buttonSpacing;
            var buttonStartX = centerX - totalButtonWidth * 0.5f;
            var buttonY = centerY + 30f;

            // Start button
            ImGui.SetCursorPos(new Vector2(buttonStartX, buttonY));

            if (ImGui.Button("Start Game", new Vector2(buttonWidth, buttonHeight)))
            {
                if (worldName.Count() == 0)
                    worldName = "Voxel World";

                OnStartGame?.Invoke(worldName.Trim());
            }

            // Quit button
            ImGui.SetCursorPos(new Vector2(buttonStartX + buttonWidth + buttonSpacing, buttonY));
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.2f, 0.2f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.9f, 0.3f, 0.3f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.7f, 0.1f, 0.1f, 1.0f));

            if (ImGui.Button("Quit", new Vector2(buttonWidth, buttonHeight)))
            {
                OnTitleQuitGame?.Invoke();
            }

            ImGui.PopStyleColor(3);

            // Version info at bottom
            ImGui.SetCursorPos(new Vector2(10, windowSize.Y - 30));
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.6f, 0.6f, 0.6f, 1.0f));
            ImGui.Text("Version 1.2.1B");
            ImGui.PopStyleColor();

            ImGui.End();
        }
    }
}