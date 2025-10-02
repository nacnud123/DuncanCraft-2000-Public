// This is the main tick system of the game, a minecraft style tick system | DA | 8/25/25
using OpenTK.Mathematics;
using System.Collections.Concurrent;
using System.Diagnostics;
using VoxelGame.Blocks;
using VoxelGame.Lighting;
using VoxelGame.Utils;
using VoxelGame.World;

namespace VoxelGame.Ticking
{
    public class WorldTickSystem : IDisposable
    {
        
        private const double TICK_TIME = 1000.0 / GameConstants.TARGET_TPS;
        private const int MAX_CHUNK_UPDATES_PER_TICK = 128;
        private const int MAX_LIGHTING_UPDATES_PER_TICK = 128;
        private const int RANDOM_TICKS_PER_CHUNK = 3;

        private readonly ChunkManager _mChunkManager;
        private readonly Thread _mTickThread;
        private readonly CancellationTokenSource _mCancellationToken;
        private volatile bool _mIsRunning;

        private readonly ConcurrentQueue<ChunkTickUpdate> _mChunkUpdates;
        private readonly ConcurrentQueue<LightingUpdate> _mLightingUpdates;
        private readonly ConcurrentQueue<BlockUpdate> _mBlockUpdates;
        private readonly ConcurrentQueue<TerrainGenerationRequest> _mTerrainGenRequests;
        private readonly Random _mRand;

        public int CurrentTPS { get; private set; }
        public long TickCount { get; private set; }
        public double AverageTickTime { get; private set; }

        private readonly Queue<double> mRecentTickTimes = new Queue<double>();
        private const int TICK_TIME_SAMPLES = 100;

        public WorldTickSystem(ChunkManager chunkManager)
        {
            _mChunkManager = chunkManager;
            _mCancellationToken = new CancellationTokenSource();
            _mRand = new Random();

            _mChunkUpdates = new ConcurrentQueue<ChunkTickUpdate>();
            _mLightingUpdates = new ConcurrentQueue<LightingUpdate>();
            _mBlockUpdates = new ConcurrentQueue<BlockUpdate>();
            _mTerrainGenRequests = new ConcurrentQueue<TerrainGenerationRequest>();

            _mTickThread = new Thread(tick)
            {
                Name = "WorldTickThread",
                IsBackground = false
            };
        }

        public void Start()
        {
            if (_mIsRunning) 
                return;

            _mIsRunning = true;
            _mTickThread.Start();
        }

        public void Stop()
        {
            if (!_mIsRunning) 
                return;

            _mIsRunning = false;
            _mCancellationToken.Cancel();

            if (_mTickThread.IsAlive)
            {
                _mTickThread.Join(5000);
            }
        }

        private void tick()
        {
            var stopwatch = Stopwatch.StartNew();
            double nextTickTime = 0;

            while (_mIsRunning && !_mCancellationToken.Token.IsCancellationRequested)
            {
                double currentTime = stopwatch.Elapsed.TotalMilliseconds;

                if (currentTime >= nextTickTime)
                {
                    double tickStartTime = currentTime;

                    try
                    {
                        processTick();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in world tick: {ex.Message}");
                        Console.WriteLine(ex.StackTrace);
                    }

                    double tickEndTime = stopwatch.Elapsed.TotalMilliseconds;
                    double tickDuration = tickEndTime - tickStartTime;

                    updateStatistics(tickDuration);

                    nextTickTime = currentTime + TICK_TIME;
                    TickCount++;
                }
                else
                {
                    // Sleep for a very short time to reduce input lag
                    int sleepTime = Math.Max(0, (int)(nextTickTime - currentTime));
                    if (sleepTime > 1)
                    {
                        Thread.Sleep(Math.Min(sleepTime, 5));
                    }
                    else if (sleepTime > 0)
                    {
                        Thread.Yield(); // Give up time slice but don't sleep for long periods
                    }
                }
            }
        }

        private void processTick()
        {
            // Process terrain generation requests
            processTerrainGeneration();

            // Process block updates
            processBlockUpdates();

            // Process random ticks
            processRandomTicks();

            // Process lighting updates
            processLightingUpdates();

            // Process chunk mesh updates
            processChunkUpdates();
        }

