// This is a helper class that helps get positions for stuff in chunks and in the world. | DA | 8/25/25
using OpenTK.Mathematics;
using VoxelGame.World;

namespace VoxelGame.Utils
{
    public static class WorldPositionHelper
    {
        public static ChunkPos WorldToChunkPos(Vector3i worldPos) => new ChunkPos(
                (int)Math.Floor(worldPos.X / (float)GameConstants.CHUNK_SIZE),
                (int)Math.Floor(worldPos.Z / (float)GameConstants.CHUNK_SIZE)
            );

        public static ChunkPos WorldToChunkPos(Vector3 worldPos) => new ChunkPos(
                (int)Math.Floor(worldPos.X / GameConstants.CHUNK_SIZE),
                (int)Math.Floor(worldPos.Z / GameConstants.CHUNK_SIZE)
            );

        public static Vector3i WorldToLocalPos(Vector3i worldPos, ChunkPos chunkPos)
        {
            Vector3i localPos = new Vector3i(
                worldPos.X - chunkPos.X * GameConstants.CHUNK_SIZE,
                worldPos.Y,
                worldPos.Z - chunkPos.Z * GameConstants.CHUNK_SIZE
            );

            // Handle negative coordinates
            if (localPos.X < 0) localPos.X += GameConstants.CHUNK_SIZE;
            if (localPos.Z < 0) localPos.Z += GameConstants.CHUNK_SIZE;

            return localPos;
        }

        public static Vector3i ChunkLocalToWorld(ChunkPos chunkPos, Vector3i localPos) => new Vector3i(
                chunkPos.X * GameConstants.CHUNK_SIZE + localPos.X,
                localPos.Y,
                chunkPos.Z * GameConstants.CHUNK_SIZE + localPos.Z
            );

        public static (Chunk? chunk, Vector3i localPos) GetChunkAndLocalPos(Vector3i worldPos, ChunkManager chunkManager)
        {
            ChunkPos chunkPos = WorldToChunkPos(worldPos);
            Chunk? chunk = chunkManager.GetChunk(chunkPos);

            if (chunk != null)
            {
                Vector3i localPos = WorldToLocalPos(worldPos, chunkPos);
                return (chunk, localPos);
            }

            return (null, Vector3i.Zero);
        }

        public static bool IsInChunkBounds(Vector3i localPos) => localPos.X >= 0 && localPos.X < GameConstants.CHUNK_SIZE && localPos.Y >= 0 && localPos.Y < GameConstants.CHUNK_HEIGHT && localPos.Z >= 0 && localPos.Z < GameConstants.CHUNK_SIZE;

        public static bool IsOnChunkBoundary(Vector3i localPos) => localPos.X == 0 || localPos.X == GameConstants.CHUNK_SIZE - 1 || localPos.Z == 0 || localPos.Z == GameConstants.CHUNK_SIZE - 1;
    }
}