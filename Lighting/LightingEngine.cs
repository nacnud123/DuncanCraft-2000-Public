// This is the main lighting script, it is basically a copy of minecraft's lighting system. Not 100% of how it works. But it looks nice | DA | 9/4/25
using DuncanCraft.Utils;
using DuncanCraft.World;
using System.Collections.Concurrent;

namespace DuncanCraft.Lighting
{
    public class LightingEngine
    {
        private readonly GameWorld _mWorld;
        private readonly ConcurrentQueue<LightingUpdateRegion> _mLightingUpdates;
        private readonly HashSet<long> _mChunksNeedingMeshUpdate;
        private readonly object _mLockObject = new();

        private const int MAX_UPDATES_PER_TICK = 20000;
        private const int MAX_UPDATE_QUEUE_SIZE = 1000000;

        private int mSkylightSubtracted = 0;
        private bool mAllowMeshRegeneration = false;

        public LightingEngine(GameWorld world)
        {
            _mWorld = world;
            _mLightingUpdates = new ConcurrentQueue<LightingUpdateRegion>();
            _mChunksNeedingMeshUpdate = new HashSet<long>();
        }

        public void ScheduleLightingUpdate(LightType lightType, int x1, int y1, int z1, int x2, int y2, int z2)
        {
            if (_mLightingUpdates.Count >= MAX_UPDATE_QUEUE_SIZE)
            {
                Console.WriteLine($"Warning: Lighting update queue full ({MAX_UPDATE_QUEUE_SIZE} updates), dropping update");
                return;
            }
            
            var newRegion = new LightingUpdateRegion(lightType, x1, y1, z1, x2, y2, z2);

            _mLightingUpdates.Enqueue(newRegion);
        }

        public void ProcessLightingUpdates()
        {
            int processedCount = 0;
            int queueSize = _mLightingUpdates.Count;
            
            lock (_mLockObject)
            {
                _mChunksNeedingMeshUpdate.Clear();
            }
            
            
            while (processedCount < MAX_UPDATES_PER_TICK && _mLightingUpdates.TryDequeue(out var region))
            {
                try
                {
                    processLightingUpdateRegion(region);
                    processedCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing lighting update: {ex.Message}");
                    break;
                }
            }
            
            if (processedCount > 0)
            {
                try
                {
                    regenUpdatedChunkMeshes();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error regenerating chunk meshes: {ex.Message}");
                }
            }
        }

