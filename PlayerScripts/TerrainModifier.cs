using OpenTK.Mathematics;
using System;
using VoxelGame.Utils;
using VoxelGame.World;

namespace VoxelGame.PlayerScripts
{
    public class TerrainModifier
    {
        private ChunkManager chunkManager;

        private byte currentBlock = BlockIDs.Stone;

        private float reach = 5.0f;

        public float Reach { get => reach; set => reach = value; }
        public byte CurrentBlock { get => currentBlock; set => currentBlock = value; }

        public TerrainModifier(ChunkManager chunkManager)
        {
            this.chunkManager = chunkManager;
        }

        public bool BreakBlock(Vector3 playerPos, Vector3 lookDirection)
        {
            var hit = Raycast(playerPos, lookDirection);
            if (hit.HasValue)
            {
                return SetBlock(hit.Value.blockPos, BlockIDs.Air);
            }
            return false;
        }

        public bool PlaceBlock(Vector3 playerPos, Vector3 lookDirection, byte blockType)
        {
            var hit = Raycast(playerPos, lookDirection);
            if (hit.HasValue)
            {
                Vector3i placePos = hit.Value.blockPos + hit.Value.normal;

                // Don't place blocks inside the player
                if (!isPosInsidePlayer(placePos, playerPos))
                {
                    return SetBlock(placePos, currentBlock);
                }
            }
            return false;
        }

        public bool SetBlock(Vector3i worldPos, byte blockType)
        {
            if (worldPos.Y < 0 || worldPos.Y >= Constants.CHUNK_HEIGHT)
                return false;

            ChunkPos chunkPos = new ChunkPos(
                (int)Math.Floor(worldPos.X / (float)Constants.CHUNK_SIZE),
                (int)Math.Floor(worldPos.Z / (float)Constants.CHUNK_SIZE)
            );

            Chunk chunk = chunkManager.GetChunk(chunkPos);
            if (chunk == null)
                return false;

            Vector3i localPos = new Vector3i(
                worldPos.X - chunkPos.X * Constants.CHUNK_SIZE,
                worldPos.Y,
                worldPos.Z - chunkPos.Z * Constants.CHUNK_SIZE
            );

            if (!chunk.IsInBounds(localPos))
                return false;

            chunk.Voxels[localPos.X, localPos.Y, localPos.Z] = blockType;

            markChunkForUpdate(chunkPos);

            if (localPos.X == 0)
                markChunkForUpdate(new ChunkPos(chunkPos.X - 1, chunkPos.Z));
            if (localPos.X == Constants.CHUNK_SIZE - 1)
                markChunkForUpdate(new ChunkPos(chunkPos.X + 1, chunkPos.Z));
            if (localPos.Z == 0)
                markChunkForUpdate(new ChunkPos(chunkPos.X, chunkPos.Z - 1));
            if (localPos.Z == Constants.CHUNK_SIZE - 1)
                markChunkForUpdate(new ChunkPos(chunkPos.X, chunkPos.Z + 1));

            chunk.Modified = true;

            return true;
        }

        public byte GetBlock(Vector3i worldPos)
        {

            if (worldPos.Y < 0 || worldPos.Y >= Constants.CHUNK_HEIGHT)
                return BlockIDs.Air;


            ChunkPos chunkPos = new ChunkPos(
                (int)Math.Floor(worldPos.X / (float)Constants.CHUNK_SIZE),
                (int)Math.Floor(worldPos.Z / (float)Constants.CHUNK_SIZE)
            );


            Chunk chunk = chunkManager.GetChunk(chunkPos);
            if (chunk == null)
                return BlockIDs.Air;

            Vector3i localPos = new Vector3i(
                worldPos.X - chunkPos.X * Constants.CHUNK_SIZE,
                worldPos.Y,
                worldPos.Z - chunkPos.Z * Constants.CHUNK_SIZE
            );

            if (chunk.IsInBounds(localPos))
                return chunk.GetBlock(localPos);

            return BlockIDs.Air;
        }

