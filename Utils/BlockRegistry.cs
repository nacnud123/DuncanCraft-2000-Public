// Holds all the blocks in the game and allows for an easy way to reference blocks. Also allows for an easy way to get block IDs. | DA | 8/1/25
using VoxelGame.Blocks;

namespace VoxelGame.Utils
{
    public static class BlockRegistry
    {
        private static Dictionary<byte, IBlock> mBlocks = new Dictionary<byte, IBlock>();

        public static Dictionary<byte, IBlock> GetAllBocks { get => mBlocks; }

        static BlockRegistry()
        {
            RegisterBlocks();
        }

        private static void RegisterBlocks()
        {
            mBlocks[0] = new AirBlock();
            mBlocks[1] = new StoneBlock();
            mBlocks[2] = new DirtBlock();
            mBlocks[3] = new GrassBlock();
            mBlocks[4] = new BedrockBlock();
            mBlocks[5] = new LogBlock();
            mBlocks[6] = new GlassBlock();
            mBlocks[7] = new PlankBlock();
            mBlocks[8] = new LeavesBlock();
            mBlocks[9] = new WhiteBlock();
            mBlocks[10] = new RedBlock();
            mBlocks[11] = new GreenBlock();
            mBlocks[12] = new BlueBlock();
            mBlocks[13] = new BlackBlock();
            mBlocks[14] = new YellowFlowerBlock();
            mBlocks[15] = new SlabBlock();
            mBlocks[16] = new SandBlock();
            mBlocks[17] = new CoalOreBlock();
            mBlocks[18] = new IronOreBlock();
            mBlocks[19] = new GoldOreBlock();
            mBlocks[20] = new DiamondOreBlock();
            mBlocks[21] = new TorchBlock();
            mBlocks[22] = new BricksBlock();
            mBlocks[23] = new GrassTuftBlock();
            mBlocks[24] = new DuncanBlock();
            mBlocks[25] = new SpongeBlock();
            mBlocks[26] = new RedMushroomBlock();
            mBlocks[27] = new BrownMushroomBlock();
            mBlocks[28] = new GravelBlock();
            
        }

        public static int GetBlocksSize()
        {
            return mBlocks.Count;
        }

        public static IBlock GetBlock(byte voxelType)
        {
            return mBlocks.TryGetValue(voxelType, out IBlock block) ? block : null;
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
        public const byte Torch = 21;
        public const byte Brick = 22;
        public const byte GrassTuff = 23;
        public const byte Duncan = 24;
        public const byte Sponge = 25;
        public const byte RedMushroom = 26;
        public const byte BrownMushroom = 27;
        public const byte Gravel = 28;
        
    }
}