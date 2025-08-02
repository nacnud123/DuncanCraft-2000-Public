// Interface that defines what a block is and it's properties. Could this be a struct, sure, but it is not | DA | 8/1/25
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
        /// <summary>
        /// Block's ID
        /// </summary>
        public int ID { get; }

        /// <summary>
        /// The texture coords for the top of the block
        /// </summary>
        TextureCoords TopTextureCoords { get; }

        /// <summary>
        /// The texture coords for the bottom of the block
        /// </summary>
        TextureCoords BottomTextureCoords { get; }

        /// <summary>
        /// The texture coords for the side of the block
        /// </summary>
        TextureCoords SideTextureCoords { get; }

        /// <summary>
        /// The texture coords used in the inventory
        /// </summary>
        TextureCoords InventoryCoords { get; }

        /// <summary>
        /// Is the block solid. EX: air and flowers are not solid so the player can walk right through them, Glass is not solid so it always renders all the sides
        /// </summary>
        public bool IsSolid { get; }

        /// <summary>
        /// Is the block effected by gravity, not used yet
        /// </summary>
        public bool GravityBlock { get; }

        /// <summary>
        /// Whats the name of the block
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Whats the block made out of / what category does it fit into
        /// </summary>
        public BlockMaterial Material { get; }
    }
}
