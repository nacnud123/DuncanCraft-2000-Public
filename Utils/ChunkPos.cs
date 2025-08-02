// My own version of Vector2 but for chunks. Could I just use a Vector2 probably but making my own data type is fun. | DA | 8/1/25
namespace VoxelGame.Utils
{
    public struct ChunkPos : IEquatable<ChunkPos>
    {
        public int X { get; }
        public int Z { get; }

        public ChunkPos(int x, int z)
        {
            X = x;
            Z = z;
        }

        public bool Equals(ChunkPos other)
        {
            return X == other.X && Z == other.Z;
        }

        public override bool Equals(object obj)
        {
            return obj is ChunkPos other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Z);
        }

        public static bool operator ==(ChunkPos left, ChunkPos right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ChunkPos left, ChunkPos right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"{X}, {Z}";
        }
    }
}
