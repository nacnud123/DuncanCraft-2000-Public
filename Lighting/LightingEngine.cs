// The main lighting engine of the game, lots and lots of functions. Probably way to complex but I wanted it to look just right. | DA | 8/25/25
using OpenTK.Mathematics;
using VoxelGame.World;
using VoxelGame.Utils;

namespace VoxelGame.Lighting
{
    public class LightingEngine
    {
        private const byte MAX_LIGHT_LEVEL = 15;
        private const byte MIN_LIGHT_LEVEL = 0;
        private const float LIGHT_LEVEL_STEP = 1.0f / MAX_LIGHT_LEVEL;

        private static readonly Vector3i[] DIRECTIONS =
        [
            new Vector3i(1, 0, 0),   // +X
            new Vector3i(-1, 0, 0),  // -X
            new Vector3i(0, 1, 0),   // +Y
            new Vector3i(0, -1, 0),  // -Y
            new Vector3i(0, 0, 1),   // +Z
            new Vector3i(0, 0, -1)   // -Z
        ];

        private ChunkManager mChunkManager;

        private Queue<(Vector3i pos, byte lightLevel, bool isSunlight)> mPropagationQueue;
        private Queue<(Vector3i pos, byte oldLightLevel, bool isSunlight)> mRemovalQueue;

        private readonly Dictionary<ChunkPos, Chunk> mChunkCache = new(16);
        private readonly object mChunkCacheLock = new object();
        private int mCacheHits = 0, mCacheMisses = 0;

        private readonly List<ChunkPos> mReusableChunkPosList = new(64);
        private readonly HashSet<ChunkPos> mReusableChunkPosSet = new(64);
        private readonly List<Chunk> mReusableChunkList = new(16);
        private readonly HashSet<Vector3i> mReusablePositionSet = new(1000);
        private readonly object mReusableCollectionsLock = new object();

        public LightingEngine(ChunkManager chunkManager)
        {
            this.mChunkManager = chunkManager;
            this.mPropagationQueue = new Queue<(Vector3i, byte, bool)>(2000);
            this.mRemovalQueue = new Queue<(Vector3i, byte, bool)>(2000);
        }

        public void CalcMultiChunkLighting(List<Chunk> chunks)
        {
            mPropagationQueue.Clear();
            mRemovalQueue.Clear();

            foreach (var chunk in chunks)
            {
                initChunkLighting(chunk);
            }

            foreach (var chunk in chunks)
            {
                calcChunkSunlight(chunk);
            }

            propagateAllLighting();

            if (chunks.Count > 1)
            {
                FixChunkBoundaryLighting(chunks);
            }

            foreach (var chunk in chunks)
            {
                chunk.MeshGenerated = false;
            }
        }

        public void FixChunkBoundaryLighting(List<Chunk> chunks)
        {
            mPropagationQueue.Clear();
            
            var chunksWithNeighbors = new HashSet<ChunkPos>();

            foreach (var chunk in chunks)
            {
                bool hasAllNeighbors = true;

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        if (dx == 0 && dz == 0) continue;
                        
                        var neighborPos = new ChunkPos(chunk.Position.X + dx, chunk.Position.Z + dz);
                        var (neighborChunk, _) = getChunkAndLocalPos(chunkLocalToWorld(neighborPos, Vector3i.Zero));
                        
                        if (neighborChunk == null || !neighborChunk.TerrainGenerated)
                        {
                            hasAllNeighbors = false;
                            break;
                        }
                    }
                    if (!hasAllNeighbors) break;
                }
                
                if (hasAllNeighbors)
                {
                    chunksWithNeighbors.Add(chunk.Position);
                }
            }
            
            foreach (var chunk in chunks)
            {
                if (chunksWithNeighbors.Contains(chunk.Position))
                {
                    recalcChunkBoundaryLighting(chunk);
                }
            }

