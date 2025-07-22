using System.Runtime.CompilerServices;

namespace VoxelGame.World
{
    public static class NoiseGenerator
    {

        private static int seed = 0;

        public static void SetSeed(int newSeed)
        {
            seed = newSeed;
        }

        public static void SetRandomSeed()
        {
            seed = (int)DateTime.Now.Ticks & 0x7FFFFFFF;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Noise(float x, float z)
        {
            int n = (int)x + (int)z * 57 + seed;
            n = n << 13 ^ n;
            return 1.0f - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824.0f;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SmoothNoise(float x, float z)
        {
            float corners = (Noise(x - 1, z - 1) + Noise(x + 1, z - 1) + Noise(x - 1, z + 1) + Noise(x + 1, z + 1)) * 0.0625f;
            float sides = (Noise(x - 1, z) + Noise(x + 1, z) + Noise(x, z - 1) + Noise(x, z + 1)) * 0.125f;
            float center = Noise(x, z) * 0.25f;
            return corners + sides + center;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InterpolatedNoise(float x, float z)
        {
            int intX = (int)x;
            float fracX = x - intX;
            int intZ = (int)z;
            float fracZ = z - intZ;

            float v1 = SmoothNoise(intX, intZ);
            float v2 = SmoothNoise(intX + 1, intZ);
            float v3 = SmoothNoise(intX, intZ + 1);
            float v4 = SmoothNoise(intX + 1, intZ + 1);

            float i1 = v1 * (1 - fracX) + v2 * fracX;
            float i2 = v3 * (1 - fracX) + v4 * fracX;

            return i1 * (1 - fracZ) + i2 * fracZ;
        }

        public static float PerlinNoise(float x, float z)
        {
            float total = 0;
            const float persistence = 0.5f;
            const int octaves = 4;

            float amplitude = 1.0f;
            float frequency = 1.0f;

            for (int i = 0; i < octaves; i++)
            {
                total += InterpolatedNoise(x * frequency, z * frequency) * amplitude;
                amplitude *= persistence;
                frequency *= 2.0f;
            }

            return total;
        }

        // ? - No idea
        public static float CaveNoise(float x, float y, float z)
        {
            int xi = FastFloor(x);
            int yi = FastFloor(y);
            int zi = FastFloor(z);

            float xf = x - xi;
            float yf = y - yi;
            float zf = z - zi;

            float n000 = Noise3D(xi, yi, zi);
            float n001 = Noise3D(xi, yi, zi + 1);
            float n010 = Noise3D(xi + 1, yi, zi);
            float n011 = Noise3D(xi + 1, yi, zi + 1);
            float n100 = Noise3D(xi, yi + 1, zi);
            float n101 = Noise3D(xi, yi + 1, zi + 1);
            float n110 = Noise3D(xi + 1, yi + 1, zi);
            float n111 = Noise3D(xi + 1, yi + 1, zi + 1);

            float nx00 = Lerp(n000, n010, xf);
            float nx01 = Lerp(n001, n011, xf);
            float nx10 = Lerp(n100, n110, xf);
            float nx11 = Lerp(n101, n111, xf);

            float nxy0 = Lerp(nx00, nx10, yf);
            float nxy1 = Lerp(nx01, nx11, yf);

            return Lerp(nxy0, nxy1, zf);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int FastFloor(float x)
        {
            int xi = (int)x;
            return x < xi ? xi - 1 : xi;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Noise3D(int x, int y, int z)
        {
            int n = x + y * 57 + z * 997 + seed;
            n = n << 13 ^ n;
            return 1.0f - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824.0f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Lerp(float a, float b, float t)
        {
            return a + t * (b - a);
        }
    }
}