        private void processTerrainGeneration()
        {
            int processed = 0;
            var generatedChunks = new List<ChunkPos>();

            while (processed < MAX_CHUNK_UPDATES_PER_TICK && _mTerrainGenRequests.TryDequeue(out var request))
            {
                try
                {
                    if (_mChunkManager._Chunks.TryGetValue(request.ChunkPos, out var chunk) && !chunk.TerrainGenerated)
                    {
                        VoxelGame.init.TerrainGen.GenerateTerrain(chunk);
                        chunk.TerrainGenerated = true;
                        generatedChunks.Add(request.ChunkPos);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error generating terrain for chunk {request.ChunkPos}: {ex.Message}");
                }
                processed++;
            }

            if (generatedChunks.Count > 0)
            {
                processNewlyGeneratedChunks(generatedChunks);
            }
        }

        private void processNewlyGeneratedChunks(List<ChunkPos> generatedChunks)
        {
            var chunksNeedingLighting = new HashSet<ChunkPos>();
            var chunksNeedingMeshUpdate = new HashSet<ChunkPos>();

            foreach (var chunkPos in generatedChunks)
            {
                chunksNeedingLighting.Add(chunkPos);
                chunksNeedingMeshUpdate.Add(chunkPos);

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        if (dx == 0 && dz == 0) continue;

                        var neighborPos = new ChunkPos(chunkPos.X + dx, chunkPos.Z + dz);
                        if (_mChunkManager._Chunks.ContainsKey(neighborPos))
                        {
                            chunksNeedingLighting.Add(neighborPos);
                            chunksNeedingMeshUpdate.Add(neighborPos);
                        }
                    }
                }
            }

            QueueLightingUpdate(new LightingUpdate
            {
                Type = LightingUpdateType.BatchChunkGenerated,
                AffectedChunks = new List<ChunkPos>(chunksNeedingLighting)
            });


            foreach (var chunkPos in chunksNeedingMeshUpdate)
            {
                QueueChunkUpdate(new ChunkTickUpdate
                {
                    ChunkPos = chunkPos,
                    UpdateType = ChunkUpdateType.MeshRegeneration
                });
            }
        }

        private void processBlockUpdates()
        {
            while (_mBlockUpdates.TryDequeue(out var update))
            {
                try
                {
                    applyBlockUpdate(update);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing block update: {ex.Message}");
                }
            }
        }

        private void processRandomTicks()
        {
            var loadedChunks = _mChunkManager._Chunks.Values.Where(chunk => chunk.TerrainGenerated).Take(16).ToList();

            foreach (var chunk in loadedChunks)
            {
                for (int i = 0; i < RANDOM_TICKS_PER_CHUNK; i++)
                {
                    int x = _mRand.Next(0, GameConstants.CHUNK_SIZE);
                    int z = _mRand.Next(0, GameConstants.CHUNK_SIZE);
                    int y = _mRand.Next(0, GameConstants.CHUNK_HEIGHT);

                    byte blockType = chunk.Voxels[x, y, z];
                    if (blockType != BlockIDs.Air)
                    {
                        var block = BlockRegistry.GetBlock(blockType);
                        if (block is IRandomTickable randomTickable)
                        {
                            Vector3i worldPos = new Vector3i(
                                chunk.Position.X * GameConstants.CHUNK_SIZE + x,
                                y,
                                chunk.Position.Z * GameConstants.CHUNK_SIZE + z
                            );

                            randomTickable.OnRandomTick(worldPos, this, _mChunkManager);
                        }
                    }
                }
            }
        }

        private void processLightingUpdates()
        {
            int processed = 0;
            while (processed < MAX_LIGHTING_UPDATES_PER_TICK && _mLightingUpdates.TryDequeue(out var update))
            {
                try
                {
                    applyLightingUpdate(update);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing lighting update: {ex.Message}");
                }
                processed++;
            }
        }

        private void processChunkUpdates()
        {
            int processed = 0;
            while (processed < MAX_CHUNK_UPDATES_PER_TICK && _mChunkUpdates.TryDequeue(out var update))
            {
                try
                {
                    applyChunkUpdate(update);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing chunk update: {ex.Message}");
                }
                processed++;
            }
        }

