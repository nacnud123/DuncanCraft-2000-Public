﻿using VoxelGame.Utils;

namespace VoxelGame.Blocks
{
    internal class BlueBlock : IBlock
    {
        public int ID => 12;

        public TextureCoords TopTextureCoords => UVHelper.FromTileCoords(3, 3);

        public TextureCoords BottomTextureCoords => TopTextureCoords;
        public TextureCoords SideTextureCoords => TopTextureCoords;
        public bool IsSolid => true;

        public string Name => "Blue";

        public TextureCoords InventoryCoords => TopTextureCoords;
        public bool GravityBlock => false;
        public BlockMaterial Material => BlockMaterial.Wool;
    }
}
