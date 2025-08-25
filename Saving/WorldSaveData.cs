// This is a small struct that holds the world's data, like name, seed, ect.. Importantly this does not hold the world's chunk data, that lives in it's own file. | DA | 8/25/25
namespace VoxelGame.Saving
{
    [Serializable]
    public struct WorldSaveData
    {
        public int ID;
        public string WorldName;
        public int Seed;
        public DateTime LastPlayed;
    }
}