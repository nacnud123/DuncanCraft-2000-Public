using OpenTK.Mathematics;
using System.Collections.Concurrent;
using VoxelGame.Saving;
using VoxelGame.Utils;

namespace VoxelGame.World
{
    public class ChunkManager : IDisposable
    {
        public readonly ConcurrentDictionary<ChunkPos, Chunk> _chunks = new();
        private readonly ConcurrentQueue<ChunkPos> meshGenerationQueue = new();
        private readonly CancellationTokenSource cancellationTokenSource = new();
        private readonly Task meshGenerationTask;

        public ChunkManager()
        {
            meshGenerationTask = Task.Run(meshGenWorker, cancellationTokenSource.Token);
        }

        public void UpdateChunks(Vector3 playerPos)
        {
            ChunkPos playerChunk = new ChunkPos(
                (int)MathF.Floor(playerPos.X / Constants.CHUNK_SIZE),
                (int)MathF.Floor(playerPos.Z / Constants.CHUNK_SIZE)
            );

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
                        meshGenerationQueue.Enqueue(pos);
                    }
                }
            }

            foreach (var newChunkPos in newChunks)
            {
                ReGenChunkNeighbors(newChunkPos);
            }

            foreach (var kvp in _chunks)
            {
                if (!kvp.Value.MeshGenerated && !meshGenerationQueue.Contains(kvp.Key))
                {
                    meshGenerationQueue.Enqueue(kvp.Key);
                }
            }

            var chunksToRemove = new List<ChunkPos>();
            foreach (var kvp in _chunks)
            {
                int dx = kvp.Key.X - playerChunk.X;
                int dz = kvp.Key.Z - playerChunk.Z;

                if (Math.Abs(dx) > Constants.RENDER_DISTANCE + 2 || Math.Abs(dz) > Constants.RENDER_DISTANCE + 2)
                {
                    chunksToRemove.Add(kvp.Key);
                }
            }

            foreach (var pos in chunksToRemove)
            {
                if (_chunks.TryRemove(pos, out var chunk))
                {
                    if(chunk.Modified)
                        Serialization.SaveChunk(chunk);

                    chunk.Dispose();
                }
            }
        }

        private async Task meshGenWorker()
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                if (meshGenerationQueue.TryDequeue(out ChunkPos pos))
                {
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
                    chunk.UploadMesh();
                }
            }
        }

        public void RenderChunks()
        {
            foreach (var kvp in _chunks)
            {
                if (kvp.Value.MeshUploaded)
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

            foreach (var chunk in _chunks.Values)
            {
                chunk.Dispose();
            }

            cancellationTokenSource.Dispose();
        }
    }
}
