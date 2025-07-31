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
            blocks[4] = new BedrockBlock();
            blocks[5] = new LogBlock();
            blocks[6] = new GlassBlock();
            blocks[7] = new PlankBlock();
            blocks[8] = new LeavesBlock();
            blocks[9] = new WhiteBlock();
            blocks[10] = new RedBlock();
            blocks[11] = new GreenBlock();
            blocks[12] = new BlueBlock();
            blocks[13] = new BlackBlock();
            blocks[14] = new YellowFlowerBlock();
            blocks[15] = new SlabBlock();
            blocks[16] = new SandBlock();
            blocks[17] = new CoalOreBlock();
            blocks[18] = new IronOreBlock();
            blocks[19] = new GoldOreBlock();
            blocks[20] = new DiamondOreBlock();
            
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
        public const byte Bedrock = 4;
        public const byte Log = 5;
        public const byte Glass = 6;
        public const byte Plank = 7;
        public const byte Leaves = 8;
        public const byte White = 9;
        public const byte Red = 10;
        public const byte Green = 11;
        public const byte Blue = 12;
        public const byte Black = 13;
        public const byte YellowFlower = 14;
        public const byte Slab = 15;
        public const byte Sand = 16;
        public const byte CoalOre = 17;
        public const byte IronOre = 18;
        public const byte GoldOre = 19;
        public const byte DiamondOre = 20;
        
    }
}