        private void regenUpdatedChunkMeshes()
        {
            if (!mAllowMeshRegeneration) 
                return;
            
            HashSet<long> chunksToUpdate;
            lock (_mLockObject)
            {
                if (_mChunksNeedingMeshUpdate.Count == 0) 
                    return;
                
                chunksToUpdate = new HashSet<long>(_mChunksNeedingMeshUpdate);
                _mChunksNeedingMeshUpdate.Clear();
            }
            
            int regenerated = 0;
            foreach (var chunkKey in chunksToUpdate)
            {
                try
                {
                    int chunkX = (int)(chunkKey >> 32);
                    int chunkZ = (int)(chunkKey & 0xFFFFFFFF);
                    
                    var chunk = _mWorld.ChunkManager.GetChunk(chunkX, chunkZ);
                    if (chunk?.State == ChunkState.Generated)
                    {
                        _mWorld.ChunkManager.RegenChunkMesh(chunk);
                        regenerated++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error regenerating mesh for chunk: {ex.Message}");
                }
            }
            
        }

        private void processLightingUpdateRegion(LightingUpdateRegion region)
        {
            if (region.GetVolume() > 32768)
            {
                Console.WriteLine("Warning: Lighting region too large, skipping!");
                return;
            }
            
            for (int x = region.MinX; x <= region.MaxX; x++)
            {
                for (int z = region.MinZ; z <= region.MaxZ; z++)
                {
                    int chunkX = x >> 4;
                    int chunkZ = z >> 4;

                    var chunk = _mWorld.ChunkManager.GetChunk(chunkX, chunkZ);
                    if (chunk?.State != ChunkState.Generated) 
                        continue;

                    long chunkKey = ((long)chunkX << 32) | (uint)chunkZ;
                    lock (_mLockObject)
                    {
                        _mChunksNeedingMeshUpdate.Add(chunkKey);
                    }
                    
                    for (int y = region.MinY; y <= region.MaxY; y++)
                    {
                        if (y < 0 || y >= GameConstants.CHUNK_HEIGHT) 
                            continue;
                        
                        processLightAtPosition(x, y, z, region.LightType);
                    }
                }
            }
        }

        private void processLightAtPosition(int worldX, int worldY, int worldZ, LightType lightType)
        {
            int chunkX = worldX >> 4;
            int chunkZ = worldZ >> 4;

            var chunk = _mWorld.ChunkManager.GetChunk(chunkX, chunkZ);
            if (chunk == null) 
                return;
            
            int localX = worldX & 15;
            int localZ = worldZ & 15;
            
            byte currentLightValue = chunk.GetSavedLightValue(lightType, localX, worldY, localZ);
            byte blockId = chunk.GetBlock(localX, worldY, localZ);
            var block = BlockRegistry.GetBlock(blockId);
            
            if (block == null) 
                return;
            
            byte lightOpacity = block.LightOpacity;
            if (lightOpacity == 0) 
                lightOpacity = 1;
            
            byte sourceLight = 0;

            if (lightType == LightType.Sky)
            {
                if (canBlockSeeTheSky(worldX, worldY, worldZ))
                {
                    sourceLight = 15;
                }
            }
            else if (lightType == LightType.Block)
            {
                sourceLight = block.LightValue;
            }
            
            byte newLightValue;
            
            if (lightOpacity >= 15 && sourceLight == 0)
            {
                newLightValue = 0;
            }
            else
            {
                byte maxNeighborLight = getMaxNeighborLight(worldX, worldY, worldZ, lightType);

                byte propagatedLight = (byte)Math.Max(0, maxNeighborLight - lightOpacity);

                newLightValue = (byte)Math.Max((int)sourceLight, (int)propagatedLight);
            }
            
            // Clamp to valid range
            newLightValue = (byte)Math.Min(15, Math.Max(0, (int)newLightValue));
            
            // Update light value if changed
            if (currentLightValue != newLightValue)
            {
                chunk.SetLightValue(lightType, localX, worldY, localZ, newLightValue);
                
                // Propagate light changes to neighbors
                propagateToNeighbors(worldX, worldY, worldZ, lightType, (byte)Math.Max(0, newLightValue - 1));
            }
        }

        // Gets the maximum light value from all 6 neighboring blocks
        private byte getMaxNeighborLight(int worldX, int worldY, int worldZ, LightType lightType)
        {
            byte maxLight = 0;

            int[] dx = { -1, 1, 0, 0, 0, 0 };
            int[] dy = { 0, 0, -1, 1, 0, 0 };
            int[] dz = { 0, 0, 0, 0, -1, 1 };
            
            for (int i = 0; i < 6; i++)
            {
                int nx = worldX + dx[i];
                int ny = worldY + dy[i];
                int nz = worldZ + dz[i];
                
                byte neighborLight = GetSavedLightValue(lightType, nx, ny, nz);
                maxLight = (byte)Math.Max((int)maxLight, (int)neighborLight);
            }
            
            return maxLight;
        }

        private void propagateToNeighbors(int worldX, int worldY, int worldZ, LightType lightType, byte lightLevel)
        {
            // Propagate to all 6 neighbors
            int[] dx = { -1, 1, 0, 0, 0, 0 };
            int[] dy = { 0, 0, -1, 1, 0, 0 };
            int[] dz = { 0, 0, 0, 0, -1, 1 };
            
            for (int i = 0; i < 6; i++)
            {
                int nx = worldX + dx[i];
                int ny = worldY + dy[i];
                int nz = worldZ + dz[i];
                
                if (_mWorld.IsPositionValid(nx, ny, nz))
                {
                    neighborLightPropagationChanged(lightType, nx, ny, nz, lightLevel);
                }
            }
        }

        // Handles light propagation from neighbors
        private void neighborLightPropagationChanged(LightType lightType, int worldX, int worldY, int worldZ, int propagatedLight)
        {
            byte currentLight = GetSavedLightValue(lightType, worldX, worldY, worldZ);
            byte expectedLight = (byte)propagatedLight;
            
            if (lightType == LightType.Sky && canBlockSeeTheSky(worldX, worldY, worldZ))
            {
                expectedLight = 15; // Direct sky access
            }
            else if (lightType == LightType.Block)
            {
                byte blockId = _mWorld.GetBlock(worldX, worldY, worldZ);
                var block = BlockRegistry.GetBlock(blockId);

                if (block != null && block.LightValue > expectedLight)
                {
                    expectedLight = block.LightValue;
                }
            }
            
            if (currentLight != expectedLight)
            {
                ScheduleLightingUpdate(lightType, worldX, worldY, worldZ, worldX, worldY, worldZ);
            }
        }

        private bool canBlockSeeTheSky(int worldX, int worldY, int worldZ)
        {
            int chunkX = worldX >> 4;
            int chunkZ = worldZ >> 4;

            var chunk = _mWorld.ChunkManager.GetChunk(chunkX, chunkZ);
            if (chunk == null) 
                return false;
            
            int localX = worldX & 15;
            int localZ = worldZ & 15;
            
            return chunk.CanBlockSeeTheSky(localX, worldY, localZ);
        }

        public byte GetSavedLightValue(LightType lightType, int worldX, int worldY, int worldZ)
        {
            if (!_mWorld.IsPositionValid(worldX, worldY, worldZ))
            {
                return lightType == LightType.Sky ? (byte)15 : (byte)0;
            }
            
            int chunkX = worldX >> 4;
            int chunkZ = worldZ >> 4;

            var chunk = _mWorld.ChunkManager.GetChunk(chunkX, chunkZ);
            if (chunk?.State != ChunkState.Generated)
            {
                return lightType == LightType.Sky ? (byte)15 : (byte)0;
            }
            
            int localX = worldX & 15;
            int localZ = worldZ & 15;
            
            return chunk.GetSavedLightValue(lightType, localX, worldY, localZ);
        }

        // Gets the combined light value
        public byte GetCombinedLightValue(int worldX, int worldY, int worldZ)
        {
            if (!_mWorld.IsPositionValid(worldX, worldY, worldZ))
                return 15;
                
            int chunkX = worldX >> 4;
            int chunkZ = worldZ >> 4;

            var chunk = _mWorld.ChunkManager.GetChunk(chunkX, chunkZ);
            if (chunk?.State != ChunkState.Generated)
                return 15;
                
            int localX = worldX & 15;
            int localZ = worldZ & 15;
            
            return chunk.GetBlockLightValue(localX, worldY, localZ, mSkylightSubtracted);
        }

        public void EnableMeshRegeneration()
        {
            mAllowMeshRegeneration = true;

            int clearedCount = 0;
            while (_mLightingUpdates.TryDequeue(out var _))
            {
                clearedCount++;
            }
            
            lock (_mLockObject)
            {
                _mChunksNeedingMeshUpdate.Clear();
            }
            
            if (clearedCount > 0)
            {
                Console.WriteLine($"Cleared {clearedCount} initialization lighting updates");
            }
        }

        public void OnBlockChanged(int worldX, int worldY, int worldZ, byte oldBlock, byte newBlock)
        {
            var oldBlockData = BlockRegistry.GetBlock(oldBlock);
            var newBlockData = BlockRegistry.GetBlock(newBlock);
            
            bool lightingChanged = false;

            if (oldBlockData?.LightValue != newBlockData?.LightValue)
            {
                ScheduleLightingUpdate(LightType.Block, worldX - 1, worldY - 1, worldZ - 1, worldX + 1, worldY + 1, worldZ + 1);
                lightingChanged = true;
            }

            if (oldBlockData?.LightOpacity != newBlockData?.LightOpacity)
            {
                ScheduleLightingUpdate(LightType.Sky, worldX - 1, worldY - 1, worldZ - 1, worldX + 1, worldY + 1, worldZ + 1);

                ScheduleLightingUpdate(LightType.Block, worldX - 1, worldY - 1, worldZ - 1, worldX + 1, worldY + 1, worldZ + 1);
                lightingChanged = true;
            }
            
            if (lightingChanged)
            {
                int chunkX = worldX >> 4;
                int chunkZ = worldZ >> 4;

                var chunk = _mWorld.ChunkManager.GetChunk(chunkX, chunkZ);
                chunk?.UpdateHeightMap();
            }
        }

        public void InitChunkLighting(Chunk chunk)
        {
            chunk.InitLighting();

            int worldX = chunk.X * GameConstants.CHUNK_SIZE;
            int worldZ = chunk.Z * GameConstants.CHUNK_SIZE;
            
            ScheduleLightingUpdate(LightType.Sky, worldX, 0, worldZ, worldX + GameConstants.CHUNK_SIZE - 1, GameConstants.CHUNK_HEIGHT - 1, worldZ + GameConstants.CHUNK_SIZE - 1);

            ScheduleLightingUpdate(LightType.Block, worldX, 0, worldZ, worldX + GameConstants.CHUNK_SIZE - 1, GameConstants.CHUNK_HEIGHT - 1, worldZ + GameConstants.CHUNK_SIZE - 1);
        }
    }
}