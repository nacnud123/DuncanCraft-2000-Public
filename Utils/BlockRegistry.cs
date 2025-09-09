// This class is used register blocks for easy lookup
using DuncanCraft.Blocks;

namespace DuncanCraft.Utils
{
    public static class BlockRegistry
    {
        private static readonly Dictionary<byte, IBlock> _mBlocks = new();
        public static Dictionary<byte, IBlock> GetAllBlocks => _mBlocks;

        static BlockRegistry()
        {
            RegisterBlocks();
        }

        private static void RegisterBlocks()
        {
            _mBlocks[0] = new AirBlock();
            _mBlocks[1] = new StoneBlock();
            _mBlocks[2] = new DirtBlock();
            _mBlocks[3] = new GrassBlock();
            _mBlocks[4] = new TorchBlock();
            _mBlocks[5] = new GlowstoneBlock();
            _mBlocks[6] = new SlabBlock();
            _mBlocks[7] = new FlowerBlock();
        }

        public static int GetBlocksSize()
        {
            return _mBlocks.Count;
        }

        public static IBlock? GetBlock(byte voxelType)
        {
            return _mBlocks.TryGetValue(voxelType, out IBlock? block) ? block : null;
        }

    }

    public static class BlockIDs
    {
        public const byte Air = 0;
        public const byte Stone = 1;
        public const byte Dirt = 2;
        public const byte Grass = 3;
        public const byte Torch = 4;
        public const byte Glowstone = 5;
        public const byte Slab = 6;
        public const byte Flower = 7;
    }
}