            if (mPropagationQueue.Count > 0)
            {
                propagateAllLighting();
            }
        }

        private void recalcChunkBoundaryLighting(Chunk chunk)
        {
            const int boundaryWidth = 1;

            for (int x = 0; x < GameConstants.CHUNK_SIZE; x++)
            {
                for (int z = 0; z < GameConstants.CHUNK_SIZE; z++)
                {
                    bool isBoundary = (x < boundaryWidth || x >= GameConstants.CHUNK_SIZE - boundaryWidth || z < boundaryWidth || z >= GameConstants.CHUNK_SIZE - boundaryWidth);   
                    
                    if (!isBoundary) 
                        continue;

                    bool isCorner = (x < boundaryWidth || x >= GameConstants.CHUNK_SIZE - boundaryWidth) && (z < boundaryWidth || z >= GameConstants.CHUNK_SIZE - boundaryWidth);
                    if (!isCorner && (x + z) % 2 == 0)
                        continue;

                    recalcSunlightColumnWithNeighbors(chunk, x, z);
                }
            }
        }

        // Recalculates sunlight for a specific column
        private void recalcSunlightColumnWithNeighbors(Chunk chunk, int x, int z)
        {
            for (int y = GameConstants.CHUNK_HEIGHT - 1; y >= 0; y--)
            {
                byte blockType = chunk.Voxels[x, y, z];
                
                // Skip solid blocks
                if (isSolid(blockType) && !isTransparent(blockType))
                {
                    continue;
                }

                byte bestLightLevel = calcBestLightLevel(chunk, x, y, z, true);

                if (bestLightLevel > chunk.SunlightLevels[x, y, z])
                {
                    chunk.SunlightLevels[x, y, z] = bestLightLevel;

                    if (bestLightLevel > 1)
                    {
                        Vector3i worldPos = chunkLocalToWorld(chunk.Position, new Vector3i(x, y, z));
                        mPropagationQueue.Enqueue((worldPos, bestLightLevel, true));
                    }
                }

                byte bestBlockLight = calcBestLightLevel(chunk, x, y, z, false);
                if (bestBlockLight > chunk.BlockLightLevels[x, y, z])
                {
                    chunk.BlockLightLevels[x, y, z] = bestBlockLight;
                    
                    if (bestBlockLight > 1)
                    {
                        Vector3i worldPos = chunkLocalToWorld(chunk.Position, new Vector3i(x, y, z));
                        mPropagationQueue.Enqueue((worldPos, bestBlockLight, false));
                    }
                }
            }
        }
        
        // Calculates the best possible light level for a position
        private byte calcBestLightLevel(Chunk chunk, int x, int y, int z, bool isSunlight)
        {
            if (chunk.SunlightLevels == null || chunk.BlockLightLevels == null)
                return 0;
                
            byte bestLight = isSunlight ? chunk.SunlightLevels[x, y, z] : chunk.BlockLightLevels[x, y, z];
            Vector3i worldPos = chunkLocalToWorld(chunk.Position, new Vector3i(x, y, z));
            
            foreach (var direction in DIRECTIONS)
            {
                Vector3i neighborPos = worldPos + direction;

                if (neighborPos.Y < 0 || neighborPos.Y >= GameConstants.CHUNK_HEIGHT)
                    continue;
                
                var (neighborChunk, localPos) = getChunkAndLocalPos(neighborPos);
                if (neighborChunk == null || !neighborChunk.TerrainGenerated || !isInChunkBounds(localPos) ||
                    neighborChunk.Voxels == null || neighborChunk.SunlightLevels == null || neighborChunk.BlockLightLevels == null)
                    continue;
                
                byte neighborBlockType = neighborChunk.Voxels[localPos.X, localPos.Y, localPos.Z];

                if (isSolid(neighborBlockType) && !isTransparent(neighborBlockType))
                    continue;
                
                byte neighborLight;
                if (isSunlight)
                {
                    neighborLight = neighborChunk.SunlightLevels[localPos.X, localPos.Y, localPos.Z];

                    if (direction.Y == -1 && neighborLight == MAX_LIGHT_LEVEL)
                    {
                        bestLight = Math.Max(bestLight, MAX_LIGHT_LEVEL);
                        continue;
                    }
                }
                else
                {
                    neighborLight = neighborChunk.BlockLightLevels[localPos.X, localPos.Y, localPos.Z];
                }

                byte currentBlockType = chunk.Voxels[x, y, z];
                byte reduction = Math.Max((byte)1, getLightReduction(currentBlockType));
                byte effectiveLight = (byte)Math.Max(0, neighborLight - reduction);
                
                bestLight = Math.Max(bestLight, effectiveLight);
            }
            
            return bestLight;
        }

        private void initChunkLighting(Chunk chunk)
        {
            for (int x = 0; x < GameConstants.CHUNK_SIZE; x++)
            {
                for (int y = 0; y < GameConstants.CHUNK_HEIGHT; y++)
                {
                    for (int z = 0; z < GameConstants.CHUNK_SIZE; z++)
                    {
                        byte blockType = chunk.Voxels[x, y, z];

                        // Sunlight
                        chunk.SunlightLevels[x, y, z] = MIN_LIGHT_LEVEL;

                        // Block light
                        if (isLightSource(blockType))
                        {
                            byte lightLevel = getBlockLightEmission(blockType);
                            chunk.BlockLightLevels[x, y, z] = lightLevel;

                            Vector3i worldPos = chunkLocalToWorld(chunk.Position, new Vector3i(x, y, z));
                            mPropagationQueue.Enqueue((worldPos, lightLevel, false));
                        }
                        else
                        {
                            chunk.BlockLightLevels[x, y, z] = MIN_LIGHT_LEVEL;
                        }
                    }
                }
            }
        }

        private void calcChunkSunlight(Chunk chunk)
        {
            for (int x = 0; x < GameConstants.CHUNK_SIZE; x++)
            {
                for (int z = 0; z < GameConstants.CHUNK_SIZE; z++)
                {
                    calcSunlightColumn(chunk, x, z, true);
                }
            }

            propagateEdgeLighting(chunk);
        }
        
        // Calculate sunlight for a single column
        private void calcSunlightColumn(Chunk chunk, int x, int z, bool queueForPropagation = false)
        {
            byte currentSunlight = MAX_LIGHT_LEVEL;

            Vector3i worldTopPos = chunkLocalToWorld(chunk.Position, new Vector3i(x, GameConstants.CHUNK_HEIGHT - 1, z));
            worldTopPos.Y += 1;
            
            var (topChunk, topLocalPos) = getChunkAndLocalPos(worldTopPos);
            if (topChunk != null && topChunk.TerrainGenerated && isInChunkBounds(topLocalPos) && 
                topChunk.SunlightLevels != null)
            {
                currentSunlight = topChunk.SunlightLevels[topLocalPos.X, topLocalPos.Y, topLocalPos.Z];
            }

            if (x == 0 || x == GameConstants.CHUNK_SIZE - 1 || z == 0 || z == GameConstants.CHUNK_SIZE - 1)
            {
                byte maxNeighborSunlight = getMaxNeighborSunlight(chunk, x, GameConstants.CHUNK_HEIGHT - 1, z);
                currentSunlight = Math.Max(currentSunlight, maxNeighborSunlight);
            }

            // Propagate sunlight down
            for (int y = GameConstants.CHUNK_HEIGHT - 1; y >= 0; y--)
            {
                byte blockType = chunk.Voxels[x, y, z];

                if (blockType == BlockIDs.Air || isTransparent(blockType))
                {
                    // Check for better lighting from neighbors
                    if ((x == 0 || x == GameConstants.CHUNK_SIZE - 1 || z == 0 || z == GameConstants.CHUNK_SIZE - 1))
                    {
                        byte neighborSunlight = getMaxNeighborSunlight(chunk, x, y, z);
                        currentSunlight = Math.Max(currentSunlight, neighborSunlight);
                    }
                    
                    chunk.SunlightLevels[x, y, z] = currentSunlight;
                    
                    // Queue for lateral propagation
                    if (queueForPropagation && currentSunlight > 1)
                    {
                        Vector3i worldPos = chunkLocalToWorld(chunk.Position, new Vector3i(x, y, z));
                        mPropagationQueue.Enqueue((worldPos, currentSunlight, true));
                    }
                }
                else if (isSolid(blockType) && !isTransparent(blockType))
                {
                    // Opaque block blocks all sunlight
                    chunk.SunlightLevels[x, y, z] = MIN_LIGHT_LEVEL;
                    currentSunlight = MIN_LIGHT_LEVEL;
                }
                else
                {
                    // Transparent or semi-transparent block reduces sunlight
                    byte reduction = getLightReduction(blockType);
                    currentSunlight = (byte)Math.Max(0, currentSunlight - reduction);
                    chunk.SunlightLevels[x, y, z] = currentSunlight;
                    
                    if (queueForPropagation && currentSunlight > 1)
                    {
                        Vector3i worldPos = chunkLocalToWorld(chunk.Position, new Vector3i(x, y, z));
                        mPropagationQueue.Enqueue((worldPos, currentSunlight, true));
                    }
                }
            }
        }

        // Gets the maximum sunlight from neighboring chunks at chunk boundaries
        private byte getMaxNeighborSunlight(Chunk chunk, int x, int y, int z)
        {
            byte maxSunlight = 0;
            Vector3i worldPos = chunkLocalToWorld(chunk.Position, new Vector3i(x, y, z));
            
            // Check the 4 horizontal neighbors
            Vector3i[] horizontalDirections = {
                new Vector3i(1, 0, 0),   // +X
                new Vector3i(-1, 0, 0),  // -X
                new Vector3i(0, 0, 1),   // +Z
                new Vector3i(0, 0, -1)   // -Z
            };
            
            foreach (var direction in horizontalDirections)
            {
                Vector3i neighborWorldPos = worldPos + direction;
                
                var (neighborChunk, neighborLocalPos) = getChunkAndLocalPos(neighborWorldPos);

                if (neighborChunk != null && neighborChunk.TerrainGenerated && isInChunkBounds(neighborLocalPos) &&
                    neighborChunk.SunlightLevels != null && neighborChunk.Voxels != null)
                {
                    byte neighborSunlight = neighborChunk.SunlightLevels[neighborLocalPos.X, neighborLocalPos.Y, neighborLocalPos.Z];
                    
                    // Account for light reduction when coming from neighbor
                    byte neighborBlockType = neighborChunk.Voxels[neighborLocalPos.X, neighborLocalPos.Y, neighborLocalPos.Z];
                    byte reduction = Math.Max((byte)1, getLightReduction(neighborBlockType));
                    byte effectiveLight = (byte)Math.Max(0, neighborSunlight - reduction);
                    
                    maxSunlight = Math.Max(maxSunlight, effectiveLight);
                }
            }
            
            return maxSunlight;
        }

        // Forces lighting propagation at chunk edges
        private void propagateEdgeLighting(Chunk chunk)
        {
            for (int x = 0; x < GameConstants.CHUNK_SIZE; x++)
            {
                for (int y = 0; y < GameConstants.CHUNK_HEIGHT; y++)
                {
                    for (int z = 0; z < GameConstants.CHUNK_SIZE; z++)
                    {
                        // Only process edge blocks
                        bool isEdge = (x == 0 || x == GameConstants.CHUNK_SIZE - 1 || z == 0 || z == GameConstants.CHUNK_SIZE - 1);
                                      
                        if (!isEdge)
                            continue;
                        
                        byte currentSunlight = chunk.SunlightLevels[x, y, z];
                        byte currentBlockLight = chunk.BlockLightLevels[x, y, z];
                        
                        // Queue edge blocks for propagation
                        if (currentSunlight > 1)
                        {
                            Vector3i worldPos = chunkLocalToWorld(chunk.Position, new Vector3i(x, y, z));
                            mPropagationQueue.Enqueue((worldPos, currentSunlight, true));
                        }
                        
                        if (currentBlockLight > 1)
                        {
                            Vector3i worldPos = chunkLocalToWorld(chunk.Position, new Vector3i(x, y, z));
                            mPropagationQueue.Enqueue((worldPos, currentBlockLight, false));
                        }
                    }
                }
            }
        }

        // Efficiently propagates all queued lighting
        private void propagateAllLighting()
        {
            mReusablePositionSet.Clear();

            while (mPropagationQueue.Count > 0)
            {
                var (currentWorldPos, lightLevel, isSunlight) = mPropagationQueue.Dequeue();

                if (lightLevel <= 1 || mReusablePositionSet.Contains(currentWorldPos))
                    continue;

                mReusablePositionSet.Add(currentWorldPos);

                // Propagate to all 6 adjacent blocks
                foreach (var direction in DIRECTIONS)
                {
                    Vector3i neighborPos = currentWorldPos + direction;

                    if (neighborPos.Y < 0 || neighborPos.Y >= GameConstants.CHUNK_HEIGHT)
                        continue;

                    if (mReusablePositionSet.Contains(neighborPos))
                        continue;

                    var (neighborChunk, localPos) = getChunkAndLocalPos(neighborPos);

                    if (neighborChunk == null || !neighborChunk.TerrainGenerated || !isInChunkBounds(localPos) ||
                        neighborChunk.Voxels == null || neighborChunk.SunlightLevels == null || neighborChunk.BlockLightLevels == null)
                        continue;

                    byte neighborBlockType = neighborChunk.Voxels[localPos.X, localPos.Y, localPos.Z];

                    if (isSolid(neighborBlockType) && !isTransparent(neighborBlockType))
                        continue;

                    byte newLightLevel;
                    if (isSunlight && direction.Y == -1 && lightLevel == MAX_LIGHT_LEVEL)
                    {
                        
                        newLightLevel = MAX_LIGHT_LEVEL;
                    }
                    else
                    {
                        byte reduction = Math.Max((byte)1, getLightReduction(neighborBlockType));
                        newLightLevel = (byte)Math.Max(0, lightLevel - reduction);
                    }

                    byte currentNeighborLight;
                    if (isSunlight)
                    {
                        currentNeighborLight = neighborChunk.SunlightLevels[localPos.X, localPos.Y, localPos.Z];
                        if (newLightLevel > currentNeighborLight)
                        {
                            neighborChunk.SunlightLevels[localPos.X, localPos.Y, localPos.Z] = newLightLevel;
                            if (newLightLevel > 1)
                            {
                                mPropagationQueue.Enqueue((neighborPos, newLightLevel, true));
                            }
                        }
                    }
                    else
                    {
                        currentNeighborLight = neighborChunk.BlockLightLevels[localPos.X, localPos.Y, localPos.Z];
                        if (newLightLevel > currentNeighborLight)
                        {
                            neighborChunk.BlockLightLevels[localPos.X, localPos.Y, localPos.Z] = newLightLevel;
                            if (newLightLevel > 1)
                            {
                                mPropagationQueue.Enqueue((neighborPos, newLightLevel, false));
                            }
                        }
                    }
                }
            }
        }

        private (Chunk chunk, Vector3i localPos) getChunkAndLocalPos(Vector3i worldPos)
        {
            ChunkPos chunkPos = new ChunkPos(
                (int)Math.Floor(worldPos.X / (float)GameConstants.CHUNK_SIZE),
                (int)Math.Floor(worldPos.Z / (float)GameConstants.CHUNK_SIZE)
            );

            Chunk chunk;
            lock (mChunkCacheLock)
            {
                if (mChunkCache.TryGetValue(chunkPos, out chunk))
                {
                    mCacheHits++;
                }
                else
                {
                    mCacheMisses++;
                    chunk = mChunkManager.GetChunk(chunkPos);

                    if (chunk != null)
                    {
                        if (mChunkCache.Count >= 32)
                        {
                            var firstKey = mChunkCache.Keys.First();
                            mChunkCache.Remove(firstKey);
                        }
                        mChunkCache[chunkPos] = chunk;
                    }
                }
            }

            if (chunk != null)
            {
                Vector3i localPos = new Vector3i(
                    worldPos.X - chunkPos.X * GameConstants.CHUNK_SIZE,
                    worldPos.Y,
                    worldPos.Z - chunkPos.Z * GameConstants.CHUNK_SIZE
                );

                if (localPos.X < 0)
                    localPos.X += GameConstants.CHUNK_SIZE;

                if (localPos.Z < 0)
                    localPos.Z += GameConstants.CHUNK_SIZE;

                return (chunk, localPos);
            }

            return (null, Vector3i.Zero);
        }

        private Vector3i chunkLocalToWorld(ChunkPos chunkPos, Vector3i localPos)
        {
            return new Vector3i(
                chunkPos.X * GameConstants.CHUNK_SIZE + localPos.X,
                localPos.Y,
                chunkPos.Z * GameConstants.CHUNK_SIZE + localPos.Z
            );
        }

        private bool isInChunkBounds(Vector3i localPos)
        {
            return localPos.X >= 0 && localPos.X < GameConstants.CHUNK_SIZE &&
                   localPos.Y >= 0 && localPos.Y < GameConstants.CHUNK_HEIGHT &&
                   localPos.Z >= 0 && localPos.Z < GameConstants.CHUNK_SIZE;
        }

        // Gets the combined light level at a specific position
        public float GetLightAtPos(Chunk chunk, int x, int y, int z)
        {
            // Early null check for the input chunk
            if (chunk == null || chunk.SunlightLevels == null || chunk.BlockLightLevels == null)
                return 0.05f;

            if (x < 0 || x >= GameConstants.CHUNK_SIZE || y < 0 || y >= GameConstants.CHUNK_HEIGHT || z < 0 || z >= GameConstants.CHUNK_SIZE)
            {
                Vector3i worldPos = chunkLocalToWorld(chunk.Position, new Vector3i(x, y, z));

                var (targetChunk, localPos) = getChunkAndLocalPos(worldPos);

                if (targetChunk == null || !targetChunk.TerrainGenerated)
                    return 0.4f;

                if (!isInChunkBounds(localPos))
                    return 0.4f;

                if (targetChunk.SunlightLevels == null || targetChunk.BlockLightLevels == null)
                    return 0.4f;

                try
                {
                    byte sunlight = targetChunk.SunlightLevels[localPos.X, localPos.Y, localPos.Z];
                    byte blockLight = targetChunk.BlockLightLevels[localPos.X, localPos.Y, localPos.Z];

                    byte maxLight = Math.Max(sunlight, blockLight);
                    return Math.Max(0.05f, maxLight * LIGHT_LEVEL_STEP);
                }
                catch (IndexOutOfRangeException)
                {
                    return 0.4f;
                }
                catch (NullReferenceException)
                {
                    return 0.4f;
                }
            }

            try
            {
                byte chunkSunlight = chunk.SunlightLevels[x, y, z];
                byte chunkBlockLight = chunk.BlockLightLevels[x, y, z];

                byte maxChunkLight = Math.Max(chunkSunlight, chunkBlockLight);

                return Math.Max(0.05f, maxChunkLight * LIGHT_LEVEL_STEP);
            }
            catch (IndexOutOfRangeException)
            {
                return 0.4f;
            }
            catch (NullReferenceException)
            {
                return 0.4f;
            }
        }

        public float GetFaceLightVal(Chunk chunk, int x, int y, int z, int faceIndex)
        {
            Vector3i samplePos = new Vector3i(x, y, z);

            switch (faceIndex)
            {
                case 0:  // Front face
                    samplePos.Z += 1;
                    break;
                case 1: // Back face
                    samplePos.Z -= 1;
                    break;
                case 2: // Left face
                    samplePos.X -= 1;
                    break;
                case 3: // Right face
                    samplePos.X += 1;
                    break;
                case 4: // Top face
                    samplePos.Y += 1;
                    break;
                case 5: // Bottom face
                    samplePos.Y -= 1;
                    break;
            }

            return GetLightAtPos(chunk, samplePos.X, samplePos.Y, samplePos.Z);
        }

        // Determines if a block type emits light
        private bool isLightSource(byte blockType) => BlockRegistry.GetBlock(blockType).LightLevel > 0;
        private byte getBlockLightEmission(byte blockType)
        {
            return BlockRegistry.GetBlock(blockType).LightLevel;
        }

        private bool isTransparent(byte blockType)
        {
            if (blockType == BlockIDs.Air) 
                return true;

            return BlockRegistry.GetBlock(blockType)?.Transparent ?? false;
        }

        private bool isSolid(byte blockType)
        {
            if (blockType == BlockIDs.Air) 
                return false;

            return BlockRegistry.GetBlock(blockType)?.IsSolid ?? true;
        }

        private byte getLightReduction(byte blockType)
        {
            switch (blockType)
            {
                case BlockIDs.Air:
                case BlockIDs.BrownMushroom:
                case BlockIDs.RedMushroom:
                case BlockIDs.GrassTuff:
                case BlockIDs.YellowFlower:
                    return 0; // No reduction for certain blocks
                case BlockIDs.Glass:
                    return 1; // Slight reduction for glass
                case BlockIDs.Leaves:
                    return 2; // Some reduction for leaves
                default:
                    if (isTransparent(blockType))
                        return 1;
                    return 15;
            }
        }

        // Removes a chunk from the cache to prevent memory leaks
        public void RemoveChunkFromCache(ChunkPos chunkPos)
        {
            lock (mChunkCacheLock)
            {
                mChunkCache.Remove(chunkPos);
            }
        }

        // Clears the entire chunk cache
        public void ClearCache()
        {
            lock (mChunkCacheLock)
            {
                mChunkCache.Clear();
            }

            lock (mReusableCollectionsLock)
            {
                mReusableChunkPosList.Clear();
                mReusableChunkPosSet.Clear();
                mReusableChunkList.Clear();
                mReusablePositionSet.Clear();
            }
        }
    }
}