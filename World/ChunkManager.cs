// This is the main chunk manager of the game, it helps keep track of what chunks are generated, what needs to be generated, and it has some threading that I'm still not 100% sure about. | DA | 8/25/25
using OpenTK.Mathematics;
using System.Collections.Concurrent;
using System.Threading.Channels;
using VoxelGame.Saving;
using VoxelGame.Utils;
using VoxelGame.World;
using VoxelGame.Ticking;
using VoxelGame.Lighting;

public class ChunkManager : IDisposable
{
    public readonly ConcurrentDictionary<ChunkPos, Chunk> _Chunks = new();

    // Mesh generation system
    private readonly Channel<ChunkPos> _mMeshGenerationChannel;
    private readonly ChannelReader<ChunkPos> _mMeshReader;
    private readonly ChannelWriter<ChunkPos> _mMeshWriter;
    private readonly HashSet<ChunkPos> _mQueuedMeshChunks = new();
    private readonly SemaphoreSlim _mMeshQueueLock = new SemaphoreSlim(1, 1);

    // Task management
    private readonly CancellationTokenSource _mCancellationTokenSource = new();
    private Task _mMeshGenerationTask;

    // Object pooling for performance
    private readonly ConcurrentQueue<List<Vertex>> _mVertexListPool = new();
    private readonly ConcurrentQueue<List<uint>> _mIndexListPool = new();

    // Performance constants - optimized values
    private const int MAX_POOLED_OBJECTS = 100;
    private const int MAX_CONCURRENT_MESH_OPERATIONS = 12;
    private const int MAX_CHUNKS_LOADED_PER_FRAME = 4;
    private const int TARGET_TPS = 20;
    private const int MAX_CHUNKS_PROCESSED_PER_FRAME = 6;

    private Frustum mFrustum = new Frustum();
    private WorldTickSystem mTickSystem;
    private LightingEngine mLightingEngine;

    private readonly object mMeshGenerationLock = new object();

    public LightingEngine LightingEngine => mLightingEngine;

    public ChunkManager()
    {
        mTickSystem = new WorldTickSystem(this);

        mLightingEngine = new LightingEngine(this);

        initializeObjectPools();
        
        var options = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        };
        
        _mMeshGenerationChannel = Channel.CreateBounded<ChunkPos>(options);
        _mMeshReader = _mMeshGenerationChannel.Reader;
        _mMeshWriter = _mMeshGenerationChannel.Writer;

        _mMeshGenerationTask = processMeshGenerationAsync(_mCancellationTokenSource.Token);
        
