// Main chunk manager, uses some multi-threading. Manages all the loaded chunks, has stuff in place for unloading chunks although they are never really used | DA | 9/4/25
using DuncanCraft.Utils;
using System.Collections.Concurrent;

namespace DuncanCraft.World;

public class ChunkManager
{
    private readonly ConcurrentDictionary<long, Chunk> _mChunks;
    private readonly ConcurrentQueue<Chunk> _mGenerateQueue;
    private readonly ConcurrentQueue<Chunk> _mUnloadQueue;
    private readonly SemaphoreSlim _mGenerationSemaphore;
    private readonly CancellationTokenSource _mCancellationTokenSource;
    private readonly List<Task> _mGenerationTasks;

    private GameWorld? mWorld;
    private Texture? mTextureAtlas;
    
    public const int WORLD_SIZE_CHUNKS = 16; // 16x16 = 256 chunks total, 256x256 block world
    
    public ChunkManager()
    {
        _mChunks = new ConcurrentDictionary<long, Chunk>();
        _mGenerateQueue = new ConcurrentQueue<Chunk>();
        _mUnloadQueue = new ConcurrentQueue<Chunk>();
        _mGenerationSemaphore = new SemaphoreSlim(Environment.ProcessorCount);
        _mCancellationTokenSource = new CancellationTokenSource();
        _mGenerationTasks = new List<Task>();
    }
    
    public void SetWorld(GameWorld world)
    {
        mWorld = world;
    }
    
    public GameWorld? GetWorld()
    {
        return mWorld;
    }
    
    public async Task InitializeAsync()
    {
        Console.WriteLine("Initializing chunk manager...");
        
        for (int x = 0; x < WORLD_SIZE_CHUNKS; x++)
        {
            for (int z = 0; z < WORLD_SIZE_CHUNKS; z++)
            {
                var chunk = new Chunk(x, z);
                var key = getChunkKey(x, z);
                _mChunks.TryAdd(key, chunk);
                _mGenerateQueue.Enqueue(chunk);
            }
        }
        
        startGenTasks();
        
        await waitForInitGeneration();

        stopGenTasks();
    }
    
    private void startGenTasks()
    {
        int taskCount = Math.Min(Environment.ProcessorCount, 4);
        
        for (int i = 0; i < taskCount; i++)
        {
            var task = Task.Run(chunkGenerationWorker, _mCancellationTokenSource.Token);
            _mGenerationTasks.Add(task);
        }
    }
    
    private void stopGenTasks()
    {
        Console.WriteLine("Stopping chunk generation tasks...");
        _mCancellationTokenSource.Cancel();
        
        try
        {
            Task.WaitAll(_mGenerationTasks.ToArray(), TimeSpan.FromSeconds(10));
            Console.WriteLine("All chunk generation tasks stopped successfully.");
        }
        catch (AggregateException ex)
        {
            Console.WriteLine($"Some generation tasks were cancelled (expected): {ex.InnerExceptions.Count} exceptions");
        }
        
        _mGenerationTasks.Clear();
    }
    
    public void GenAllMeshes(Texture textureAtlas)
    {
        mTextureAtlas = textureAtlas;
        
        int count = 0;
        int skipped = 0;
        
        foreach (var chunk in _mChunks.Values)
        {
            if (chunk.State == ChunkState.Generated)
            {
                var mesh = chunk.GenerateMesh(this, textureAtlas);
                chunk.SetMesh(mesh);
                count++;
            }
            else
            {
                skipped++;
                if (skipped <= 10)
                {
                    Console.WriteLine($"WARNING: Chunk ({chunk.X}, {chunk.Z}) not generated (State: {chunk.State})");
                }
            }
        }
        
    }
    
    public void RegenChunkMesh(Chunk chunk)
    {
        if (mTextureAtlas != null && chunk.State == ChunkState.Generated)
        {
            var mesh = chunk.GenerateMesh(this, mTextureAtlas);
            chunk.SetMesh(mesh);
        }
    }
    
    private async Task chunkGenerationWorker()
    {
        while (!_mCancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                if (_mGenerateQueue.TryDequeue(out var chunk))
                {
                    await _mGenerationSemaphore.WaitAsync(_mCancellationTokenSource.Token);
                    
                    try
                    {
                        await chunk.GenerateAsync();
                        
                        // Init lighting for the newly generated chunk
                        if (mWorld?.LightingEngine != null)
                        {
                            mWorld.LightingEngine.InitChunkLighting(chunk);
                        }
                    }
                    finally
                    {
                        _mGenerationSemaphore.Release();
                    }
                }
                else
                {
                    await Task.Delay(100, _mCancellationTokenSource.Token);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating chunk: {ex.Message}");
            }
        }
    }

