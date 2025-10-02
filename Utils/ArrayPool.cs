// Array pooling utility to reduce garbage collection pressure | DA 
using System.Collections.Concurrent;
using OpenTK.Mathematics;

namespace VoxelGame.Utils
{
    public static class ArrayPool
    {
        private static readonly ConcurrentQueue<Vector3[]> _vector3Pool = new();
        private static readonly ConcurrentQueue<bool[]> _boolPool = new();
        
        public static Vector3[] RentVector3Array(int size)
        {
            if (_vector3Pool.TryDequeue(out var array) && array.Length >= size)
                return array;
            return new Vector3[Math.Max(size, 32)];
        }

        public static void ReturnVector3Array(Vector3[] array)
        {
            if (array != null && array.Length >= 16 && _vector3Pool.Count < 20)
                _vector3Pool.Enqueue(array);
        }

        public static bool[] RentBoolArray(int size)
        {
            if (_boolPool.TryDequeue(out var array) && array.Length >= size)
            {
                Array.Fill(array, false, 0, Math.Min(array.Length, size));
                return array;
            }
            return new bool[Math.Max(size, 16)];
        }

        public static void ReturnBoolArray(bool[] array)
        {
            if (array != null && array.Length >= 6 && _boolPool.Count < 30)
                _boolPool.Enqueue(array);
        }
    }
}