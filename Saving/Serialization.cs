// Saves and loads worlds. Serializes the chunk into a 1d array and then when you load a chunk it makes it back into a 3d array and loads the data into the chunk. Also helps give the title screen data like a sorted list of all the worlds | DA | 8/1/25
using System.Xml.Serialization;
using VoxelGame.Utils;
using VoxelGame.World;

namespace VoxelGame.Saving
{
    public static class Serialization
    {
        public static string s_WorldName;
        
        public static string SaveLocation()
        {
            string saveLoc = GameConstants.SAVE_LOCATION + "/" + s_WorldName + "/";

            if (!Directory.Exists(saveLoc))
            {
                Directory.CreateDirectory(saveLoc);
            }

            return saveLoc;
        }

        public static string FileName(ChunkPos chunkLocation) => chunkLocation.X + "," + chunkLocation.Z + ".bin";
        public static string GetWorldDataFile() => "world_info.xml";
        public static string GetSeedFile() => "WorldSeedInfo.txt";
        
        public static WorldSaveData? LoadWorldData(string worldName)
        {
            string metaDataPath = GameConstants.SAVE_LOCATION + "/" + worldName + "/" + GetWorldDataFile();

            if (!File.Exists(metaDataPath))
                return null;
            
            var serializer = new XmlSerializer(typeof(WorldSaveData));
            using (var stream = new FileStream(metaDataPath, FileMode.Open))
            {
                return (WorldSaveData)serializer.Deserialize(stream);
            }
        }

        public static void SaveWorldMetadata(WorldSaveData saveData)
        {
            string savePath = GameConstants.SAVE_LOCATION + "/" + s_WorldName + "/";

            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            string metadataPath = savePath + GetWorldDataFile();
            
            var serializer = new XmlSerializer(typeof(WorldSaveData));
            using (var stream = new FileStream(metadataPath, FileMode.Create))
            {
                serializer.Serialize(stream, saveData);
            }
        }

        public static WorldSaveData CreateWorld(string worldName, int? customSeed = null)
        {
            WorldSaveData worldData = new WorldSaveData
            {
                ID = GenerateWorldID(),
                WorldName = worldName,
                Seed = customSeed ?? genSeed(worldName),
                LastPlayed = DateTime.Now
            };
            
            SaveWorldMetadata(worldData);
            return worldData;
        }
        
        public static void UpdateLastPlayed(string worldName)
        {
            var worldData = LoadWorldData(worldName);
            if (worldData.HasValue)
            {
                var updatedData = worldData.Value;
                updatedData.LastPlayed = DateTime.Now;
                
                string oldWorldName = s_WorldName;
                s_WorldName = worldName;
                SaveWorldMetadata(updatedData);
                s_WorldName = oldWorldName;
            }
        }
        
        public static List<WorldSaveData> GetAllWorlds()
        {
            var worlds = new List<WorldSaveData>();
            
            if (!Directory.Exists(GameConstants.SAVE_LOCATION))
                return worlds;

            var worldDirs = Directory.GetDirectories(GameConstants.SAVE_LOCATION);
            
            foreach (var dir in worldDirs)
            {
                string worldName = Path.GetFileName(dir);
                var worldData = LoadWorldData(worldName);
                if (worldData.HasValue)
                {
                    var data = worldData.Value;

                    if (data.LastPlayed == DateTime.MinValue)
                    {
                        try
                        {
                            data.LastPlayed = Directory.GetCreationTime(dir);
                        }
                        catch
                        {
                            data.LastPlayed = DateTime.Now.AddDays(-365);
                        }
                    }
                    
                    worlds.Add(data);
                }
            }

            worlds.Sort((w1, w2) => w2.LastPlayed.CompareTo(w1.LastPlayed));

            return worlds;
        }
        
        private static int GenerateWorldID()
        {
            return Math.Abs(Guid.NewGuid().GetHashCode());
        }
        
        private static int genSeed(string worldName)
        {
            int hash = worldName.GetHashCode();

            var today = DateTime.Today;
            int dateHash = today.GetHashCode();

            int seed = Math.Abs(hash ^ dateHash);
            return seed;
        }

        public static void SaveChunk(Chunk chunk)
        {
            Save save = new Save(chunk);
            if (save.FlatBlocks.Length == 0)
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
            for (int x = 0; x < GameConstants.CHUNK_SIZE; x++)
            {
                for (int y = 0; y < GameConstants.CHUNK_HEIGHT; y++)
                {
                    for (int z = 0; z < GameConstants.CHUNK_SIZE; z++)
                    {
                        chunk.Voxels[x, y, z] = temp[x, y, z];
                    }
                }
            }

            chunk.TerrainGenerated = true;
            return true;
        }
    }
}