    private async Task waitForInitGeneration()
    {
        var timeout = TimeSpan.FromMinutes(5);
        var startTime = DateTime.UtcNow;
        
        while (DateTime.UtcNow - startTime < timeout)
        {
            bool allGenerated = true;
            int generatedCount = 0;
            int generatingCount = 0;
            int emptyCount = 0;
            
            foreach (var chunk in _mChunks.Values)
            {
                switch (chunk.State)
                {
                    case ChunkState.Generated:
                        generatedCount++;
                        break;
                    case ChunkState.Generating:
                        generatingCount++;
                        allGenerated = false;
                        break;
                    case ChunkState.Empty:
                        emptyCount++;
                        allGenerated = false;
                        break;
                    default:
                        allGenerated = false;
                        break;
                }
            }
            
            
            if (allGenerated)
            {
                return;
            }
            
            await Task.Delay(100);
        }

        int finalGenerated = _mChunks.Values.Count(c => c.State == ChunkState.Generated);
    }
    
    public Chunk? GetChunk(int chunkX, int chunkZ)
    {
        var key = getChunkKey(chunkX, chunkZ);
        _mChunks.TryGetValue(key, out var chunk);
        return chunk;
    }
    
    public byte GetBlock(int worldX, int worldY, int worldZ)
    {
        if (worldY < 0 || worldY >= GameConstants.CHUNK_HEIGHT)
            return BlockIDs.Air;
            
        int chunkX = worldX >> 4; // worldX / 16
        int chunkZ = worldZ >> 4; // worldZ / 16
        
        var chunk = GetChunk(chunkX, chunkZ);
        if (chunk == null) 
            return BlockIDs.Air;
        
        int localX = worldX & 15; // worldX % 16
        int localZ = worldZ & 15; // worldZ % 16
        
        return chunk.GetBlock(localX, worldY, localZ);
    }
    
    public void SetBlock(int worldX, int worldY, int worldZ, byte block)
    {
        if (worldY < 0 || worldY >= GameConstants.CHUNK_HEIGHT)
            return;
            
        int chunkX = worldX >> 4;
        int chunkZ = worldZ >> 4;
        
        var chunk = GetChunk(chunkX, chunkZ);
        if (chunk == null) 
            return;
        
        int localX = worldX & 15;
        int localZ = worldZ & 15;
        
        // Get the old block before changing it
        var oldBlock = chunk.GetBlock(localX, worldY, localZ);
        
        chunk.SetBlock(localX, worldY, localZ, block);
        chunk.MarkForRebuild();
        
        markNeighborForRebuild(chunkX, chunkZ, localX, worldY, localZ);
    }
    
    private void markNeighborForRebuild(int chunkX, int chunkZ, int localX, int y, int localZ)
    {
        var neighbors = new List<Chunk>();
        
        if (localX == 0)
        {
            var chunk = GetChunk(chunkX - 1, chunkZ);
            if (chunk != null) neighbors.Add(chunk);
        }
        if (localX == GameConstants.CHUNK_SIZE - 1)
        {
            var chunk = GetChunk(chunkX + 1, chunkZ);
            if (chunk != null) neighbors.Add(chunk);
        }
        if (localZ == 0)
        {
            var chunk = GetChunk(chunkX, chunkZ - 1);
            if (chunk != null) neighbors.Add(chunk);
        }
        if (localZ == GameConstants.CHUNK_SIZE - 1)
        {
            var chunk = GetChunk(chunkX, chunkZ + 1);
            if (chunk != null) neighbors.Add(chunk);
        }

        foreach (var neighbor in neighbors)
        {
            neighbor.MarkForRebuild();
        }
    }
    
    public IEnumerable<Chunk> GetLoadedChunks() => _mChunks.Values.Where(c => c.State == ChunkState.Generated);

    public Task TickAsync()
    {
        processUnloadQueue();
        return Task.CompletedTask;
    }
    
    private void processUnloadQueue()
    {
        int processedCount = 0;
        while (processedCount < 10 && _mUnloadQueue.TryDequeue(out var chunk))
        {
            var key = getChunkKey(chunk.X, chunk.Z);
            _mChunks.TryRemove(key, out _);
            processedCount++;
        }
    }
    
    public void Dispose()
    {
        _mCancellationTokenSource.Cancel();
        
        try
        {
            Task.WaitAll(_mGenerationTasks.ToArray(), TimeSpan.FromSeconds(5));
        }
        catch (AggregateException)
        {
        }
        
        _mCancellationTokenSource.Dispose();
        _mGenerationSemaphore.Dispose();
    }
    
    private long getChunkKey(int x, int z)
    {
        return (long)x << 32 | (uint)z;
    }
}