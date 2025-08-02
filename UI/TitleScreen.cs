// Makes the title screen. | DA | 8/1/25
using ImGuiNET;
using System.Numerics;
using VoxelGame.Saving;
using VoxelGame.Utils;

namespace VoxelGame.UI
{
    public class TitleScreen
    {
        private string mWorldName = "";
        private int mSelectedWorld = -1;
        private bool mWorldsLoaded = false;

        private readonly byte[] _mWorldNameBuffer = new byte[256];
        private List<string> mAvailableWorlds = new List<string>();

        public event Action<string> OnStartGame;
        public event Action OnTitleQuitGame;

        public string WorldName { get => mWorldName; set => mWorldName = value; }

        public void Render()
        {
            if (!mWorldsLoaded)
            {
                RefreshWorldList();
                mWorldsLoaded = true;
            }

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
                                          ImGuiWindowFlags.NoFocusOnAppearing |
                                          ImGuiWindowFlags.NoBackground;

            ImGui.Begin("TitleScreen", windowFlags);

            var windowSize = ImGui.GetWindowSize();
            var centerX = windowSize.X * 0.5f;
            var centerY = windowSize.Y * 0.5f;

            // Title
            var titleText = "Duncan Craft 2000";
            var titleSize = ImGui.CalcTextSize(titleText);
            ImGui.SetCursorPos(new Vector2(centerX - titleSize.X * 0.5f, centerY - 150f));
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.2f, 0.8f, 0.2f, 1.0f));
            ImGui.Text(titleText);
            ImGui.PopStyleColor();

            // World list
            ImGui.SetCursorPos(new Vector2(centerX - 200f, centerY - 80f));
            ImGui.Text("Select a world:");

            ImGui.SetCursorPos(new Vector2(centerX - 200f, centerY - 50f));
            ImGui.BeginChild("WorldList", new Vector2(400f, 100f), ImGuiChildFlags.None);

            for (int i = 0; i < mAvailableWorlds.Count; i++)
            {
                if (ImGui.Selectable(mAvailableWorlds[i], mSelectedWorld == i))
                {
                    mSelectedWorld = i;
                }
            }

            ImGui.EndChild();

            // World name input
            ImGui.SetCursorPos(new Vector2(centerX - 200f, centerY + 70f));
            ImGui.Text("New World Name:");

            ImGui.SetCursorPos(new Vector2(centerX - 200f, centerY + 100f));
            ImGui.SetNextItemWidth(400f);

            // Convert input
            var bytes = System.Text.Encoding.UTF8.GetBytes(mWorldName);
            Array.Clear(_mWorldNameBuffer, 0, _mWorldNameBuffer.Length);
            Array.Copy(bytes, _mWorldNameBuffer, Math.Min(bytes.Length, _mWorldNameBuffer.Length - 1));

            if (ImGui.InputText("##worldname", _mWorldNameBuffer, (uint)_mWorldNameBuffer.Length))
            {
                var nullIndex = Array.IndexOf(_mWorldNameBuffer, (byte)0);
                if (nullIndex == -1) nullIndex = _mWorldNameBuffer.Length;
                mWorldName = System.Text.Encoding.UTF8.GetString(_mWorldNameBuffer, 0, nullIndex);
            }

            // Buttons
            var buttonWidth = 120f;
            var buttonHeight = 40f;
            var buttonSpacing = 20f;
            var buttonY = centerY + 150f;

            // Play Selected button
            ImGui.SetCursorPos(new Vector2(centerX - (buttonWidth * 1.5f + buttonSpacing), buttonY));
            if (ImGui.Button("Play Selected", new Vector2(buttonWidth, buttonHeight)))
            {
                if (mSelectedWorld >= 0 && mSelectedWorld < mAvailableWorlds.Count)
                {
                    OnStartGame?.Invoke(mAvailableWorlds[mSelectedWorld]);
                }
            }

            // Create New button
            ImGui.SetCursorPos(new Vector2(centerX - buttonWidth * 0.5f, buttonY));
            if (ImGui.Button("Create New", new Vector2(buttonWidth, buttonHeight)))
            {
                if (!string.IsNullOrWhiteSpace(mWorldName))
                {
                    OnStartGame?.Invoke(mWorldName.Trim());
                }
            }

            // Quit button
            ImGui.SetCursorPos(new Vector2(centerX + buttonWidth * 0.5f + buttonSpacing, buttonY));
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.2f, 0.2f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.9f, 0.3f, 0.3f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.7f, 0.1f, 0.1f, 1.0f));

            if (ImGui.Button("Quit", new Vector2(buttonWidth, buttonHeight)))
            {
                OnTitleQuitGame?.Invoke();
            }

            ImGui.PopStyleColor(3);

            // Version info
            ImGui.SetCursorPos(new Vector2(10, windowSize.Y - 30));
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.6f, 0.6f, 0.6f, 1.0f));
            ImGui.Text("Version 1.2.6B");
            ImGui.PopStyleColor();

            ImGui.End();
        }

        public void RefreshWorldList()
        {
            mAvailableWorlds.Clear();

            foreach (var world in Serialization.GetAllWorlds())
            {
                mAvailableWorlds.Add(world.WorldName);
            }
        }
    }
}