// This script handles stuff related to the title screen. | DA | 8/25/25
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
        private readonly byte[] mWorldNameBuffer = new byte[256];
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

            var bytes = System.Text.Encoding.UTF8.GetBytes(mWorldName);
            Array.Clear(mWorldNameBuffer, 0, mWorldNameBuffer.Length);
            Array.Copy(bytes, mWorldNameBuffer, Math.Min(bytes.Length, mWorldNameBuffer.Length - 1));

            if (ImGui.InputText("##worldname", mWorldNameBuffer, (uint)mWorldNameBuffer.Length))
            {
                var nullIndex = Array.IndexOf(mWorldNameBuffer, (byte)0);
                if (nullIndex == -1) nullIndex = mWorldNameBuffer.Length;
                mWorldName = System.Text.Encoding.UTF8.GetString(mWorldNameBuffer, 0, nullIndex);
            }

            // Buttons
            var buttonWidth = 120f;
            var buttonHeight = 40f;
            var buttonSpacing = 15f;
            var buttonY = centerY + 150f;
            var totalButtonsWidth = (buttonWidth * 4) + (buttonSpacing * 3);
            var startX = centerX - (totalButtonsWidth * 0.5f);

            // Play Selected button
            ImGui.SetCursorPos(new Vector2(startX, buttonY));
            if (ImGui.Button("Play Selected", new Vector2(buttonWidth, buttonHeight)))
            {
                if (mSelectedWorld >= 0 && mSelectedWorld < mAvailableWorlds.Count)
                {
                    OnStartGame?.Invoke(mAvailableWorlds[mSelectedWorld]);
                }
            }

            // Create New button
            ImGui.SetCursorPos(new Vector2(startX + buttonWidth + buttonSpacing, buttonY));
            if (ImGui.Button("Create New", new Vector2(buttonWidth, buttonHeight)))
            {
                if (!string.IsNullOrWhiteSpace(mWorldName))
                {
                    OnStartGame?.Invoke(mWorldName.Trim());
                }
            }

            // Delete Selected button
            ImGui.SetCursorPos(new Vector2(startX + (buttonWidth + buttonSpacing) * 2, buttonY));
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.4f, 0.2f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.9f, 0.5f, 0.3f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.7f, 0.3f, 0.1f, 1.0f));

            bool canDelete = mSelectedWorld >= 0 && mSelectedWorld < mAvailableWorlds.Count;
            if (!canDelete)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
            }

            if (ImGui.Button("Delete Selected", new Vector2(buttonWidth, buttonHeight)) && canDelete)
            {
                string worldToDelete = mAvailableWorlds[mSelectedWorld];
                DeleteWorld(worldToDelete);
            }

            if (!canDelete)
            {
                ImGui.PopStyleVar();
            }

            ImGui.PopStyleColor(3);

            // Quit button
            ImGui.SetCursorPos(new Vector2(startX + (buttonWidth + buttonSpacing) * 3, buttonY));
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.2f, 0.2f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.9f, 0.3f, 0.3f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.7f, 0.1f, 0.1f, 1.0f));

            if (ImGui.Button("Quit", new Vector2(buttonWidth, buttonHeight)))
            {
                OnTitleQuitGame?.Invoke();
            }

            ImGui.PopStyleColor(3);

            // Version info
            ImGui.SetCursorPos(new Vector2(10, windowSize.Y - 50));
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.6f, 0.6f, 0.6f, 1.0f));
            ImGui.Text("Version 1.5.0B");
            ImGui.SetCursorPos(new Vector2(10, windowSize.Y - 30));
            ImGui.Text("Press DEL to delete currently selected world");
            ImGui.PopStyleColor();

            ImGui.End();
        }

        private void DeleteWorld(string worldName)
        {
            try
            {
                string worldPath = Path.Combine(GameConstants.SAVE_LOCATION, worldName);
                if (Directory.Exists(worldPath))
                {
                    Directory.Delete(worldPath, true);
                    Console.WriteLine($"Successfully deleted world: {worldName}");
                }
                else
                {
                    Console.WriteLine($"World directory not found: {worldPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting world '{worldName}': {ex.Message}");
            }
            finally
            {
                RefreshWorldList();
                mSelectedWorld = -1;
            }
        }

        public void RefreshWorldList()
        {
            mAvailableWorlds.Clear();

            foreach (var world in Serialization.GetAllWorlds())
            {
                mAvailableWorlds.Add(world.WorldName);
            }
        }

        public void HandleDeleteKey()
        {
            if (mSelectedWorld >= 0 && mSelectedWorld < mAvailableWorlds.Count)
            {
                string worldToDelete = mAvailableWorlds[mSelectedWorld];
                DeleteWorld(worldToDelete);
            }
        }
    }
}