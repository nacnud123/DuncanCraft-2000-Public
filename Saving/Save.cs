// Small class that holds data related to saving. | DA | 8/1/25
using VoxelGame.Utils;
using VoxelGame.World;

namespace VoxelGame.Saving
{
    [Serializable]
    public class Save
    {
        public byte[] FlatBlocks;
        public int SizeX, SizeY, SizeZ;
        public List<KeyValuePair<int, int>> Seed = new();

        public Save() { } // Needed for serialization, don't get rid of.

        public Save(Chunk chunk)
        {
            SizeX = Constants.CHUNK_SIZE;
            SizeY = Constants.CHUNK_HEIGHT;
            SizeZ = Constants.CHUNK_SIZE;
            FlatBlocks = new byte[SizeX * SizeY * SizeZ];

            for (int x = 0; x < SizeX; x++)
            {
                for (int y = 0; y < SizeY; y++)
                {
                    for (int z = 0; z < SizeZ; z++)
                    {
                        int index = x + SizeX * (y + SizeY * z);
                        FlatBlocks[index] = chunk.Voxels[x, y, z];
                    }
                }
            }
        }

        public byte[,,] To3DArray()
        {
            var result = new byte[SizeX, SizeY, SizeZ];
            for (int x = 0; x < SizeX; x++)
                for (int y = 0; y < SizeY; y++)
                    for (int z = 0; z < SizeZ; z++)
                    {
                        int index = x + SizeX * (y + SizeY * z);
                        result[x, y, z] = FlatBlocks[index];
                    }
            return result;
        }
    }

}
