using VoxelGame.Blocks;

namespace VoxelGame.Utils
{
    public static class BlockRegistry
    {
        private static Dictionary<byte, IBlock> blocks = new Dictionary<byte, IBlock>();

        public static Dictionary<byte, IBlock> GetAllBocks { get => blocks; }

        static BlockRegistry()
        {
            RegisterBlocks();
        }

        private static void RegisterBlocks()
        {
            blocks[0] = new AirBlock();
            blocks[1] = new StoneBlock();
            blocks[2] = new DirtBlock();
            blocks[3] = new GrassBlock();
            blocks[4] = new LogBlock();
            blocks[5] = new GlassBlock();
            blocks[6] = new PlankBlock();
            blocks[7] = new LeavesBlock();
            blocks[8] = new WhiteBlock();
            blocks[9] = new RedBlock();
            blocks[10] = new GreenBlock();
            blocks[11] = new BlueBlock();
            blocks[12] = new BlackBlock();
            blocks[13] = new YellowFlowerBlock();
            blocks[14] = new SlabBlock();
            //blocks[15] = new SandBlock();
        }

        public static int GetBlocksSize()
        {
            return blocks.Count;
        }

        public static IBlock GetBlock(byte voxelType)
        {
            return blocks.TryGetValue(voxelType, out IBlock block) ? block : null;
        }
    }

    public static class BlockIDs
    {
        public const byte Air = 0;
        public const byte Stone = 1;
        public const byte Dirt = 2;
        public const byte Grass = 3;
        public const byte Log = 4;
        public const byte Glass = 5;
        public const byte Plank = 6;
        public const byte Leaves = 7;
        public const byte White = 8;
        public const byte Red = 9;
        public const byte Green = 10;
        public const byte Blue = 11;
        public const byte Black = 12;
        public const byte YellowFlower = 13;
        public const byte Slab = 14;
        //public const byte Sand = 15;
    }
}