        private (Vector3i blockPos, Vector3i normal)? Raycast(Vector3 origin, Vector3 direction)
        {
            direction = Vector3.Normalize(direction);

            float stepSize = 0.1f;
            Vector3 step = direction * stepSize;
            Vector3 currentPos = origin;

            Vector3i lastBlockPos = new Vector3i(
                (int)Math.Floor(currentPos.X),
                (int)Math.Floor(currentPos.Y),
                (int)Math.Floor(currentPos.Z)
            );

            for (float distance = 0; distance < reach; distance += stepSize)
            {
                currentPos += step;

                Vector3i blockPos = new Vector3i(
                    (int)Math.Floor(currentPos.X),
                    (int)Math.Floor(currentPos.Y),
                    (int)Math.Floor(currentPos.Z)
                );

                if (GetBlock(blockPos) != BlockIDs.Air)
                {
                    Vector3i normal = calculateHitNormal(lastBlockPos, blockPos);
                    return (blockPos, normal);
                }

                lastBlockPos = blockPos;
            }

            return null;
        }

        private Vector3i calculateHitNormal(Vector3i fromPos, Vector3i toPos)
        {
            Vector3i diff = fromPos - toPos;

            // largest component as the normal
            if (Math.Abs(diff.X) > Math.Abs(diff.Y) && Math.Abs(diff.X) > Math.Abs(diff.Z))
                return new Vector3i(Math.Sign(diff.X), 0, 0);
            else if (Math.Abs(diff.Y) > Math.Abs(diff.Z))
                return new Vector3i(0, Math.Sign(diff.Y), 0);
            else
                return new Vector3i(0, 0, Math.Sign(diff.Z));
        }

        private bool isPosInsidePlayer(Vector3i blockPos, Vector3 playerPos)
        {
            Vector3 playerSize = new Vector3(0.6f, 1.8f, 0.6f);
            Vector3 halfSize = playerSize * 0.5f;

            Vector3 playerMin = playerPos - halfSize;
            Vector3 playerMax = playerPos + halfSize;

            Vector3 blockMin = new Vector3(blockPos.X, blockPos.Y, blockPos.Z);
            Vector3 blockMax = blockMin + Vector3.One;

            // Does block overlaps with the player's bounding box
            return blockMax.X > playerMin.X && blockMin.X < playerMax.X &&
                   blockMax.Y > playerMin.Y && blockMin.Y < playerMax.Y &&
                   blockMax.Z > playerMin.Z && blockMin.Z < playerMax.Z;
        }

        private void markChunkForUpdate(ChunkPos chunkPos)
        {
            Chunk chunk = chunkManager.GetChunk(chunkPos);
            if (chunk != null)
            {
                chunk.MeshGenerated = false;
            }
        }

        public (Vector3i blockPos, byte blockType)? getTargetBlock(Vector3 playerPos, Vector3 lookDirection)
        {
            var hit = Raycast(playerPos, lookDirection);
            if (hit.HasValue)
            {
                byte blockType = GetBlock(hit.Value.blockPos);
                return (hit.Value.blockPos, blockType);
            }
            return null;
        }

        public void SetCurrentBlockByNum(int blockPos)
        {
            VoxelGame.init._hotbar.SetHotbarSlot(blockPos);

            currentBlock = (byte)VoxelGame.init._hotbar.GetSelectedBlock().ID;

            VoxelGame.init.UIText = $"Current Block: {BlockRegistry.GetBlock(currentBlock).Name}";
        }

        public void SetCurrentBlock(int modifier)
        {
            currentBlock = (byte)VoxelGame.init._hotbar.MoveHotbarSlot((modifier));

            VoxelGame.init.UIText = $"Current Block: {BlockRegistry.GetBlock(currentBlock).Name}";
            
        }
    }
}
