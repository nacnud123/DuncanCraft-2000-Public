// Used for stuff like dirt to grow grass, add this to a block that can be effected by random ticks.
using OpenTK.Mathematics;
using VoxelGame.Ticking;
using VoxelGame.World;

namespace VoxelGame.Blocks
{
    public interface IRandomTickable
    {
        void OnRandomTick(Vector3i worldPos, WorldTickSystem tickSystem, ChunkManager chunkManager);
    }
}