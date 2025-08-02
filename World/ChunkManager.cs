// This is the main chunk manager of the game, making sure that new chunks or chunks that have been modified are generated or re-generated. It uses some threading which I am still not 100% on. But it works. | DA | 8/1/2025
using OpenTK.Mathematics;
using System.Collections.Concurrent;
using VoxelGame.Saving;
using VoxelGame.Utils;
using VoxelGame.World;

public class ChunkManager : IDisposable
{
    public readonly ConcurrentDictionary<ChunkPos, Chunk> _Chunks = new();

    private readonly ConcurrentQueue<ChunkPos> _mCeshGenerationQueue = new();
    private readonly ConcurrentQueue<ChunkPos> _mPriorityMeshQueue = new();
    private readonly ConcurrentQueue<ChunkPos> _mTerrainGenerationQueue = new();

    private readonly HashSet<ChunkPos> _mQueuedChunks = new();
    private readonly HashSet<ChunkPos> _mQueuedTerrainChunks = new();

    private readonly object _mQueueLock = new object();

    private readonly CancellationTokenSource _mCancellationTokenSource = new();

    private readonly Task[] _mMeshGenerationTasks;
    private readonly Task[] _mTerrainGenerationTasks;

    private readonly HashSet<ChunkPos> _ChunksNeedingMeshUpdate = new();

    private readonly object _mMeshUpdateLock = new object();

    private readonly ConcurrentQueue<List<Vertex>> _mVertexListPool = new();
    private readonly ConcurrentQueue<List<uint>> _mIndexListPool = new();

    private const int MAX_POOLED_OBJECTS = 50;
    private const int WORKER_THREAD_COUNT = 2;
    private const int TERRAIN_THREAD_COUNT = 2;

    public Frustum ChunkManagerFrustum;

    public ChunkManager()
    {
        ChunkManagerFrustum = new Frustum();

        for (int i = 0; i < 20; i++)
        {
            _mVertexListPool.Enqueue(new List<Vertex>());
            _mIndexListPool.Enqueue(new List<uint>());
        }

        _mMeshGenerationTasks = new Task[WORKER_THREAD_COUNT];
        for (int i = 0; i < WORKER_THREAD_COUNT; i++)
        {
            _mMeshGenerationTasks[i] = Task.Run(meshGenWorker, _mCancellationTokenSource.Token);
        }

        _mTerrainGenerationTasks = new Task[TERRAIN_THREAD_COUNT];
        for (int i = 0; i < TERRAIN_THREAD_COUNT; i++)
        {
            _mTerrainGenerationTasks[i] = Task.Run(terrainGenWorker, _mCancellationTokenSource.Token);
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
            (int)MathF.Floor(playerPos.X / Constants.CHUNK_SIZE),
            (int)MathF.Floor(playerPos.Z / Constants.CHUNK_SIZE)
        );

        VoxelGame.VoxelGame.init.CurrentChunkPosition = new OpenTK.Mathematics.Vector2i(playerChunk.X, playerChunk.Z);

        var newChunks = new List<ChunkPos>();
        for (int x = playerChunk.X - Constants.RENDER_DISTANCE; x <= playerChunk.X + Constants.RENDER_DISTANCE; x++)
        {
            for (int z = playerChunk.Z - Constants.RENDER_DISTANCE; z <= playerChunk.Z + Constants.RENDER_DISTANCE; z++)
            {
                ChunkPos pos = new ChunkPos(x, z);

                if (!_Chunks.ContainsKey(pos))
                {
                    _Chunks[pos] = new Chunk(pos, false);
                    newChunks.Add(pos);

                    if (!Serialization.Load(_Chunks[pos]))
                    {
                        //queueTerrainGeneration(pos);

                        lock (_mQueueLock)
                        {
                            if (!_mQueuedTerrainChunks.Contains(pos))
                            {
                                _mQueuedTerrainChunks.Add(pos);
                                _mTerrainGenerationQueue.Enqueue(pos);
                            }
                        }
                    }
                    else
                    {
                        queueMeshGen(pos);
                    }
                }
            }
        }

        // Re-gen chunks around new chunks to get rid of chunk boarders
        foreach (var newChunkPos in newChunks)
        {
            ReGenChunkNeighbors(newChunkPos);
        }

        // Process priority
        lock (_mMeshUpdateLock)
        {
            foreach (var chunkPos in _ChunksNeedingMeshUpdate)
            {
                if (_Chunks.TryGetValue(chunkPos, out var chunk))
                {
                    chunk.MeshGenerated = false;
                    //queuePriorityMesh(chunkPos);

                    lock (_mQueueLock)
                    {
                        if (!_mQueuedChunks.Contains(chunkPos))
                        {
                            _mQueuedChunks.Add(chunkPos);
                            _mPriorityMeshQueue.Enqueue(chunkPos);
                        }
                    }
                }
            }
            _ChunksNeedingMeshUpdate.Clear();
        }

        // Add the chunks to the queue, if their mesh is not generated
        foreach (var kvp in _Chunks.ToList())
        {
            if (!kvp.Value.MeshGenerated && kvp.Value.TerrainGenerated)
            {
                queueMeshGen(kvp.Key);
            }
        }

        // Process all chunks to remove at once
        var chunksToRemove = new List<ChunkPos>();
        foreach (var kvp in _Chunks)
        {
            int dx = kvp.Key.X - playerChunk.X;
            int dz = kvp.Key.Z - playerChunk.Z;

            if (Math.Abs(dx) > Constants.RENDER_DISTANCE + 1 || Math.Abs(dz) > Constants.RENDER_DISTANCE + 1)
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

                lock (_mQueueLock)
                {
                    _mQueuedChunks.Remove(pos);
                    _mQueuedTerrainChunks.Remove(pos);
                }

                chunk.Dispose();
            }
        }

