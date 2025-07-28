using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoxelGame.Utils;

namespace VoxelGame.Blocks
{
    public enum BlockMaterial
    {
        None,
        Dirt,
        Stone,
        Wooden,
        Leaves,
        Wool,
        Sand,
        Glass
    }

    public interface IBlock
    {
        public int ID { get; }
        TextureCoords TopTextureCoords { get; }
        TextureCoords BottomTextureCoords { get; }
        TextureCoords SideTextureCoords { get; }
        TextureCoords InventoryCoords { get; }

        public bool IsSolid { get; }
        public bool GravityBlock { get; }
        public string Name { get; }
        public BlockMaterial Material { get; }
    }
}