        private void applyBlockUpdate(BlockUpdate update)
        {
            ChunkPos chunkPos = new ChunkPos(
                (int)Math.Floor(update.WorldPos.X / (float)GameConstants.CHUNK_SIZE),
                (int)Math.Floor(update.WorldPos.Z / (float)GameConstants.CHUNK_SIZE)
            );

            if (_mChunkManager._Chunks.TryGetValue(chunkPos, out var chunk))
            {
                Vector3i localPos = new Vector3i(
                    update.WorldPos.X - chunkPos.X * GameConstants.CHUNK_SIZE,
                    update.WorldPos.Y,
                    update.WorldPos.Z - chunkPos.Z * GameConstants.CHUNK_SIZE
                );

                if (localPos.X < 0) 
                    localPos.X += GameConstants.CHUNK_SIZE;

                if (localPos.Z < 0) 
                    localPos.Z += GameConstants.CHUNK_SIZE;

                if (chunk.IsInBounds(localPos))
                {
                    byte oldBlock = chunk.Voxels[localPos.X, localPos.Y, localPos.Z];

                    if (oldBlock == BlockIDs.Bedrock && update.NewBlockType != BlockIDs.Bedrock)
                        return; // Can't break bedrock

                    chunk.Voxels[localPos.X, localPos.Y, localPos.Z] = update.NewBlockType;
                    chunk.Modified = true;

                    if (update.IsBreaking && oldBlock != BlockIDs.Air)
                    {
                        VoxelGame.init.WorldAudioManager.PlayBlockBreakSound(BlockRegistry.GetBlock(oldBlock).Material);
                    }

                    QueueLightingUpdate(new LightingUpdate
                    {
                        Type = LightingUpdateType.BlockChanged,
                        WorldPos = update.WorldPos,
                        ChunkPos = chunkPos
                    });
                }
            }
        }

        private void applyLightingUpdate(LightingUpdate update)
        {
            switch (update.Type)
            {
                case LightingUpdateType.BlockChanged:
                    var affectedChunks = getAffectedChunks(update.WorldPos);
                    var chunks = new List<Chunk>();

                    foreach (var chunkPos in affectedChunks)
                    {
                        if (_mChunkManager._Chunks.TryGetValue(chunkPos, out var chunk) && chunk.TerrainGenerated)
                        {
                            chunks.Add(chunk);
                        }
                    }

                    if (chunks.Count > 0)
                    {
                        _mChunkManager.LightingEngine.CalcMultiChunkLighting(chunks);

                        foreach (var chunkPos in affectedChunks)
                        {
                            QueueChunkUpdate(new ChunkTickUpdate
                            {
                                ChunkPos = chunkPos,
                                UpdateType = ChunkUpdateType.MeshRegeneration
                            });
                        }
                    }
                    break;

                case LightingUpdateType.ChunkGenerated:
                    if (_mChunkManager._Chunks.TryGetValue(update.ChunkPos, out var newChunk))
                    {
                        var neighborChunks = new List<Chunk> { newChunk };

                        // Add neighboring chunks
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                if (dx == 0 && dz == 0)
                                    continue;

                                var neighborPos = new ChunkPos(update.ChunkPos.X + dx, update.ChunkPos.Z + dz);
                                if (_mChunkManager._Chunks.TryGetValue(neighborPos, out var neighbor) && neighbor.TerrainGenerated)
                                {
                                    neighborChunks.Add(neighbor);
                                }
                            }
                        }

                        _mChunkManager.LightingEngine.CalcMultiChunkLighting(neighborChunks);
                    }
                    break;

