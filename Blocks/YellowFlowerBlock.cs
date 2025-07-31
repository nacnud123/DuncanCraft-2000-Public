using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoxelGame.Utils;

namespace VoxelGame.Blocks
{
    internal class YellowFlowerBlock : IBlock
    {
        public int ID => 14;

        public TextureCoords TopTextureCoords => UVHelper.FromTileCoords(4, 1);

        public TextureCoords BottomTextureCoords => TopTextureCoords;

        public TextureCoords SideTextureCoords => TopTextureCoords;

        public bool IsSolid => false;

        public string Name => "Yellow Flower";

        public TextureCoords InventoryCoords => TopTextureCoords;

        public bool GravityBlock => false;

        public BlockMaterial Material => BlockMaterial.Leaves;
    }
}