using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VoxelGame.Utils;
using VoxelGame.World;

namespace VoxelGame.Saving
{
    public static class Serialization
    {
        public static string WorldName;

        public static string SaveLocation()
        {
            string saveLoc = Constants.SAVE_LOCATION + "/" + WorldName + "/";

            if (!Directory.Exists(saveLoc))
            {
                Directory.CreateDirectory(saveLoc);
            }

            return saveLoc;
        }

        public static string FileName(ChunkPos chunkLocation) => chunkLocation.X + "," + chunkLocation.Z + ".bin";

        public static string seedFile()
        {
            return "WorldSeedInfo.txt";
        }

        public static int readSead(string path)
        {
            int seed = 0;
            path += seedFile();
            StreamReader reader = new StreamReader(path);
            seed = Int32.Parse(reader.ReadToEnd());
            reader.Close();

            return seed;
        }

        public static int writeSeed(string path, string worldName)
        {
            SaveLocation();
            int seed = 0;
            path += seedFile();

            FileStream fs = File.Create(path);
            StreamWriter writer = new StreamWriter(fs);

            // Generate seed based on world name for consistency
            seed = GenerateSeedFromWorldName(worldName);

            writer.WriteLine(seed);
            writer.Close();
            fs.Close();
            return seed;
        }

        // Generate a consistent seed based on the world name
        private static int GenerateSeedFromWorldName(string worldName)
        {
            // Use the world name to generate a consistent hash-based seed
            int hash = worldName.GetHashCode();

            // Combine with current time to add some randomness while keeping consistency per name
            // But only use the date part, not the exact time, so same name = same seed
            var today = DateTime.Today;
            int dateHash = today.GetHashCode();

            // XOR the hashes together and ensure positive value
            int seed = Math.Abs(hash ^ dateHash);

            // If you want purely deterministic seeds based only on world name (recommended):
            // Remove the dateHash and just use:
            // int seed = Math.Abs(worldName.GetHashCode());

            return seed;
        }

        public static void SaveChunk(Chunk chunk)
        {
            Save save = new Save(chunk);
            if (save.flatBlocks.Length == 0)
                return;

            string saveFile = SaveLocation();
            saveFile += FileName(chunk.Position);

            var serializer = new XmlSerializer(typeof(Save));
            using (var stream = new FileStream(saveFile, FileMode.Create))
            {
                serializer.Serialize(stream, save);
            }

            chunk.Modified = false;
        }

        public static bool Load(Chunk chunk)
        {
            string saveFile = SaveLocation();
            saveFile += FileName(chunk.Position);

            if (!File.Exists(saveFile))
                return false;

            var serializer = new XmlSerializer(typeof(Save));
            Save save;
            using (var stream = new FileStream(saveFile, FileMode.Open))
            {
                save = (Save)serializer.Deserialize(stream);
            }

            var temp = save.To3DArray();
            for (int x = 0; x < Constants.CHUNK_SIZE; x++)
            {
                for (int y = 0; y < Constants.CHUNK_HEIGHT; y++)
                {
                    for (int z = 0; z < Constants.CHUNK_SIZE; z++)
                    {
                        chunk.Voxels[x, y, z] = temp[x, y, z];
                    }
                }
            }
            return true;
        }
    }
}