                case LightingUpdateType.BatchChunkGenerated:
                    if (update.AffectedChunks != null)
                    {
                        var batchChunks = new List<Chunk>();

                        foreach (var chunkPos in update.AffectedChunks)
                        {
                            if (_mChunkManager._Chunks.TryGetValue(chunkPos, out var chunk) && chunk.TerrainGenerated)
                            {
                                batchChunks.Add(chunk);
                            }
                        }

                        if (batchChunks.Count > 0)
                        {
                            _mChunkManager.LightingEngine.CalcMultiChunkLighting(batchChunks);
                        }
                    }
                    break;
            }
        }

        private void applyChunkUpdate(ChunkTickUpdate update)
        {
            if (_mChunkManager._Chunks.TryGetValue(update.ChunkPos, out var chunk))
            {
                switch (update.UpdateType)
                {
                    case ChunkUpdateType.MeshRegeneration:
                        if (chunk.TerrainGenerated)
                            chunk.MeshGenerated = false;

                        break;
                }
            }
        }

        private List<ChunkPos> getAffectedChunks(Vector3i worldPos)
        {
            var chunks = new HashSet<ChunkPos>(8);
            const int lightRadius = 15;

            ChunkPos centerChunk = new ChunkPos(
                (int)Math.Floor(worldPos.X / (float)GameConstants.CHUNK_SIZE),
                (int)Math.Floor(worldPos.Z / (float)GameConstants.CHUNK_SIZE)
            );

            chunks.Add(centerChunk);

            Vector3i localPos = new Vector3i(
                worldPos.X - centerChunk.X * GameConstants.CHUNK_SIZE,
                worldPos.Y,
                worldPos.Z - centerChunk.Z * GameConstants.CHUNK_SIZE
            );

            if (localPos.X <= lightRadius)
                chunks.Add(new ChunkPos(centerChunk.X - 1, centerChunk.Z));
            if (localPos.X >= GameConstants.CHUNK_SIZE - lightRadius)
                chunks.Add(new ChunkPos(centerChunk.X + 1, centerChunk.Z));
            if (localPos.Z <= lightRadius)
                chunks.Add(new ChunkPos(centerChunk.X, centerChunk.Z - 1));
            if (localPos.Z >= GameConstants.CHUNK_SIZE - lightRadius)
                chunks.Add(new ChunkPos(centerChunk.X, centerChunk.Z + 1));

            return new List<ChunkPos>(chunks);
        }

        private void updateStatistics(double tickTime)
        {
            mRecentTickTimes.Enqueue(tickTime);
            if (mRecentTickTimes.Count > TICK_TIME_SAMPLES)
            {
                mRecentTickTimes.Dequeue();
            }

            double sum = 0;
            foreach (double time in mRecentTickTimes)
            {
                sum += time;
            }
            AverageTickTime = sum / mRecentTickTimes.Count;

            if (TickCount % GameConstants.TARGET_TPS == 0)
            {
                CurrentTPS = (int)(1000.0 / AverageTickTime);
            }
        }

        public void QueueBlockUpdate(Vector3i worldPos, byte newBlockType, bool isBreaking = false)
        {
            _mBlockUpdates.Enqueue(new BlockUpdate
            {
                WorldPos = worldPos,
                NewBlockType = newBlockType,
                IsBreaking = isBreaking
            });
        }

        public void QueueChunkUpdate(ChunkTickUpdate update)
        {
            _mChunkUpdates.Enqueue(update);
        }

        public void QueueLightingUpdate(LightingUpdate update)
        {
            _mLightingUpdates.Enqueue(update);
        }

        public void QueueTerrainGeneration(ChunkPos chunkPos)
        {
            _mTerrainGenRequests.Enqueue(new TerrainGenerationRequest
            {
                ChunkPos = chunkPos
            });
        }

        public void Dispose()
        {
            Stop();
            _mCancellationToken?.Dispose();
        }
    }

    public struct BlockUpdate
    {
        public Vector3i WorldPos;
        public byte NewBlockType;
        public bool IsBreaking;
    }

    public struct ChunkTickUpdate
    {
        public ChunkPos ChunkPos;
        public ChunkUpdateType UpdateType;
    }

    public struct LightingUpdate
    {
        public LightingUpdateType Type;
        public Vector3i WorldPos;
        public ChunkPos ChunkPos;
        public List<ChunkPos> AffectedChunks;
    }

    public struct TerrainGenerationRequest
    {
        public ChunkPos ChunkPos;
    }

    public enum ChunkUpdateType
    {
        MeshRegeneration,
        LightingUpdate,
        BlockUpdate
    }

    public enum LightingUpdateType
    {
        BlockChanged,
        ChunkGenerated,
        BatchChunkGenerated,
        AreaUpdate
    }
}