        mTickSystem.Start();
    }

    private void initializeObjectPools()
    {
        for (int i = 0; i < 20; i++)
        {
            _mVertexListPool.Enqueue(new List<Vertex>());
            _mIndexListPool.Enqueue(new List<uint>());
        }
    }

    public List<Vertex> GetVertexList()
    {
        if (_mVertexListPool.TryDequeue(out var list))
        {
            list.Clear();
            return list;
        }
        return new List<Vertex>();
    }

    public List<uint> GetIndexList()
    {
        if (_mIndexListPool.TryDequeue(out var list))
        {
            list.Clear();
            return list;
        }
        return new List<uint>();
    }

    public void ReturnVertexList(List<Vertex> list)
    {
        if (list != null && _mVertexListPool.Count < MAX_POOLED_OBJECTS)
        {
            list.Clear();
            list.TrimExcess();
            _mVertexListPool.Enqueue(list);
        }
    }

    public void ReturnIndexList(List<uint> list)
    {
        if (list != null && _mIndexListPool.Count < MAX_POOLED_OBJECTS)
        {
            list.Clear();
            list.TrimExcess();
            _mIndexListPool.Enqueue(list);
        }
    }

    public void UpdateChunks(Vector3 playerPos)
    {
        ChunkPos playerChunk = new ChunkPos(
            (int)MathF.Floor(playerPos.X / GameConstants.CHUNK_SIZE),
            (int)MathF.Floor(playerPos.Z / GameConstants.CHUNK_SIZE)
        );

        VoxelGame.VoxelGame.init.CurrentChunkPosition = new Vector2i(playerChunk.X, playerChunk.Z);

        var newChunks = new List<ChunkPos>();
        var loadedFromDiskChunks = new List<ChunkPos>();

        var chunksToLoad = getDistanceSortedChunksPos(playerChunk, GameConstants.RENDER_DISTANCE);

        int chunksLoadedThisFrame = 0;
        foreach (var pos in chunksToLoad)
        {
            if (!_Chunks.ContainsKey(pos))
            {
                _Chunks[pos] = new Chunk(pos, false);
                newChunks.Add(pos);

                // Try to load from disk synchronously first for immediate chunks, async for distant ones
                bool loadedSync = false;
                float distance = MathF.Sqrt((pos.X - playerChunk.X) * (pos.X - playerChunk.X) + (pos.Z - playerChunk.Z) * (pos.Z - playerChunk.Z));
                
                if (distance <= 2.0f) // Load nearby chunks synchronously for immediate availability
                {
                    if (Serialization.Load(_Chunks[pos]))
                    {
                        _Chunks[pos].TerrainGenerated = true;
                        loadedFromDiskChunks.Add(pos);
                        loadedSync = true;
                    }
                }

                if (!loadedSync)
                {
                    // Load distant chunks asynchronously or generate terrain
                    _ = Task.Run(() =>
                    {
                        if (Serialization.Load(_Chunks[pos]))
                        {
                            _Chunks[pos].TerrainGenerated = true;
                            processLoadedChunk(pos);
                        }
                        else
                        {
                            mTickSystem.QueueTerrainGeneration(pos);
                        }
                    });
                }

                if (!loadedSync)
                {
                    mTickSystem.QueueTerrainGeneration(pos);
                }

                chunksLoadedThisFrame++;

                if (chunksLoadedThisFrame >= MAX_CHUNKS_LOADED_PER_FRAME)
                {
                    break;
                }
            }
        }

        if (loadedFromDiskChunks.Count > 0)
        {
            processLoadedChunks(loadedFromDiskChunks);
        }

        // Queue mesh generation for chunks that need it
        foreach (var kvp in _Chunks.ToList())
        {
            if (!kvp.Value.MeshGenerated && kvp.Value.TerrainGenerated)
            {
                _ = queueMeshGenAsync(kvp.Key);
            }
        }

        // Remove distant chunks
        var chunksToRemove = new List<ChunkPos>();
        foreach (var kvp in _Chunks)
        {
            int dx = kvp.Key.X - playerChunk.X;
            int dz = kvp.Key.Z - playerChunk.Z;

            if (Math.Abs(dx) > GameConstants.RENDER_DISTANCE + 1 || Math.Abs(dz) > GameConstants.RENDER_DISTANCE + 1)
            {
                chunksToRemove.Add(kvp.Key);
            }
        }

        // Remove chunks in batches
        foreach (var pos in chunksToRemove)
        {
            if (_Chunks.TryRemove(pos, out var chunk))
            {
                if (chunk.Modified)
                    Serialization.SaveChunk(chunk);

                lock (_mMeshQueueLock)
                {
                    _mQueuedMeshChunks.Remove(pos);
                }

                mLightingEngine?.RemoveChunkFromCache(pos);

                chunk.Dispose();
            }
        }
    }

    private void processLoadedChunk(ChunkPos chunkPos)
    {
        var loadedChunks = new List<ChunkPos> { chunkPos };
        processLoadedChunks(loadedChunks);
    }

    private void processLoadedChunks(List<ChunkPos> loadedChunks)
    {
        var chunksNeedingLighting = new HashSet<ChunkPos>();
        var chunksNeedingMeshUpdate = new HashSet<ChunkPos>();

        foreach (var chunkPos in loadedChunks)
        {
            chunksNeedingLighting.Add(chunkPos);
            chunksNeedingMeshUpdate.Add(chunkPos);

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dz == 0) continue;

                    var neighborPos = new ChunkPos(chunkPos.X + dx, chunkPos.Z + dz);
                    if (_Chunks.ContainsKey(neighborPos) && _Chunks[neighborPos].TerrainGenerated)
                    {
                        chunksNeedingLighting.Add(neighborPos);
                        chunksNeedingMeshUpdate.Add(neighborPos);
                    }
                }
            }
        }

        // Queue lighting update
        mTickSystem.QueueLightingUpdate(new LightingUpdate
        {
            Type = LightingUpdateType.BatchChunkGenerated,
            AffectedChunks = new List<ChunkPos>(chunksNeedingLighting)
        });

        // Queue mesh updates
        foreach (var chunkPos in chunksNeedingMeshUpdate)
        {
            mTickSystem.QueueChunkUpdate(new ChunkTickUpdate
            {
                ChunkPos = chunkPos,
                UpdateType = ChunkUpdateType.MeshRegeneration
            });
        }
    }

    private async Task queueMeshGenAsync(ChunkPos pos)
    {
        await _mMeshQueueLock.WaitAsync();
        try
        {
            if (!_mQueuedMeshChunks.Contains(pos))
            {
                _mQueuedMeshChunks.Add(pos);
                await _mMeshWriter.WriteAsync(pos, _mCancellationTokenSource.Token);
            }
        }
        finally
        {
            _mMeshQueueLock.Release();
        }
    }

    // Processes mesh generation requests using async/await
    private async Task processMeshGenerationAsync(CancellationToken cancellationToken)
    {
        var semaphore = new SemaphoreSlim(MAX_CONCURRENT_MESH_OPERATIONS, MAX_CONCURRENT_MESH_OPERATIONS);
        var activeTasks = new List<Task>();

        try
        {
            await foreach (var chunkPos in _mMeshReader.ReadAllAsync(cancellationToken))
            {
                // Remove from queued set
                await _mMeshQueueLock.WaitAsync(cancellationToken);
                try
                {
                    _mQueuedMeshChunks.Remove(chunkPos);
                }
                finally
                {
                    _mMeshQueueLock.Release();
                }

                // Wait for available slot
                await semaphore.WaitAsync(cancellationToken);

                // Start mesh generation task
                var meshTask = genChunkMeshAsync(chunkPos, semaphore, cancellationToken);
                activeTasks.Add(meshTask);

                // Clean up completed tasks
                activeTasks.RemoveAll(t => t.IsCompleted);
            }

            // Wait for all remaining tasks to complete
            if (activeTasks.Count > 0)
            {
                await Task.WhenAll(activeTasks);
            }
        }
        catch (OperationCanceledException)
        {

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in mesh generation processor: {ex.Message}");
        }
        finally
        {
            semaphore?.Dispose();
        }
    }

    // Generates mesh for a single chunk asynchronously
    private async Task genChunkMeshAsync(ChunkPos pos, SemaphoreSlim semaphore, CancellationToken cancellationToken)
    {
        try
        {
            // Check if chunk still needs mesh generation
            if (!_Chunks.TryGetValue(pos, out var chunk) || chunk.MeshGenerated || !chunk.TerrainGenerated)
            {
                return;
            }

            // Generate mesh on background thread to avoid blocking the async context
            await Task.Run(() =>
            {
                if (!cancellationToken.IsCancellationRequested && _Chunks.TryGetValue(pos, out var currentChunk) && !currentChunk.MeshGenerated && currentChunk.TerrainGenerated)
                {
                    currentChunk.GenMesh(this);
                }
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating mesh for chunk {pos}: {ex.Message}");
        }
        finally
        {
            semaphore.Release();
        }
    }

    public void UploadPendingMeshes()
    {
        int uploadsThisFrame = 0;
        const int maxUploadsPerFrame = 2;
        
        // Get player position from current chunk position for distance-based prioritization
        Vector3 playerPos = new Vector3(
            VoxelGame.VoxelGame.init.CurrentChunkPosition.X * GameConstants.CHUNK_SIZE,
            GameConstants.CHUNK_HEIGHT / 2f,
            VoxelGame.VoxelGame.init.CurrentChunkPosition.Y * GameConstants.CHUNK_SIZE
        );
        
        // Create priority list of chunks to upload based on distance
        var chunksToUpload = new List<(Chunk chunk, float distance)>();
        
        foreach (var chunk in _Chunks.Values)
        {
            if (chunk.MeshGenerated && !chunk.MeshUploaded && chunk.TerrainGenerated && chunk.IsMeshReadyForUpload())
            {
                Vector3 chunkCenter = new Vector3(
                    chunk.Position.X * GameConstants.CHUNK_SIZE + GameConstants.CHUNK_SIZE / 2f,
                    GameConstants.CHUNK_HEIGHT / 2f,
                    chunk.Position.Z * GameConstants.CHUNK_SIZE + GameConstants.CHUNK_SIZE / 2f
                );
                
                float distance = (playerPos - chunkCenter).LengthSquared;
                chunksToUpload.Add((chunk, distance));
            }
        }
        
        // Sort by distance (closest first)
        chunksToUpload.Sort((a, b) => a.distance.CompareTo(b.distance));
        
        // Upload closest chunks first
        foreach (var (chunk, _) in chunksToUpload)
        {
            chunk.UploadMesh(this);
            uploadsThisFrame++;
            
            if (uploadsThisFrame >= maxUploadsPerFrame)
                break;
        }
    }

    public void RenderChunks(Vector3 cameraPosition, Vector3 cameraFront, Vector3 cameraUp, float fov, float aspectRatio)
    {
        Matrix4 view = Matrix4.LookAt(cameraPosition, cameraPosition + cameraFront, cameraUp);
        Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(fov), aspectRatio, 0.1f, 1000f);
        Matrix4 viewProjection = view * projection;
        mFrustum.Update(viewProjection);
        
        // Get player chunk position for distance-based culling
        ChunkPos playerChunk = new ChunkPos(
            (int)MathF.Floor(cameraPosition.X / GameConstants.CHUNK_SIZE),
            (int)MathF.Floor(cameraPosition.Z / GameConstants.CHUNK_SIZE)
        );
        
        // Create sorted list of chunks by distance for optimized rendering order
        var visibleChunks = new List<(Chunk chunk, float distance)>();
        
        foreach (var kvp in _Chunks)
        {
            var chunk = kvp.Value;
            if (!chunk.MeshUploaded || !chunk.TerrainGenerated)
                continue;
            
            // Distance check first (cheaper than frustum culling)
            int dx = kvp.Key.X - playerChunk.X;
            int dz = kvp.Key.Z - playerChunk.Z;
            float distanceSquared = dx * dx + dz * dz;
            
            if (distanceSquared > GameConstants.RENDER_DISTANCE * GameConstants.RENDER_DISTANCE)
                continue;

            if (!mFrustum.IsChunkVisible(kvp.Key))
                continue;
                
            visibleChunks.Add((chunk, distanceSquared));
        }
        
        // Sort by distance (front to back for early z-reject)
        visibleChunks.Sort((a, b) => a.distance.CompareTo(b.distance));
        
        // Render visible chunks in order
        foreach (var (chunk, _) in visibleChunks)
        {
            chunk.Render();
        }
    }

    public Chunk? GetChunk(ChunkPos position)
    {
        _Chunks.TryGetValue(position, out var chunk);
        return chunk;
    }

    
    // Immediately updates a block with optimized lighting to prevent black boxes.
    public bool immediateBlockUpdate(Vector3i worldPos, byte newBlockType, bool isBreaking = false)
    {
        var (chunk, localPos) = getChunkAndLocalPos(worldPos);

        if (chunk == null || !WorldPositionHelper.IsInChunkBounds(localPos))
            return false;

        byte oldBlock = chunk.Voxels[localPos.X, localPos.Y, localPos.Z];

        // Can't break bedrock
        if (oldBlock == BlockIDs.Bedrock && newBlockType != BlockIDs.Bedrock)
            return false;

        // Immediately update the block data
        chunk.Voxels[localPos.X, localPos.Y, localPos.Z] = newBlockType;
        chunk.Modified = true;

        // If placing a block, check if there's grass below that should turn to dirt. Only certain blocks should turn grass into dirt.
        if (!isBreaking && newBlockType != BlockIDs.Air && newBlockType != BlockIDs.Torch && newBlockType != BlockIDs.YellowFlower && newBlockType != BlockIDs.GrassTuff && newBlockType != BlockIDs.BrownMushroom && newBlockType != BlockIDs.RedMushroom) // TODO: Make less strict / hard coded
        {
            Vector3i posBelow = new Vector3i(worldPos.X, worldPos.Y - 1, worldPos.Z);
            var (chunkBelow, localPosBelow) = getChunkAndLocalPos(posBelow);
            
            if (chunkBelow != null && WorldPositionHelper.IsInChunkBounds(localPosBelow))
            {
                byte blockBelow = chunkBelow.Voxels[localPosBelow.X, localPosBelow.Y, localPosBelow.Z];
                if (blockBelow == BlockIDs.Grass)
                {
                    chunkBelow.Voxels[localPosBelow.X, localPosBelow.Y, localPosBelow.Z] = BlockIDs.Dirt;
                    chunkBelow.Modified = true;

                    if (chunkBelow != chunk)
                    {
                        _ = queueMeshGenAsync(WorldPositionHelper.WorldToChunkPos(posBelow));
                    }
                }
            }
        }

        // Play audio immediately
        if (isBreaking && oldBlock != BlockIDs.Air)
        {
            VoxelGame.VoxelGame.init.WorldAudioManager.PlayBlockBreakSound(
                BlockRegistry.GetBlock(oldBlock).Material);
        }

        // Apply fast lighting update
        var affectedChunks = fastLightingUpdate(worldPos, oldBlock, newBlockType);

        // Queue immediate mesh updates asynchronously instead of blocking
        _ = Task.Run(() =>
        {
            foreach (var affectedChunkPos in affectedChunks)
            {
                asyncChunkMesh(affectedChunkPos);
            }
        });

        // Queue proper lighting update for the tick system
        mTickSystem.QueueLightingUpdate(new LightingUpdate
        {
            Type = LightingUpdateType.BlockChanged,
            WorldPos = worldPos,
            ChunkPos = WorldPositionHelper.WorldToChunkPos(worldPos)
        });

        return true;
    }
    
    // Asynchronously regenerates a chunk's mesh without blocking the main thread.
    private void asyncChunkMesh(ChunkPos chunkPos)
    {
        if (_Chunks.TryGetValue(chunkPos, out var chunk))
        {
            // Queue for async mesh regeneration
            chunk.MeshGenerated = false;
            
            if (chunk.TerrainGenerated)
            {
                _ = queueMeshGenAsync(chunkPos);
            }
        }
    }
    
    // Fast lighting update that prevents black boxes
    private HashSet<ChunkPos> fastLightingUpdate(Vector3i worldPos, byte oldBlockType, byte newBlockType)
    {
        var affectedChunks = new HashSet<ChunkPos>();

        var (chunk, localPos) = getChunkAndLocalPos(worldPos);
        
        if (chunk == null || !chunk.IsInBounds(localPos)) 
            return affectedChunks;

        ChunkPos centerChunk = WorldPositionHelper.WorldToChunkPos(worldPos);
        affectedChunks.Add(centerChunk);

        if (isLightSource(oldBlockType) != isLightSource(newBlockType))
        {
            lightSourceChange(worldPos, oldBlockType, newBlockType, affectedChunks);
        }
        else if (oldBlockType != BlockIDs.Air && newBlockType == BlockIDs.Air)
        {
            propagateImmediateLightToPosition(worldPos, affectedChunks);
        }
        else if (oldBlockType == BlockIDs.Air && newBlockType != BlockIDs.Air)
        {
            blockPlacement(worldPos, newBlockType, affectedChunks);
        }

        return affectedChunks;
    }

    // Handles block placement lighting updates
    private void blockPlacement(Vector3i worldPos, byte newBlockType, HashSet<ChunkPos> affectedChunks)
    {
        var (chunk, localPos) = getChunkAndLocalPos(worldPos);
        if (chunk == null) return;

        // Set light levels for the new block
        if (isLightSource(newBlockType))
        {
            chunk.BlockLightLevels[localPos.X, localPos.Y, localPos.Z] = getBlockLight(newBlockType);
            chunk.SunlightLevels[localPos.X, localPos.Y, localPos.Z] = 0; // Block blocks sunlight
            
            // Propagate light from the new source
            propagateImmediateLightFromSource(worldPos, getBlockLight(newBlockType), affectedChunks);
        }
        else
        {
            // Regular block
            chunk.BlockLightLevels[localPos.X, localPos.Y, localPos.Z] = 0;
            chunk.SunlightLevels[localPos.X, localPos.Y, localPos.Z] = 0;
        }
    }

    private void lightSourceChange(Vector3i worldPos, byte oldBlockType, byte newBlockType, HashSet<ChunkPos> affectedChunks)
    {
        var (chunk, localPos) = getChunkAndLocalPos(worldPos);
        if (chunk == null) 
            return;

        if (isLightSource(newBlockType))
        {
            byte lightLevel = getBlockLight(newBlockType);
            chunk.BlockLightLevels[localPos.X, localPos.Y, localPos.Z] = lightLevel;
            propagateImmediateLightFromSource(worldPos, lightLevel, affectedChunks);
        }
        else if (isLightSource(oldBlockType))
        {
            chunk.BlockLightLevels[localPos.X, localPos.Y, localPos.Z] = 0;
            removeImmediateLight(worldPos, getBlockLight(oldBlockType), affectedChunks);
        }
    }

    private void propagateImmediateLightToPosition(Vector3i worldPos, HashSet<ChunkPos> affectedChunks)
    {
        var lightQueue = new Queue<(Vector3i pos, byte sunlight, byte blockLight)>();
        var processed = new HashSet<Vector3i>();
        
        // Initial light calculation for the broken block position
        var (chunk, localPos) = getChunkAndLocalPos(worldPos);
        if (chunk == null || !chunk.IsInBounds(localPos)) return;

        var (initialSun, initialBlock) = calcInitLight(worldPos);
        chunk.SunlightLevels[localPos.X, localPos.Y, localPos.Z] = initialSun;
        chunk.BlockLightLevels[localPos.X, localPos.Y, localPos.Z] = initialBlock;
        
        // Queue initial position if it has light to spread
        if (initialSun > 1 || initialBlock > 1)
        {
            lightQueue.Enqueue((worldPos, initialSun, initialBlock));
        }

        // Propagation with limited radius
        int maxSteps = 27;
        int steps = 0;

        while (lightQueue.Count > 0 && steps < maxSteps)
        {
            var (currentPos, sunlight, blockLight) = lightQueue.Dequeue();
            
            if (processed.Contains(currentPos)) continue;
            processed.Add(currentPos);
            steps++;

            // Propagate to neighbors
            propagateToNeighbors(currentPos, sunlight, blockLight, lightQueue, processed, affectedChunks);
        }
    }

    // Calculates the initial light level for a position by sampling neighbors
    private (byte sunlight, byte blockLight) calcInitLight(Vector3i worldPos)
    {
        byte bestSunlight = 0;
        byte bestBlockLight = 0;

        Vector3i[] directions = {
            new Vector3i(1, 0, 0), new Vector3i(-1, 0, 0),
            new Vector3i(0, 1, 0), new Vector3i(0, -1, 0),
            new Vector3i(0, 0, 1), new Vector3i(0, 0, -1)
        };

        foreach (var dir in directions)
        {
            Vector3i neighborPos = worldPos + dir;
            if (neighborPos.Y < 0 || neighborPos.Y >= GameConstants.CHUNK_HEIGHT) 
                continue;

            var (neighborChunk, neighborLocal) = getChunkAndLocalPos(neighborPos);

            if (neighborChunk == null || !neighborChunk.IsInBounds(neighborLocal)) 
                continue;

            byte neighborSunlight = neighborChunk.SunlightLevels[neighborLocal.X, neighborLocal.Y, neighborLocal.Z];
            byte neighborBlockLight = neighborChunk.BlockLightLevels[neighborLocal.X, neighborLocal.Y, neighborLocal.Z];

            if (dir.Y == -1 && neighborSunlight == 15)
            {
                bestSunlight = 15;
            }
            else if (neighborSunlight > 1)
            {
                bestSunlight = Math.Max(bestSunlight, (byte)(neighborSunlight - 1));
            }

            if (neighborBlockLight > 1)
            {
                bestBlockLight = Math.Max(bestBlockLight, (byte)(neighborBlockLight - 1));
            }
        }

        return (bestSunlight, bestBlockLight);
    }

    // Propagates light to neighboring positions
    private void propagateToNeighbors(Vector3i currentPos, byte sunlight, byte blockLight, Queue<(Vector3i, byte, byte)> lightQueue, HashSet<Vector3i> processed, HashSet<ChunkPos> affectedChunks)
    {
        Vector3i[] directions = {
            new Vector3i(1, 0, 0), new Vector3i(-1, 0, 0),
            new Vector3i(0, 1, 0), new Vector3i(0, -1, 0),
            new Vector3i(0, 0, 1), new Vector3i(0, 0, -1)
        };

        foreach (var dir in directions)
        {
            Vector3i neighborPos = currentPos + dir;

            if (neighborPos.Y < 0 || neighborPos.Y >= GameConstants.CHUNK_HEIGHT) 
                continue;

            if (processed.Contains(neighborPos)) 
                continue;

            var (neighborChunk, neighborLocal) = getChunkAndLocalPos(neighborPos);
            if (neighborChunk == null || !neighborChunk.IsInBounds(neighborLocal)) 
                continue;

            byte neighborBlockType = neighborChunk.Voxels[neighborLocal.X, neighborLocal.Y, neighborLocal.Z];
            if (neighborBlockType != BlockIDs.Air && !isTransparent(neighborBlockType)) 
                continue;

            // Calculate new light levels
            byte newSunlight = 0;
            byte newBlockLight = 0;

            if (dir.Y == -1 && sunlight == 15)
            {
                newSunlight = 15;
            }
            else if (sunlight > 1)
            {
                newSunlight = (byte)(sunlight - 1);
            }

            if (blockLight > 1)
            {
                newBlockLight = (byte)(blockLight - 1);
            }

            bool updated = false;
            if (newSunlight > neighborChunk.SunlightLevels[neighborLocal.X, neighborLocal.Y, neighborLocal.Z])
            {
                neighborChunk.SunlightLevels[neighborLocal.X, neighborLocal.Y, neighborLocal.Z] = newSunlight;
                updated = true;
            }
            if (newBlockLight > neighborChunk.BlockLightLevels[neighborLocal.X, neighborLocal.Y, neighborLocal.Z])
            {
                neighborChunk.BlockLightLevels[neighborLocal.X, neighborLocal.Y, neighborLocal.Z] = newBlockLight;
                updated = true;
            }

            if (updated)
            {
                affectedChunks.Add(WorldPositionHelper.WorldToChunkPos(neighborPos));
                lightQueue.Enqueue((neighborPos, newSunlight, newBlockLight));
            }
        }
    }

    private void propagateImmediateLightFromSource(Vector3i worldPos, byte lightLevel, HashSet<ChunkPos> affectedChunks)
    {
        var lightQueue = new Queue<(Vector3i pos, byte level)>();
        var processed = new HashSet<Vector3i>();
        
        lightQueue.Enqueue((worldPos, lightLevel));

        while (lightQueue.Count > 0)
        {
            var (currentPos, currentLevel) = lightQueue.Dequeue();
            
            if (processed.Contains(currentPos) || currentLevel <= 0) 
                continue;

            processed.Add(currentPos);

            Vector3i[] directions = {
                new Vector3i(1, 0, 0), new Vector3i(-1, 0, 0),
                new Vector3i(0, 1, 0), new Vector3i(0, -1, 0),
                new Vector3i(0, 0, 1), new Vector3i(0, 0, -1)
            };

            foreach (var dir in directions)
            {
                Vector3i neighborPos = currentPos + dir;
                if (neighborPos.Y < 0 || neighborPos.Y >= GameConstants.CHUNK_HEIGHT) 
                    continue;

                if (processed.Contains(neighborPos)) 
                    continue;

                var (neighborChunk, neighborLocal) = getChunkAndLocalPos(neighborPos);
                if (neighborChunk == null || !neighborChunk.IsInBounds(neighborLocal)) 
                    continue;

                byte neighborBlock = neighborChunk.Voxels[neighborLocal.X, neighborLocal.Y, neighborLocal.Z];
                if (neighborBlock != BlockIDs.Air && !isTransparent(neighborBlock)) 
                    continue;

                byte newLightLevel = (byte)Math.Max(0, currentLevel - 1);
                byte currentNeighborLight = neighborChunk.BlockLightLevels[neighborLocal.X, neighborLocal.Y, neighborLocal.Z];
                
                if (newLightLevel > currentNeighborLight)
                {
                    neighborChunk.BlockLightLevels[neighborLocal.X, neighborLocal.Y, neighborLocal.Z] = newLightLevel;
                    affectedChunks.Add(WorldPositionHelper.WorldToChunkPos(neighborPos));
                    
                    if (newLightLevel > 1)
                    {
                        lightQueue.Enqueue((neighborPos, newLightLevel));
                    }
                }
            }
        }
    }

    private void removeImmediateLight(Vector3i worldPos, byte oldLightLevel, HashSet<ChunkPos> affectedChunks)
    {
        var lightSourcesToProcess = new Queue<Vector3i>();
        var allLightRemoved = new HashSet<Vector3i>();
        var maxRadius = Math.Min(oldLightLevel + 2, 16);

        for (int dx = -maxRadius; dx <= maxRadius; dx++)
        {
            for (int dy = -maxRadius; dy <= maxRadius; dy++)
            {
                for (int dz = -maxRadius; dz <= maxRadius; dz++)
                {
                    Vector3i checkPos = worldPos + new Vector3i(dx, dy, dz);
                    if (checkPos.Y < 0 || checkPos.Y >= GameConstants.CHUNK_HEIGHT) 
                        continue;
                    
                    var (chunk, localPos) = getChunkAndLocalPos(checkPos);
                    if (chunk == null || !chunk.IsInBounds(localPos)) 
                        continue;
                    
                    byte currentLight = chunk.BlockLightLevels[localPos.X, localPos.Y, localPos.Z];
                    if (currentLight == 0) 
                        continue;
                    
                    byte blockType = chunk.Voxels[localPos.X, localPos.Y, localPos.Z];
                    
                    if (isLightSource(blockType) && checkPos != worldPos)
                    {
                        lightSourcesToProcess.Enqueue(checkPos);
                        continue;
                    }
                    
                    int manhattanDist = Math.Abs(dx) + Math.Abs(dy) + Math.Abs(dz);
                    byte expectedMaxLight = (byte)Math.Max(0, oldLightLevel - manhattanDist);
                    
                    if (currentLight <= expectedMaxLight || checkPos == worldPos)
                    {
                        chunk.BlockLightLevels[localPos.X, localPos.Y, localPos.Z] = 0;
                        allLightRemoved.Add(checkPos);
                        affectedChunks.Add(WorldPositionHelper.WorldToChunkPos(checkPos));
                    }
                }
            }
        }
        while (lightSourcesToProcess.Count > 0)
        {
            Vector3i lightSourcePos = lightSourcesToProcess.Dequeue();
            
            var (sourceChunk, sourceLocal) = getChunkAndLocalPos(lightSourcePos);
            if (sourceChunk == null || !sourceChunk.IsInBounds(sourceLocal)) 
                continue;
            
            byte sourceBlockType = sourceChunk.Voxels[sourceLocal.X, sourceLocal.Y, sourceLocal.Z];
            if (!isLightSource(sourceBlockType)) 
                continue;
            
            byte sourceLightLevel = getBlockLight(sourceBlockType);
            var propagationQueue = new Queue<(Vector3i pos, byte level)>();
            var propagationProcessed = new HashSet<Vector3i>();
            
            propagationQueue.Enqueue((lightSourcePos, sourceLightLevel));
            sourceChunk.BlockLightLevels[sourceLocal.X, sourceLocal.Y, sourceLocal.Z] = sourceLightLevel;
            
            while (propagationQueue.Count > 0)
            {
                var (currentPos, lightLevel) = propagationQueue.Dequeue();
                if (propagationProcessed.Contains(currentPos) || lightLevel <= 1) 
                    continue;

                propagationProcessed.Add(currentPos);
                
                Vector3i[] directions = {
                    new Vector3i(1, 0, 0), new Vector3i(-1, 0, 0),
                    new Vector3i(0, 1, 0), new Vector3i(0, -1, 0),
                    new Vector3i(0, 0, 1), new Vector3i(0, 0, -1)
                };
                
                foreach (var dir in directions)
                {
                    Vector3i neighborPos = currentPos + dir;
                    if (neighborPos.Y < 0 || neighborPos.Y >= GameConstants.CHUNK_HEIGHT) 
                        continue;

                    if (propagationProcessed.Contains(neighborPos)) 
                        continue;
                    
                    var (neighborChunk, neighborLocal) = getChunkAndLocalPos(neighborPos);
                    if (neighborChunk == null || !neighborChunk.IsInBounds(neighborLocal)) 
                        continue;
                    
                    byte neighborBlockType = neighborChunk.Voxels[neighborLocal.X, neighborLocal.Y, neighborLocal.Z];
                    if (neighborBlockType != BlockIDs.Air && !isTransparent(neighborBlockType)) 
                        continue;
                    
                    byte newLightLevel = (byte)Math.Max(0, lightLevel - 1);
                    byte currentNeighborLight = neighborChunk.BlockLightLevels[neighborLocal.X, neighborLocal.Y, neighborLocal.Z];
                    
                    if (newLightLevel > currentNeighborLight)
                    {
                        neighborChunk.BlockLightLevels[neighborLocal.X, neighborLocal.Y, neighborLocal.Z] = newLightLevel;
                        affectedChunks.Add(WorldPositionHelper.WorldToChunkPos(neighborPos));
                        
                        if (newLightLevel > 1)
                        {
                            propagationQueue.Enqueue((neighborPos, newLightLevel));
                        }
                    }
                }
            }
        }
    }

    private bool isLightSource(byte blockType)
    {
        return BlockRegistry.GetBlock(blockType).LightLevel > 0;
    }

    private byte getBlockLight(byte blockType)
    {
        return BlockRegistry.GetBlock(blockType).LightLevel;
    }

    private bool isTransparent(byte blockType)
    {
        if (blockType == BlockIDs.Air) return true;
        return BlockRegistry.GetBlock(blockType)?.Transparent ?? false;
    }

    private (Chunk chunk, Vector3i localPos) getChunkAndLocalPos(Vector3i worldPos)
    {
        ChunkPos chunkPos = new ChunkPos(
            (int)Math.Floor(worldPos.X / (float)GameConstants.CHUNK_SIZE),
            (int)Math.Floor(worldPos.Z / (float)GameConstants.CHUNK_SIZE)
        );

        if (!_Chunks.TryGetValue(chunkPos, out var chunk))
            return (null, Vector3i.Zero);

        Vector3i localPos = new Vector3i(
            worldPos.X - chunkPos.X * GameConstants.CHUNK_SIZE,
            worldPos.Y,
            worldPos.Z - chunkPos.Z * GameConstants.CHUNK_SIZE
        );

        // Handle negative coordinates
        if (localPos.X < 0) 
            localPos.X += GameConstants.CHUNK_SIZE;

        if (localPos.Z < 0) 
            localPos.Z += GameConstants.CHUNK_SIZE;

        return (chunk, localPos);
    }

    public string GetTickSystemStats()
    {
        return $"TPS: {mTickSystem.CurrentTPS}/{TARGET_TPS} | Avg: {mTickSystem.AverageTickTime:F2}ms | Ticks: {mTickSystem.TickCount}";
    }

    public long GetCurrentTick() => mTickSystem.TickCount;

    // Gets diagnostic information about the async mesh generation system
    public string GetMeshGenerationStats()
    {
        var queuedCount = 0;
        if (_mMeshQueueLock.Wait(0))
        {
            try
            {
                queuedCount = _mQueuedMeshChunks.Count;
            }
            finally
            {
                _mMeshQueueLock.Release();
            }
        }

        var taskStatus = _mMeshGenerationTask?.Status.ToString() ?? "Not Started";
        var channelCount = _mMeshReader.Count;
        
        return $"Mesh Gen: Queued={queuedCount}, Channel={channelCount}, Task={taskStatus}";
    }

    #region Spiral Chunk Loading
    private List<ChunkPos> getDistanceSortedChunksPos(ChunkPos centerChunk, int renderDistance)
    {
        var chunkPositions = new List<(ChunkPos pos, float distance)>();

        for (int x = centerChunk.X - renderDistance; x <= centerChunk.X + renderDistance; x++)
        {
            for (int z = centerChunk.Z - renderDistance; z <= centerChunk.Z + renderDistance; z++)
            {
                ChunkPos pos = new ChunkPos(x, z);

                float dx = x - centerChunk.X;
                float dz = z - centerChunk.Z;
                float distance = MathF.Sqrt(dx * dx + dz * dz);

                if (distance <= renderDistance)
                {
                    chunkPositions.Add((pos, distance));
                }
            }
        }

        chunkPositions.Sort((a, b) => a.distance.CompareTo(b.distance));

        var sortedPositions = new List<ChunkPos>();
        foreach (var (pos, _) in chunkPositions)
        {
            sortedPositions.Add(pos);
        }
        
        return sortedPositions;
    }
    #endregion

    public void Dispose()
    {
        mTickSystem?.Stop();
        mTickSystem?.Dispose();

        _mCancellationTokenSource.Cancel();
        _mMeshWriter.Complete();

        try
        {
            if (!_mMeshGenerationTask.Wait(2000))
            {
                Console.WriteLine("Mesh generation task did not complete in time, force disposal");
            }
        }
        catch (AggregateException ex)
        {
            var nonCancellationExceptions = ex.InnerExceptions
                .Where(e => !(e is OperationCanceledException))
                .ToList();

            if (nonCancellationExceptions.Any())
            {
                Console.WriteLine($"Error waiting for mesh generation task: {string.Join(", ", nonCancellationExceptions.Select(e => e.Message))}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error waiting for mesh generation task: {ex.Message}");
        }

        mLightingEngine?.ClearCache();

        try
        {
            _mMeshQueueLock.Wait(100);
            _mQueuedMeshChunks.Clear();
        }
        finally
        {
            _mMeshQueueLock.Release();
        }

        // Dispose all chunks
        var chunksToDispose = _Chunks.Values.ToList();
        foreach (var chunk in chunksToDispose)
        {
            try
            {
                chunk.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disposing chunk: {ex.Message}");
            }
        }

        _Chunks.Clear();

        while (_mVertexListPool.TryDequeue(out var vertexList))
        {
            vertexList.Clear();
            vertexList.TrimExcess(); // Release internal array capacity
        }
        while (_mIndexListPool.TryDequeue(out var indexList))
        {
            indexList.Clear();
            indexList.TrimExcess(); // Release internal array capacity
        }

        try
        {
            _mMeshQueueLock?.Dispose();
            _mCancellationTokenSource?.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error disposing resources: {ex.Message}");
        }

        GC.Collect(2, GCCollectionMode.Aggressive, true, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Aggressive, true, true);
    }
}