using OpenTK.Mathematics;
using VoxelGame.Utils;
using VoxelGame.Ticking;
using VoxelGame.World;

namespace VoxelGame.Blocks
{
    public class DirtBlock : IBlock, IRandomTickable
    {
        public int ID => 2;

        public TextureCoords TopTextureCoords => UVHelper.FromTileCoords(1, 1);

        public TextureCoords BottomTextureCoords => TopTextureCoords;
        public TextureCoords SideTextureCoords => TopTextureCoords;
        public bool IsSolid => true;

        public string Name => "Dirt";

        public TextureCoords InventoryCoords => TopTextureCoords;
        public bool GravityBlock => false;
        public BlockMaterial Material => BlockMaterial.Dirt;
        public bool Transparent => false;
        public byte LightLevel => 0;
        public byte LightOpacity => 15;

        public void OnRandomTick(Vector3i worldPos, WorldTickSystem tickSystem, ChunkManager chunkManager)
        {
            Vector3i[] adjacentPositions =
            [
                new Vector3i(worldPos.X + 1, worldPos.Y, worldPos.Z),
                new Vector3i(worldPos.X - 1, worldPos.Y, worldPos.Z),
                new Vector3i(worldPos.X, worldPos.Y, worldPos.Z + 1),
                new Vector3i(worldPos.X, worldPos.Y, worldPos.Z - 1)
            ];

            foreach (Vector3i adjacentPos in adjacentPositions)
            {
                ChunkPos chunkPos = new ChunkPos(
                    (int)System.Math.Floor(adjacentPos.X / (float)GameConstants.CHUNK_SIZE),
                    (int)System.Math.Floor(adjacentPos.Z / (float)GameConstants.CHUNK_SIZE)
                );

                if (chunkManager._Chunks.TryGetValue(chunkPos, out var chunk))
                {
                    Vector3i localPos = new Vector3i(
                        adjacentPos.X - chunkPos.X * GameConstants.CHUNK_SIZE,
                        adjacentPos.Y,
                        adjacentPos.Z - chunkPos.Z * GameConstants.CHUNK_SIZE
                    );

                    if (localPos.X < 0) localPos.X += GameConstants.CHUNK_SIZE;
                    if (localPos.Z < 0) localPos.Z += GameConstants.CHUNK_SIZE;

                    if (chunk.IsInBounds(localPos))
                    {
                        byte adjacentBlock = chunk.Voxels[localPos.X, localPos.Y, localPos.Z];
                        
                        if (adjacentBlock == BlockIDs.Grass)
                        {
                            tickSystem.QueueBlockUpdate(worldPos, BlockIDs.Grass);
                            return;
                        }
                    }
                }
            }
        }
    }
}
