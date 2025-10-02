// This script helps with managing the state of the game, handling stuff like changing "scenes", the esc key, inv key, and some helper functions | DA | 10/2/25
using VoxelGame.Utils;

namespace VoxelGame.Managers
{
    public class GameStateManager
    {

        public GameState CurrentState { get; private set; }
        public event Action<GameState, GameState> OnStateChanged;

        public GameStateManager()
        {
            CurrentState = GameState.TitleScreen;
        }

        public bool IsWorldLoading()
        {
            if (!IsInGame() || VoxelGame.init._ChunkManager == null)
                return false;

            return VoxelGame.init._ChunkManager.GetCurrentTick() < GameConstants.LOADING_TICKS_REQUIRED;
        }

        public bool IsWorldReady()
        {
            if (!IsInGame() || VoxelGame.init._ChunkManager == null)
                return false;

            return VoxelGame.init._ChunkManager.GetCurrentTick() >= GameConstants.LOADING_TICKS_REQUIRED;
        }

        public void ChangeState(GameState newState)
        {
            var oldState = CurrentState;
            CurrentState = newState;
            OnStateChanged?.Invoke(oldState, newState);
        }

        public bool HandleEscapeKey(VoxelGame game)
        {
            switch (CurrentState)
            {
                case GameState.TitleScreen:
                    game.Close();
                    return true;

                case GameState.Pause:
                case GameState.Inventory:
                    game.ResumeGame();
                    return true;

                case GameState.InGame:
                    if (IsWorldReady())
                        game.PauseGame();
                    return true;

                default:
                    return false;
            }
        }

        public bool HandleInventoryKey(VoxelGame game)
        {
            if (IsWorldLoading())
                return false;

            if (CurrentState == GameState.Pause)
                return false;

            if (CurrentState == GameState.Inventory)
            {
                game.ResumeGame();
            }
            else if (CurrentState == GameState.InGame)
            {
                game.OpenInventory();
            }

            return true;
        }

        public bool IsGameplayState() => CurrentState == GameState.InGame || CurrentState == GameState.Inventory || CurrentState == GameState.Pause;

        public bool IsInGame() => CurrentState == GameState.InGame;

        public bool ShouldUpdatePlayer() => CurrentState == GameState.InGame;

        public bool ShouldHandleInput() => IsGameplayState();
    }
}