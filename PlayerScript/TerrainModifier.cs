// Main terrain mod script, place and break blocks | DA | 9/4/25
using DuncanCraft.Utils;
using DuncanCraft.World;
using OpenTK.Mathematics;

namespace DuncanCraft.PlayerScript
{
    public class TerrainModifier
    {
        private readonly GameWorld _mWorld;

        public TerrainModifier(GameWorld world)
        {
            _mWorld = world;
        }

        public void BreakBlock(Vector3 cameraPosition, Vector3 cameraFront)
        {
            var raycast = Raycast.Cast(cameraPosition, cameraFront, _mWorld);
            if (raycast.HasValue && raycast.Value.Hit)
            {
                var hit = raycast.Value;
                _mWorld.SetBlock(hit.HitBlock.X, hit.HitBlock.Y, hit.HitBlock.Z, BlockIDs.Air);
            }
        }

        public void PlaceBlock(Vector3 cameraPosition, Vector3 cameraFront, byte blockType)
        {
            var raycast = Raycast.Cast(cameraPosition, cameraFront, _mWorld);
            if (raycast.HasValue && raycast.Value.Hit)
            {
                var hit = raycast.Value;
                
                var playerBlockPos = new Vector3i(
                    (int)Math.Floor(cameraPosition.X),
                    (int)Math.Floor(cameraPosition.Y),
                    (int)Math.Floor(cameraPosition.Z)
                );
                
                if (hit.PlaceBlock != playerBlockPos && 
                    hit.PlaceBlock != new Vector3i(playerBlockPos.X, playerBlockPos.Y - 1, playerBlockPos.Z))
                {
                    if (_mWorld.IsPositionValid(hit.PlaceBlock.X, hit.PlaceBlock.Y, hit.PlaceBlock.Z))
                    {
                        _mWorld.SetBlock(hit.PlaceBlock.X, hit.PlaceBlock.Y, hit.PlaceBlock.Z, blockType);
                    }
                }
            }
        }
    }
}