        // If there are a lot of chunks to remove, then force garbage collection
        if (chunksToRemove.Count > 5)
        {
            GC.Collect(0, GCCollectionMode.Optimized);
        }
    }

    private void queueMeshGen(ChunkPos pos)
    {
        lock (_mQueueLock)
        {
            if (!_mQueuedChunks.Contains(pos))
            {
                _mQueuedChunks.Add(pos);
                _mCeshGenerationQueue.Enqueue(pos);
            }
        }
    }

    public void MarkChunksForUpdate(IEnumerable<ChunkPos> chunkPositions)
    {
        lock (_mMeshUpdateLock)
        {
            foreach (var pos in chunkPositions)
            {
                _ChunksNeedingMeshUpdate.Add(pos);
            }
        }
    }

    private async Task terrainGenWorker()
    {
        while (!_mCancellationTokenSource.Token.IsCancellationRequested)
        {
            ChunkPos pos;
            bool foundWork = false;

            if (_mTerrainGenerationQueue.TryDequeue(out pos))
            {
                foundWork = true;

                lock (_mQueueLock)
                {
                    _mQueuedTerrainChunks.Remove(pos);
                }

                if (_Chunks.TryGetValue(pos, out var chunk) && !chunk.TerrainGenerated)
                {
                    VoxelGame.VoxelGame.init.TerrainGen.GenerateTerrain(chunk);
                    chunk.TerrainGenerated = true;

                    queueMeshGen(pos);
                }
            }

            if (!foundWork)
            {
                await Task.Delay(10, _mCancellationTokenSource.Token);
            }
        }
    }

    private async Task meshGenWorker()
    {
        while (!_mCancellationTokenSource.Token.IsCancellationRequested)
        {
            ChunkPos pos;
            bool foundWork = false;

            // Priority queue
            if (_mPriorityMeshQueue.TryDequeue(out pos))
            {
                foundWork = true;
            }
            // Regular queue
            else if (_mCeshGenerationQueue.TryDequeue(out pos))
            {
                foundWork = true;
            }

            if (foundWork)
            {
                lock (_mQueueLock)
                {
                    _mQueuedChunks.Remove(pos);
                }

                if (_Chunks.TryGetValue(pos, out var chunk) && !chunk.MeshGenerated && chunk.TerrainGenerated)
                {
                    chunk.GenMesh(this);
                }
            }
            else
            {
                await Task.Delay(5, _mCancellationTokenSource.Token);
            }
        }
    }

    public void UploadPendingMeshes()
    {
        int uploadsThisFrame = 0;
        const int maxUploadsPerFrame = 3;

        foreach (var chunk in _Chunks.Values)
        {
            if (chunk.MeshGenerated && !chunk.MeshUploaded && chunk.TerrainGenerated)
            {
                chunk.UploadMesh(this);
                uploadsThisFrame++;

                if (uploadsThisFrame >= maxUploadsPerFrame)
                    break;
            }
        }
    }

    public void RenderChunks(Vector3 cameraPosition, Vector3 cameraFront, Vector3 cameraUp, float fov, float aspectRatio)
    {
        float nearPlane = 0.1f;
        float farPlane = 1000f;
        ChunkManagerFrustum.UpdateFromCamera(cameraPosition, cameraFront, cameraUp, fov, aspectRatio, nearPlane, farPlane);

        foreach (var kvp in _Chunks)
        {
            if (kvp.Value.MeshUploaded && kvp.Value.TerrainGenerated && kvp.Value.IsInFrustum(ChunkManagerFrustum))
            {
                kvp.Value.Render();
            }
        }
    }

    public Chunk GetChunk(ChunkPos position)
    {
        _Chunks.TryGetValue(position, out var chunk);
        return chunk;
    }

    public void ReGenChunkNeighbors(ChunkPos position)
    {
        var neighbors = new ChunkPos[]
        {
            new ChunkPos(position.X - 1, position.Z),
            new ChunkPos(position.X + 1, position.Z),
            new ChunkPos(position.X, position.Z - 1),
            new ChunkPos(position.X, position.Z + 1)
        };

        foreach (var neighborPos in neighbors)
        {
            if (_Chunks.TryGetValue(neighborPos, out var neighborChunk) && neighborChunk.MeshGenerated)
            {
                neighborChunk.MeshGenerated = false;
            }
        }
    }

    public void Dispose()
    {
        _mCancellationTokenSource.Cancel();

        try
        {
            var allTasks = _mMeshGenerationTasks.Concat(_mTerrainGenerationTasks).ToArray();

            if (!Task.WaitAll(allTasks, 1000))
            {
                Console.WriteLine("Worker tasks did not complete in time, force disposal");
            }
        }
        catch (AggregateException ex)
        {
            var nonCancellationExceptions = ex.InnerExceptions
                .Where(e => !(e is OperationCanceledException))
                .ToList();

            if (nonCancellationExceptions.Any())
            {
                Console.WriteLine($"Error waiting for worker tasks: {string.Join(", ", nonCancellationExceptions.Select(e => e.Message))}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error waiting for worker tasks: {ex.Message}");
        }

        // Dispose chunks on main thread
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

        lock (_mQueueLock)
        {
            _mQueuedChunks.Clear();
            _mQueuedTerrainChunks.Clear();
            while (_mCeshGenerationQueue.TryDequeue(out _)) { }
            while (_mPriorityMeshQueue.TryDequeue(out _)) { }
            while (_mTerrainGenerationQueue.TryDequeue(out _)) { }
        }

        // Clear object pools
        while (_mVertexListPool.TryDequeue(out var vertexList))
        {
            vertexList.Clear();
        }
        while (_mIndexListPool.TryDequeue(out var indexList))
        {
            indexList.Clear();
        }

        try
        {
            _mCancellationTokenSource.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error disposing cancellation token: {ex.Message}");
        }

        // Force garbage collection one final time
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}