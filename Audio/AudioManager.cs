// The audio manager for the game. It lets you play sounds / manages what sounds should be played. | DA | 8/1/25
using SFML.Audio;
using System;
using System.Collections.Generic;
using VoxelGame.Blocks;

namespace VoxelGame.Audio
{
    public class AudioManager : IDisposable
    {
        private Dictionary<string, SoundBuffer> mSoundBuffers;
        private List<Sound> mActiveSounds;
        private bool mDisposed = false;

        public AudioManager()
        {
            mSoundBuffers = new Dictionary<string, SoundBuffer>();
            mActiveSounds = new List<Sound>();
            Console.WriteLine("SFML Audio initialized");
        }

        public void PlayAudio(string filePath, bool loop = true)
        {
            if (mDisposed) return;

            try
            {
                CleanupFinishedSounds();

                if (!mSoundBuffers.ContainsKey(filePath))
                {
                    var buffer = new SoundBuffer(filePath);
                    mSoundBuffers[filePath] = buffer;
                }

                var sound = new Sound(mSoundBuffers[filePath])
                {
                    Loop = loop,
                    Volume = 100f
                };

                mActiveSounds.Add(sound);
                sound.Play();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error playing audio {filePath}: {ex.Message}");
            }
        }

        public void Stop()
        {
            if (mDisposed) return;

            foreach (var sound in mActiveSounds)
            {
                try
                {
                    sound.Stop();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error stopping sound: {ex.Message}");
                }
            }
        }

        public void SetVolume(float volume)
        {
            if (mDisposed) return;

            volume = Math.Max(0f, Math.Min(100f, volume * 100f));
            foreach (var sound in mActiveSounds)
            {
                try
                {
                    sound.Volume = volume;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error setting volume: {ex.Message}");
                }
            }
        }

        public void PlayBlockBreakSound(BlockMaterial blockMaterial)
        {
            if (mDisposed) return;

            switch (blockMaterial)
            {
                case BlockMaterial.None:
                    Console.WriteLine("Trying to break block with no material! So no sound!");
                    break;
                case BlockMaterial.Dirt:
                    PlayAudio("Resources/Audio/BlockBreaking/DirtBreak.ogg", false);
                    break;
                case BlockMaterial.Stone:
                    PlayAudio("Resources/Audio/BlockBreaking/StoneBreak.ogg", false);
                    break;
                case BlockMaterial.Wooden:
                    PlayAudio("Resources/Audio/BlockBreaking/WoodenBreak.ogg", false);
                    break;
                case BlockMaterial.Leaves:
                    PlayAudio("Resources/Audio/BlockBreaking/LeavesBreak.ogg", false);
                    break;
                case BlockMaterial.Wool:
                    PlayAudio("Resources/Audio/BlockBreaking/WoolBreak.ogg", false);
                    break;
                case BlockMaterial.Sand:
                    PlayAudio("Resources/Audio/BlockBreaking/SandBreak.ogg", false);
                    break;
                case BlockMaterial.Glass:
                    PlayAudio("Resources/Audio/BlockBreaking/GlassBreak.ogg", false);
                    break;
            }
        }

        public void CleanupFinishedSounds()
        {
            if (mDisposed) return;

            for (int i = mActiveSounds.Count - 1; i >= 0; i--)
            {
                try
                {
                    if (mActiveSounds[i].Status == SoundStatus.Stopped)
                    {
                        mActiveSounds[i].Dispose();
                        mActiveSounds.RemoveAt(i);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error cleaning up sound: {ex.Message}");
                    mActiveSounds.RemoveAt(i);
                }
            }
        }

        public void Dispose()
        {
            if (mDisposed) return;

            Console.WriteLine("Disposing AudioManager...");

            try
            {
                // Stop all sounds
                foreach (var sound in mActiveSounds)
                {
                    try
                    {
                        sound.Stop();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error stopping sound during dispose: {ex.Message}");
                    }
                }

                System.Threading.Thread.Sleep(50);

                // Dispose all sounds
                foreach (var sound in mActiveSounds)
                {
                    try
                    {
                        sound.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error disposing sound: {ex.Message}");
                    }
                }
                mActiveSounds.Clear();

                // Dispose all sound buffers
                foreach (var buffer in mSoundBuffers.Values)
                {
                    try
                    {
                        buffer.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error disposing sound buffer: {ex.Message}");
                    }
                }
                mSoundBuffers.Clear();

                Console.WriteLine("AudioManager disposed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during AudioManager disposal: {ex.Message}");
            }
            finally
            {
                mDisposed = true;
                GC.SuppressFinalize(this);
            }
        }

        // Backup
        ~AudioManager()
        {
            if (!mDisposed)
            {
                Console.WriteLine("AudioManager finalizer called - dispose was not called properly!");
                Dispose();
            }
        }
    }
}