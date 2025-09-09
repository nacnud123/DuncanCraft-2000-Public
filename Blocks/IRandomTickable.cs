using DuncanCraft.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DuncanCraft.Blocks
{
    public interface IRandomTickable
    {
        void OnRandomTick(GameWorld world, int x, int y, int z, Random random);
    }
}