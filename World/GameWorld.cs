// Main world script, handles stuff like making the world and has a lot of helper functions for getting blocks or setting blocks in chunks
using DuncanCraft.Utils;
using DuncanCraft.Lighting;
using DuncanCraft.Blocks;
using System;

namespace DuncanCraft.World;

public class GameWorld : IDisposable
{
    private readonly ChunkManager _mChunkManager;
    private readonly LightingEngine _mCightingEngine;

    public const int WORLD_WIDTH = 256; // 16 * 16
    public const int WORLD_DEPTH = 256; // 16 * 16
    public const int WORLD_HEIGHT = GameConstants.CHUNK_HEIGHT;

    private const int RANDOM_TICKS_PER_CHUNK = 3;

    private bool mIsInitializing = true;
    private readonly Random _mRandom = new Random();

    public ChunkManager ChunkManager => _mChunkManager;
    public LightingEngine LightingEngine => _mCightingEngine;
    
    public GameWorld()
    {
        _mChunkManager = new ChunkManager();
        _mChunkManager.SetWorld(this);
        _mCightingEngine = new LightingEngine(this);
    }
    
    public async Task InitializeAsync()
    {
        Console.WriteLine($"Initializing world ({WORLD_WIDTH}x{WORLD_HEIGHT}x{WORLD_DEPTH}) with 16x16 chunks...");
        await _mChunkManager.InitializeAsync();
        Console.WriteLine("World initialized!");
        mIsInitializing = false;
        _mCightingEngine.EnableMeshRegeneration();
    }
    
    public async Task TickAsync()
    {
        await _mChunkManager.TickAsync();
        _mCightingEngine.ProcessLightingUpdates();

        ProcessRandomBlockUpdates();
    }
    
    public byte GetBlock(int x, int y, int z)
    {
        if (x < 0 || x >= WORLD_WIDTH || y < 0 || y >= WORLD_HEIGHT || z < 0 || z >= WORLD_DEPTH)
            return BlockIDs.Air;
            
        return _mChunkManager.GetBlock(x, y, z);
    }
    
    public void SetBlock(int x, int y, int z, byte block)
    {
        if (x < 0 || x >= WORLD_WIDTH || y < 0 || y >= WORLD_HEIGHT || z < 0 || z >= WORLD_DEPTH)
            return;
        
        byte oldBlock = GetBlock(x, y, z);

        if (block != BlockIDs.Air && block != BlockIDs.Torch && block != BlockIDs.Flower && y > 0)
        {
            byte blockBelow = GetBlock(x, y - 1, z);
            if (blockBelow == BlockIDs.Grass)
            {
                _mChunkManager.SetBlock(x, y - 1, z, BlockIDs.Dirt);
                _mCightingEngine.OnBlockChanged(x, y - 1, z, BlockIDs.Grass, BlockIDs.Dirt);
            }
        }
        
        _mChunkManager.SetBlock(x, y, z, block);

        _mCightingEngine.OnBlockChanged(x, y, z, oldBlock, block);
        
        if (!mIsInitializing)
        {
            _mCightingEngine.ProcessLightingUpdates();
            
            // Force immediate mesh update for custom blocks that might not trigger lighting changes
            if (block == BlockIDs.Slab || block == BlockIDs.Flower || oldBlock == BlockIDs.Slab || oldBlock == BlockIDs.Flower)
            {
                int chunkX = x >> 4;
                int chunkZ = z >> 4;
                var chunk = _mChunkManager.GetChunk(chunkX, chunkZ);
                if (chunk != null)
                {
                    _mChunkManager.RegenChunkMesh(chunk);
                }
            }
        }
    }
    
    public bool IsBlockSolid(int x, int y, int z)
    {
        var block = GetBlock(x, y, z);
        return block != BlockIDs.Air && block != BlockIDs.Torch && block != BlockIDs.Flower;
    }
    
    public bool IsPositionValid(int x, int y, int z) => x >= 0 && x < WORLD_WIDTH && y >= 0 && y < WORLD_HEIGHT && z >= 0 && z < WORLD_DEPTH;

    public IEnumerable<Chunk> GetLoadedChunks() => _mChunkManager.GetLoadedChunks();


    private void ProcessRandomBlockUpdates()
    {
        if (mIsInitializing) 
            return;

        foreach (var chunk in _mChunkManager.GetLoadedChunks())
        {
            ProcessChunkRandomTicks(chunk);
        }
    }
    
    private void ProcessChunkRandomTicks(Chunk chunk)
    {
        for (int i = 0; i < RANDOM_TICKS_PER_CHUNK; i++)
        {
            int x = _mRandom.Next(0, GameConstants.CHUNK_SIZE);
            int y = _mRandom.Next(0, GameConstants.CHUNK_HEIGHT);
            int z = _mRandom.Next(0, GameConstants.CHUNK_SIZE);
            
            byte blockId = chunk.GetBlock(x, y, z);
            var block = BlockRegistry.GetBlock(blockId);

            if (block is IRandomTickable randomTickable)
            {
                int worldX = chunk.X * GameConstants.CHUNK_SIZE + x;
                int worldZ = chunk.Z * GameConstants.CHUNK_SIZE + z;
                randomTickable.OnRandomTick(this, worldX, y, worldZ, _mRandom);
            }
        }
    }
    
    
    public void Dispose()
    {
        _mChunkManager?.Dispose();
    }
}