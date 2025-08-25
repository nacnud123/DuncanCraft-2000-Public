using System;
using VoxelGame.Utils;

namespace VoxelGame.Blocks
{
    public class AirBlock : IBlock
    {
        public int ID => 0;

        public TextureCoords TopTextureCoords => throw new NotImplementedException();

        public TextureCoords BottomTextureCoords => throw new NotImplementedException();

        public TextureCoords SideTextureCoords => throw new NotImplementedException();

        public bool IsSolid => false;

        public string Name => "AIR";

        public TextureCoords InventoryCoords => throw new NotImplementedException();
        public bool GravityBlock => false;
        public BlockMaterial Material => BlockMaterial.None;

        public bool Transparent => true;
        public byte LightLevel => 0;
        public byte LightOpacity => 0;
    }
}
