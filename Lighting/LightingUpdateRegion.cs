// Not sure this script is used anymore
namespace DuncanCraft.Lighting
{
    public class LightingUpdateRegion
    {
        public LightType LightType { get; }
        public int MinX { get; private set; }
        public int MinY { get; private set; }
        public int MinZ { get; private set; }
        public int MaxX { get; private set; }
        public int MaxY { get; private set; }
        public int MaxZ { get; private set; }
        
        public LightingUpdateRegion(LightType lightType, int x1, int y1, int z1, int x2, int y2, int z2)
        {
            LightType = lightType;
            MinX = Math.Min(x1, x2);
            MinY = Math.Min(y1, y2);
            MinZ = Math.Min(z1, z2);
            MaxX = Math.Max(x1, x2);
            MaxY = Math.Max(y1, y2);
            MaxZ = Math.Max(z1, z2);
        }

        // Checks if this update region can be merged with another region
        public bool CanMergeWith(LightingUpdateRegion other)
        {
            if (LightType != other.LightType)
                return false;

            const int tolerance = 1;
            
            bool xOverlap = other.MinX <= MaxX + tolerance && other.MaxX >= MinX - tolerance;
            bool yOverlap = other.MinY <= MaxY + tolerance && other.MaxY >= MinY - tolerance;
            bool zOverlap = other.MinZ <= MaxZ + tolerance && other.MaxZ >= MinZ - tolerance;
            
            if (!(xOverlap && yOverlap && zOverlap))
                return false;

            int mergedMinX = Math.Min(MinX, other.MinX);
            int mergedMinY = Math.Min(MinY, other.MinY);
            int mergedMinZ = Math.Min(MinZ, other.MinZ);
            int mergedMaxX = Math.Max(MaxX, other.MaxX);
            int mergedMaxY = Math.Max(MaxY, other.MaxY);
            int mergedMaxZ = Math.Max(MaxZ, other.MaxZ);
            
            int currentVolume = (MaxX - MinX + 1) * (MaxY - MinY + 1) * (MaxZ - MinZ + 1);
            int otherVolume = (other.MaxX - other.MinX + 1) * (other.MaxY - other.MinY + 1) * (other.MaxZ - other.MinZ + 1);
            int mergedVolume = (mergedMaxX - mergedMinX + 1) * (mergedMaxY - mergedMinY + 1) * (mergedMaxZ - mergedMinZ + 1);

            return mergedVolume - (currentVolume + otherVolume) <= 2;
        }

        // Merges this region with another region
        public void MergeWith(LightingUpdateRegion other)
        {
            if (!CanMergeWith(other))
                return;
                
            MinX = Math.Min(MinX, other.MinX);
            MinY = Math.Min(MinY, other.MinY);
            MinZ = Math.Min(MinZ, other.MinZ);
            MaxX = Math.Max(MaxX, other.MaxX);
            MaxY = Math.Max(MaxY, other.MaxY);
            MaxZ = Math.Max(MaxZ, other.MaxZ);
        }

        // Checks if a point is contained within this region
        public bool Contains(int x, int y, int z)
        {
            return x >= MinX && x <= MaxX &&
                   y >= MinY && y <= MaxY &&
                   z >= MinZ && z <= MaxZ;
        }
        
        // Gets the volume of this update region
        public int GetVolume()
        {
            return (MaxX - MinX + 1) * (MaxY - MinY + 1) * (MaxZ - MinZ + 1);
        }
    }
}