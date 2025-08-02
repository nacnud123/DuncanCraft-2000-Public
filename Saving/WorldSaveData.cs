namespace VoxelGame.Saving
{
    [Serializable]
    public struct WorldSaveData
    {
        public int ID;
        public string WorldName;
        public int Seed;
    }
}