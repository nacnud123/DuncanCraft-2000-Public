using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoxelGame.Utils;
using VoxelGame.World;

namespace VoxelGame.Saving
{
    [Serializable]
    public class Save
    {
        public byte[] flatBlocks;
        public int sizeX, sizeY, sizeZ;
        public List<KeyValuePair<int, int>> seed = new();

        public Save() { }

        public Save(Chunk chunk)
        {
            sizeX = Constants.CHUNK_SIZE;
            sizeY = Constants.CHUNK_HEIGHT;
            sizeZ = Constants.CHUNK_SIZE;
            flatBlocks = new byte[sizeX * sizeY * sizeZ];

            for (int x = 0; x < sizeX; x++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        int index = x + sizeX * (y + sizeY * z);
                        flatBlocks[index] = chunk.Voxels[x, y, z];
                    }
                }
            }
        }

        public byte[,,] To3DArray()
        {
            var result = new byte[sizeX, sizeY, sizeZ];
            for (int x = 0; x < sizeX; x++)
                for (int y = 0; y < sizeY; y++)
                    for (int z = 0; z < sizeZ; z++)
                    {
                        int index = x + sizeX * (y + sizeY * z);
                        result[x, y, z] = flatBlocks[index];
                    }
            return result;
        }
    }

}
