// Another class I am not 100% about. It generates noise, I know that, but I don't know the specifics. | DA | 8/1/25
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoxelGame.World
{
    internal class Noise : IDisposable
    {

        private bool mDisposed = false;

        public enum NoiseType
        {
            OpenSimplex2,
            Perlin,
            Value
        }

        private int mSeed = 1337;
        private float mFrequency = 0.01f;
        private NoiseType mNoiseType = NoiseType.OpenSimplex2;

        // Gradient vectors for 3D noise
        private static readonly float[] Grad3 = {
            1,1,0, -1,1,0, 1,-1,0, -1,-1,0,
            1,0,1, -1,0,1, 1,0,-1, -1,0,-1,
            0,1,1, 0,-1,1, 0,1,-1, 0,-1,-1
        };

        // Permutation table
        private readonly int[] _perm = new int[512];

        public Noise()
        {
            InitializePermutationTable();
        }

        private void InitializePermutationTable()
        {
            var random = new Random(mSeed);
            var p = new int[256];
            for (int i = 0; i < 256; i++)
                p[i] = i;

            // Shuffle
            for (int i = 255; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (p[i], p[j]) = (p[j], p[i]);
            }

            // Duplicate for easy overflow
            for (int i = 0; i < 512; i++)
                _perm[i] = p[i & 255];
        }

        public void SetSeed(int seed)
        {
            mSeed = seed;
            InitializePermutationTable();
        }

        public void SetFrequency(float frequency)
        {
            mFrequency = frequency;
        }

        public void SetNoiseType(NoiseType noiseType)
        {
            mNoiseType = noiseType;
        }

        public float GetNoise(float x, float y)
        {
            return GetNoise(x, y, 0);
        }

        public float GetNoise(float x, float y, float z)
        {
            x *= mFrequency;
            y *= mFrequency;
            z *= mFrequency;

            return mNoiseType switch
            {
                NoiseType.OpenSimplex2 => SimplexNoise(x, y, z),
                NoiseType.Perlin => PerlinNoise(x, y, z),
                NoiseType.Value => ValueNoise(x, y, z),
                _ => 0.0f
            };
        }

        private float SimplexNoise(float x, float y, float z)
        {
            float n0, n1, n2, n3;

            // Skew the input space to determine which simplex cell we're in
            float s = (x + y + z) * (1.0f / 3.0f);
            int i = FastFloor(x + s);
            int j = FastFloor(y + s);
            int k = FastFloor(z + s);

            float t = (i + j + k) * (1.0f / 6.0f);
            float X0 = i - t;
            float Y0 = j - t;
            float Z0 = k - t;
            float x0 = x - X0;
            float y0 = y - Y0;
            float z0 = z - Z0;

            // Determine which simplex we are in
            int i1, j1, k1;
            int i2, j2, k2;

            if (x0 >= y0)
            {
                if (y0 >= z0)
                {
                    i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 1; k2 = 0;
                }
                else if (x0 >= z0)
                {
                    i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 0; k2 = 1;
                }
                else
                {
                    i1 = 0; j1 = 0; k1 = 1; i2 = 1; j2 = 0; k2 = 1;
                }
            }
            else
            {
                if (y0 < z0)
                {
                    i1 = 0; j1 = 0; k1 = 1; i2 = 0; j2 = 1; k2 = 1;
                }
                else if (x0 < z0)
                {
                    i1 = 0; j1 = 1; k1 = 0; i2 = 0; j2 = 1; k2 = 1;
                }
                else
                {
                    i1 = 0; j1 = 1; k1 = 0; i2 = 1; j2 = 1; k2 = 0;
                }
            }

            float x1 = x0 - i1 + (1.0f / 6.0f);
            float y1 = y0 - j1 + (1.0f / 6.0f);
            float z1 = z0 - k1 + (1.0f / 6.0f);
            float x2 = x0 - i2 + (1.0f / 3.0f);
            float y2 = y0 - j2 + (1.0f / 3.0f);
            float z2 = z0 - k2 + (1.0f / 3.0f);
            float x3 = x0 - 1.0f + 0.5f;
            float y3 = y0 - 1.0f + 0.5f;
            float z3 = z0 - 1.0f + 0.5f;

            // Calculate the contribution from the four corners
            int ii = i & 255;
            int jj = j & 255;
            int kk = k & 255;
            int gi0 = _perm[ii + _perm[jj + _perm[kk]]] % 12;
            int gi1 = _perm[ii + i1 + _perm[jj + j1 + _perm[kk + k1]]] % 12;
            int gi2 = _perm[ii + i2 + _perm[jj + j2 + _perm[kk + k2]]] % 12;
            int gi3 = _perm[ii + 1 + _perm[jj + 1 + _perm[kk + 1]]] % 12;

            float t0 = 0.6f - x0 * x0 - y0 * y0 - z0 * z0;
            if (t0 < 0) n0 = 0.0f;
            else
            {
                t0 *= t0;
                n0 = t0 * t0 * Dot(Grad3, gi0, x0, y0, z0);
            }

            float t1 = 0.6f - x1 * x1 - y1 * y1 - z1 * z1;
            if (t1 < 0) n1 = 0.0f;
            else
            {
                t1 *= t1;
                n1 = t1 * t1 * Dot(Grad3, gi1, x1, y1, z1);
            }

            float t2 = 0.6f - x2 * x2 - y2 * y2 - z2 * z2;
            if (t2 < 0) n2 = 0.0f;
            else
            {
                t2 *= t2;
                n2 = t2 * t2 * Dot(Grad3, gi2, x2, y2, z2);
            }

            float t3 = 0.6f - x3 * x3 - y3 * y3 - z3 * z3;
            if (t3 < 0) n3 = 0.0f;
            else
            {
                t3 *= t3;
                n3 = t3 * t3 * Dot(Grad3, gi3, x3, y3, z3);
            }

            return 32.0f * (n0 + n1 + n2 + n3);
        }

        private float PerlinNoise(float x, float y, float z)
        {
            int X = FastFloor(x) & 255;
            int Y = FastFloor(y) & 255;
            int Z = FastFloor(z) & 255;

            x -= FastFloor(x);
            y -= FastFloor(y);
            z -= FastFloor(z);

            float u = Fade(x);
            float v = Fade(y);
            float w = Fade(z);

            int A = _perm[X] + Y;
            int AA = _perm[A] + Z;
            int AB = _perm[A + 1] + Z;
            int B = _perm[X + 1] + Y;
            int BA = _perm[B] + Z;
            int BB = _perm[B + 1] + Z;

            return Lerp(w, Lerp(v, Lerp(u, Grad(_perm[AA], x, y, z),
                                           Grad(_perm[BA], x - 1, y, z)),
                                   Lerp(u, Grad(_perm[AB], x, y - 1, z),
                                           Grad(_perm[BB], x - 1, y - 1, z))),
                           Lerp(v, Lerp(u, Grad(_perm[AA + 1], x, y, z - 1),
                                           Grad(_perm[BA + 1], x - 1, y, z - 1)),
                                   Lerp(u, Grad(_perm[AB + 1], x, y - 1, z - 1),
                                           Grad(_perm[BB + 1], x - 1, y - 1, z - 1))));
        }

        private float ValueNoise(float x, float y, float z)
        {
            int X = FastFloor(x) & 255;
            int Y = FastFloor(y) & 255;
            int Z = FastFloor(z) & 255;

            x -= FastFloor(x);
            y -= FastFloor(y);
            z -= FastFloor(z);

            float u = Fade(x);
            float v = Fade(y);
            float w = Fade(z);

            int A = _perm[X] + Y;
            int AA = _perm[A] + Z;
            int AB = _perm[A + 1] + Z;
            int B = _perm[X + 1] + Y;
            int BA = _perm[B] + Z;
            int BB = _perm[B + 1] + Z;

            return Lerp(w, Lerp(v, Lerp(u, _perm[AA] / 255.0f, _perm[BA] / 255.0f),
                                   Lerp(u, _perm[AB] / 255.0f, _perm[BB] / 255.0f)),
                           Lerp(v, Lerp(u, _perm[AA + 1] / 255.0f, _perm[BA + 1] / 255.0f),
                                   Lerp(u, _perm[AB + 1] / 255.0f, _perm[BB + 1] / 255.0f)));
        }

        private static float Dot(float[] g, int gi, float x, float y, float z)
        {
            return g[gi * 3] * x + g[gi * 3 + 1] * y + g[gi * 3 + 2] * z;
        }

        private static float Fade(float t)
        {
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        private static float Lerp(float t, float a, float b)
        {
            return a + t * (b - a);
        }

        private static float Grad(int hash, float x, float y, float z)
        {
            int h = hash & 15;
            float u = h < 8 ? x : y;
            float v = h < 4 ? y : h == 12 || h == 14 ? x : z;
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }

        private static int FastFloor(float x)
        {
            int xi = (int)x;
            return x < xi ? xi - 1 : xi;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!mDisposed)
            {
                if (disposing)
                {
                    Array.Clear(_perm, 0, _perm.Length);
                }
                mDisposed = true;
            }
        }

    }
}