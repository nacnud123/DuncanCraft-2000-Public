﻿// Main terrain modification script. Allows the player to break and place blocks in chunks. When that is done it tells the chunks to regen | DA | 8/1/25
using OpenTK.Mathematics;
using System;
using VoxelGame.Utils;
using VoxelGame.World;

namespace VoxelGame.PlayerScripts
{
    public class TerrainModifier
    {
        private ChunkManager mChunkManager;

        private byte mCurrentBlock = BlockIDs.Stone;
        private float mReach = 5.0f;

        public byte CurrentBlock { get => mCurrentBlock; set => mCurrentBlock = value; }

        public TerrainModifier(ChunkManager chunkManager)
        {
            this.mChunkManager = chunkManager;
        }

        public bool BreakBlock(Vector3 playerPos, Vector3 lookDirection)
        {
            var hit = raycastDDA(playerPos, lookDirection);
            if (hit.HasValue)
            {
                return SetBlock(hit.Value.blockPos, BlockIDs.Air, true);
            }
            return false;
        }

        public bool PlaceBlock(Vector3 playerPos, Vector3 lookDirection)
        {
            var hit = raycastDDA(playerPos, lookDirection);
            if (hit.HasValue)
            {
                Vector3i hitPos = hit.Value.blockPos;
                Vector3i normal = hit.Value.normal;
                Vector3i placePos = hitPos + normal;

                if (placePos == hitPos)
                {
                    Console.WriteLine("ERROR: Place position same as hit position!");
                    return false;
                }
                if (!isPosInsidePlayer(placePos, playerPos))
                {
                    if (GetBlock(placePos) != BlockIDs.Air)
                    {
                        return false;
                    }

                    return SetBlock(placePos, mCurrentBlock);
                }
            }
            return false;
        }

        // New raycast using DDA. Not sure what all the math does, had to look it up.
        private (Vector3i blockPos, Vector3i normal)? raycastDDA(Vector3 origin, Vector3 direction)
        {
            direction = Vector3.Normalize(direction);

            Vector3 pos = origin + direction * 0.001f;

            Vector3i mapPos = new Vector3i(
                (int)Math.Floor(pos.X),
                (int)Math.Floor(pos.Y),
                (int)Math.Floor(pos.Z)
            );

            Vector3 deltaDist = new Vector3(
                Math.Abs(direction.X) < 1e-6f ? 1e6f : Math.Abs(1.0f / direction.X),
                Math.Abs(direction.Y) < 1e-6f ? 1e6f : Math.Abs(1.0f / direction.Y),
                Math.Abs(direction.Z) < 1e-6f ? 1e6f : Math.Abs(1.0f / direction.Z)
            );

            Vector3i step = new Vector3i();
            Vector3 sideDist = new Vector3();

            // X direction
            if (direction.X < 0)
            {
                step.X = -1;
                sideDist.X = (pos.X - mapPos.X) * deltaDist.X;
            }
            else
            {
                step.X = 1;
                sideDist.X = (mapPos.X + 1.0f - pos.X) * deltaDist.X;
            }

            // Y direction
            if (direction.Y < 0)
            {
                step.Y = -1;
                sideDist.Y = (pos.Y - mapPos.Y) * deltaDist.Y;
            }
            else
            {
                step.Y = 1;
                sideDist.Y = (mapPos.Y + 1.0f - pos.Y) * deltaDist.Y;
            }

            // Z direction
            if (direction.Z < 0)
            {
                step.Z = -1;
                sideDist.Z = (pos.Z - mapPos.Z) * deltaDist.Z;
            }
            else
            {
                step.Z = 1;
                sideDist.Z = (mapPos.Z + 1.0f - pos.Z) * deltaDist.Z;
            }

            bool hit = false;
            int side = 0;
            float distance = 0;

            while (!hit && distance < mReach)
            {
                if (sideDist.X < sideDist.Y && sideDist.X < sideDist.Z)
                {
                    sideDist.X += deltaDist.X;
                    mapPos.X += step.X;
                    side = 0;
                    distance = sideDist.X - deltaDist.X;
                }
                else if (sideDist.Y < sideDist.Z)
                {
                    sideDist.Y += deltaDist.Y;
                    mapPos.Y += step.Y;
                    side = 1;
                    distance = sideDist.Y - deltaDist.Y;
                }
                else
                {
                    sideDist.Z += deltaDist.Z;
                    mapPos.Z += step.Z;
                    side = 2;
                    distance = sideDist.Z - deltaDist.Z;
                }

                if (GetBlock(mapPos) != BlockIDs.Air)
                    hit = true;
            }

            if (!hit) return null;

            Vector3i normal = new Vector3i();
            switch (side)
            {
                case 0: 
                    normal.X = -step.X; 
                    break;
                case 1: 
                    normal.Y = -step.Y; 
                    break;
                case 2: 
                    normal.Z = -step.Z; 
                    break;
            }

            return (mapPos, normal);
        }

