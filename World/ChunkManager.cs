using OpenTK.Mathematics;
using System.Collections.Concurrent;
using VoxelGame.Saving;
using VoxelGame.Utils;
using VoxelGame.World;

public class ChunkManager : IDisposable
{
    public readonly ConcurrentDictionary<ChunkPos, Chunk> _chunks = new();
    private readonly ConcurrentQueue<ChunkPos> meshGenerationQueue = new();
    private readonly HashSet<ChunkPos> queuedChunks = new();
    private readonly object queueLock = new object();
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly Task meshGenerationTask;

    private readonly HashSet<ChunkPos> chunksNeedingMeshUpdate = new();
    private readonly object meshUpdateLock = new object();

    private readonly ConcurrentQueue<List<Vertex>> vertexListPool = new();
    private readonly ConcurrentQueue<List<uint>> indexListPool = new();
    private const int MAX_POOLED_OBJECTS = 50;

    public Frustum frustum;

    public ChunkManager()
    {
        frustum = new Frustum();

        for (int i = 0; i < 20; i++)
        {
            vertexListPool.Enqueue(new List<Vertex>());
            indexListPool.Enqueue(new List<uint>());
        }

        meshGenerationTask = Task.Run(meshGenWorker, cancellationTokenSource.Token);
    }

    public List<Vertex> GetVertexList()
    {
        if (vertexListPool.TryDequeue(out var list))
        {
            list.Clear();
            return list;
        }
        return new List<Vertex>();
    }

    public List<uint> GetIndexList()
    {
        if (indexListPool.TryDequeue(out var list))
        {
            list.Clear();
            return list;
        }
        return new List<uint>();
    }

    public void ReturnVertexList(List<Vertex> list)
    {
        if (list != null && vertexListPool.Count < MAX_POOLED_OBJECTS)
        {
            list.Clear();
            list.TrimExcess();
            vertexListPool.Enqueue(list);
        }
    }

    public void ReturnIndexList(List<uint> list)
    {
        if (list != null && indexListPool.Count < MAX_POOLED_OBJECTS)
        {
            list.Clear();
            list.TrimExcess();
            indexListPool.Enqueue(list);
        }
    }

    public void UpdateChunks(Vector3 playerPos)
    {
        ChunkPos playerChunk = new ChunkPos(
            (int)MathF.Floor(playerPos.X / Constants.CHUNK_SIZE),
            (int)MathF.Floor(playerPos.Z / Constants.CHUNK_SIZE)
        );

        VoxelGame.VoxelGame.init.currentChunkPosition = new OpenTK.Mathematics.Vector2i(playerChunk.X, playerChunk.Z);

        var newChunks = new List<ChunkPos>();
        for (int x = playerChunk.X - Constants.RENDER_DISTANCE; x <= playerChunk.X + Constants.RENDER_DISTANCE; x++)
        {
            for (int z = playerChunk.Z - Constants.RENDER_DISTANCE; z <= playerChunk.Z + Constants.RENDER_DISTANCE; z++)
            {
                ChunkPos pos = new ChunkPos(x, z);

                if (!_chunks.ContainsKey(pos))
                {
                    _chunks[pos] = new Chunk(pos);
                    newChunks.Add(pos);
                    Serialization.Load(_chunks[pos]);
                    enqueueMeshGeneration(pos);
                }
            }
        }

        foreach (var newChunkPos in newChunks)
        {
            ReGenChunkNeighbors(newChunkPos);
        }

        lock (meshUpdateLock)
        {
            foreach (var chunkPos in chunksNeedingMeshUpdate)
            {
                if (_chunks.TryGetValue(chunkPos, out var chunk))
                {
                    chunk.MeshGenerated = false;
                    enqueueMeshGeneration(chunkPos);
                }
            }
            chunksNeedingMeshUpdate.Clear();
        }

        foreach (var kvp in _chunks.ToList())
        {
            if (!kvp.Value.MeshGenerated)
            {
                enqueueMeshGeneration(kvp.Key);
            }
        }

        // IProcess all chunks to remove at once
        var chunksToRemove = new List<ChunkPos>();
        foreach (var kvp in _chunks)
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
            if (_chunks.TryRemove(pos, out var chunk))
            {
                if (chunk.Modified)
                    Serialization.SaveChunk(chunk);

                lock (queueLock)
                {
                    queuedChunks.Remove(pos);
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

    private void enqueueMeshGeneration(ChunkPos pos)
    {
        lock (queueLock)
        {
            if (!queuedChunks.Contains(pos))
            {
                queuedChunks.Add(pos);
                meshGenerationQueue.Enqueue(pos);
            }
        }
    }

    public void MarkChunksForUpdate(IEnumerable<ChunkPos> chunkPositions)
    {
        lock (meshUpdateLock)
        {
            foreach (var pos in chunkPositions)
            {
                chunksNeedingMeshUpdate.Add(pos);
            }
        }
    }

    private async Task meshGenWorker()
    {
        while (!cancellationTokenSource.Token.IsCancellationRequested)
        {
            if (meshGenerationQueue.TryDequeue(out ChunkPos pos))
            {
                lock (queueLock)
                {
                    queuedChunks.Remove(pos);
                }

                if (_chunks.TryGetValue(pos, out var chunk) && !chunk.MeshGenerated)
                {
                    chunk.GenMesh(this);
                }
            }
            else
            {
                await Task.Delay(10, cancellationTokenSource.Token);
            }
        }
    }

    public void UploadPendingMeshes()
    {
        foreach (var chunk in _chunks.Values)
        {
            if (chunk.MeshGenerated && !chunk.MeshUploaded)
            {
                chunk.UploadMesh(this);
            }
        }
    }

    public void RenderChunks(Vector3 cameraPosition, Vector3 cameraFront, Vector3 cameraUp, float fov, float aspectRatio)
    {
        float nearPlane = 0.1f;
        float farPlane = 1000f;
        frustum.UpdateFromCamera(cameraPosition, cameraFront, cameraUp, fov, aspectRatio, nearPlane, farPlane);

        foreach (var kvp in _chunks)
        {
            if (kvp.Value.MeshUploaded && kvp.Value.IsInFrustum(frustum))
            {
                kvp.Value.Render();
            }
        }
    }

    public Chunk GetChunk(ChunkPos position)
    {
        _chunks.TryGetValue(position, out var chunk);
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
            if (_chunks.TryGetValue(neighborPos, out var neighborChunk) && neighborChunk.MeshGenerated)
            {
                neighborChunk.MeshGenerated = false;
            }
        }
    }

    public void Dispose()
    {
        cancellationTokenSource.Cancel();

        try
        {
            if (!meshGenerationTask.Wait(1000))
            {
                Console.WriteLine("Mesh generation task did not complete in time, forcing disposal");
            }
        }
        catch (AggregateException ex) when (ex.InnerException is TaskCanceledException)
        {
            // Expected when cancelling
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error waiting for mesh generation task: {ex.Message}");
        }

        // Dispose chunks on main thread
        var chunksToDispose = _chunks.Values.ToList();
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

        _chunks.Clear();

        lock (queueLock)
        {
            queuedChunks.Clear();
            while (meshGenerationQueue.TryDequeue(out _)) { }
        }

        // Clear object pools
        while (vertexListPool.TryDequeue(out var vertexList))
        {
            vertexList.Clear();
        }
        while (indexListPool.TryDequeue(out var indexList))
        {
            indexList.Clear();
        }

        try
        {
            cancellationTokenSource.Dispose();
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