// This class handles raycasts and how they work. Also holds the resaults of what the raycast hit | DA | 9/4/25
using DuncanCraft.World;
using OpenTK.Mathematics;

namespace DuncanCraft.Utils;

public static class Raycast
{
    public static RaycastResult? Cast(Vector3 origin, Vector3 direction, GameWorld world, float maxDistance = 10.0f)
    {
        direction = direction.Normalized();
        
        float stepSize = 0.1f;
        int steps = (int)(maxDistance / stepSize);
        
        Vector3 lastAirPos = origin;
        
        for (int i = 0; i < steps; i++)
        {
            Vector3 currentPos = origin + direction * (i * stepSize);
            
            int blockX = (int)Math.Floor(currentPos.X);
            int blockY = (int)Math.Floor(currentPos.Y);
            int blockZ = (int)Math.Floor(currentPos.Z);
            
            if (!world.IsPositionValid(blockX, blockY, blockZ))
                break;
                
            var block = world.GetBlock(blockX, blockY, blockZ);
            
            if (block != BlockIDs.Air)
            {
                // Hit a solid block
                Vector3 lastAirBlockPos = new Vector3(
                    (int)Math.Floor(lastAirPos.X),
                    (int)Math.Floor(lastAirPos.Y),
                    (int)Math.Floor(lastAirPos.Z)
                );
                
                return new RaycastResult
                {
                    Hit = true,
                    HitBlock = new Vector3i(blockX, blockY, blockZ),
                    PlaceBlock = new Vector3i((int)lastAirBlockPos.X, (int)lastAirBlockPos.Y, (int)lastAirBlockPos.Z),
                    Distance = i * stepSize,
                    HitPosition = currentPos
                };
            }
            
            lastAirPos = currentPos;
        }
        
        return new RaycastResult { Hit = false };
    }
}

public struct RaycastResult
{
    public bool Hit;
    public Vector3i HitBlock;
    public Vector3i PlaceBlock;
    public float Distance;
    public Vector3 HitPosition;
}