        public bool SetBlock(Vector3i worldPos, byte blockType, bool breakingBlock = false)
        {
            if (worldPos.Y < 0 || worldPos.Y >= Constants.CHUNK_HEIGHT)
                return false;

            ChunkPos chunkPos = new ChunkPos(
                (int)Math.Floor(worldPos.X / (float)Constants.CHUNK_SIZE),
                (int)Math.Floor(worldPos.Z / (float)Constants.CHUNK_SIZE)
            );

            Chunk chunk = mChunkManager.GetChunk(chunkPos);
            if (chunk == null)
                return false;

            Vector3i localPos = new Vector3i(
                worldPos.X - chunkPos.X * Constants.CHUNK_SIZE,
                worldPos.Y,
                worldPos.Z - chunkPos.Z * Constants.CHUNK_SIZE
            );

            if (!chunk.IsInBounds(localPos))
                return false;

            byte oldBlock = chunk.Voxels[localPos.X, localPos.Y, localPos.Z];
            if (oldBlock == BlockIDs.Bedrock)
            {
                return false;
            }

            if (breakingBlock)
            {
                VoxelGame.init.WorldAudioManager.PlayBlockBreakSound(BlockRegistry.GetBlock(chunk.Voxels[localPos.X, localPos.Y, localPos.Z]).Material);
            }

            chunk.Voxels[localPos.X, localPos.Y, localPos.Z] = blockType;

            // Get all chunks that need updating
            var chunksToUpdate = new List<ChunkPos> { chunkPos };

            if (localPos.X == 0)
                chunksToUpdate.Add(new ChunkPos(chunkPos.X - 1, chunkPos.Z));
            if (localPos.X == Constants.CHUNK_SIZE - 1)
                chunksToUpdate.Add(new ChunkPos(chunkPos.X + 1, chunkPos.Z));
            if (localPos.Z == 0)
                chunksToUpdate.Add(new ChunkPos(chunkPos.X, chunkPos.Z - 1));
            if (localPos.Z == Constants.CHUNK_SIZE - 1)
                chunksToUpdate.Add(new ChunkPos(chunkPos.X, chunkPos.Z + 1));

            mChunkManager.MarkChunksForUpdate(chunksToUpdate);
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

            Chunk chunk = mChunkManager.GetChunk(chunkPos);
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

        private bool isPosInsidePlayer(Vector3i blockPos, Vector3 playerPos)
        {
            Vector3 playerSize = new Vector3(0.6f, 1.8f, 0.6f);
            Vector3 halfSize = playerSize * 0.5f;

            Vector3 playerMin = playerPos - halfSize;
            Vector3 playerMax = playerPos + halfSize;

            Vector3 blockMin = new Vector3(blockPos.X, blockPos.Y, blockPos.Z);
            Vector3 blockMax = blockMin + Vector3.One;

            return blockMax.X > playerMin.X && blockMin.X < playerMax.X &&
                   blockMax.Y > playerMin.Y && blockMin.Y < playerMax.Y &&
                   blockMax.Z > playerMin.Z && blockMin.Z < playerMax.Z;
        }

        public void SetCurrentBlockByNum(int blockPos)
        {
            VoxelGame.init.UI_Hotbar.SetHotbarSlot(blockPos);
            mCurrentBlock = (byte)VoxelGame.init.UI_Hotbar.GetSelectedBlock().ID;
            VoxelGame.init.UIText = $"Current Block: {BlockRegistry.GetBlock(mCurrentBlock).Name}";
        }

        public void SetCurrentBlock(int modifier)
        {
            mCurrentBlock = (byte)VoxelGame.init.UI_Hotbar.MoveHotbarSlot((modifier));
            VoxelGame.init.UIText = $"Current Block: {BlockRegistry.GetBlock(mCurrentBlock).Name}";
        }
    }
}