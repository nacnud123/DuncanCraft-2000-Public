using SFML.Audio;
using System;
using System.Collections.Generic;
using VoxelGame.Blocks;

namespace VoxelGame.Audio
{
    public class AudioManager : IDisposable
    {
        private Dictionary<string, SoundBuffer> _soundBuffers;
        private List<Sound> _activeSounds;
        private bool _disposed = false;

        public AudioManager()
        {
            _soundBuffers = new Dictionary<string, SoundBuffer>();
            _activeSounds = new List<Sound>();
            Console.WriteLine("SFML Audio initialized");
        }

        public void PlayAudio(string filePath, bool loop = true)
        {
            if (_disposed) return;

            try
            {
                CleanupFinishedSounds();

                if (!_soundBuffers.ContainsKey(filePath))
                {
                    var buffer = new SoundBuffer(filePath);
                    _soundBuffers[filePath] = buffer;
                }

                var sound = new Sound(_soundBuffers[filePath])
                {
                    Loop = loop,
                    Volume = 100f
                };

                _activeSounds.Add(sound);
                sound.Play();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error playing audio {filePath}: {ex.Message}");
            }
        }

        public void Stop()
        {
            if (_disposed) return;

            foreach (var sound in _activeSounds)
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
            if (_disposed) return;

            volume = Math.Max(0f, Math.Min(100f, volume * 100f));
            foreach (var sound in _activeSounds)
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
            if (_disposed) return;

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
            if (_disposed) return;

            for (int i = _activeSounds.Count - 1; i >= 0; i--)
            {
                try
                {
                    if (_activeSounds[i].Status == SoundStatus.Stopped)
                    {
                        _activeSounds[i].Dispose();
                        _activeSounds.RemoveAt(i);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error cleaning up sound: {ex.Message}");
                    _activeSounds.RemoveAt(i);
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            Console.WriteLine("Disposing AudioManager...");

            try
            {
                // Stop all sounds first
                foreach (var sound in _activeSounds)
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
                foreach (var sound in _activeSounds)
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
                _activeSounds.Clear();

                // Dispose all sound buffers
                foreach (var buffer in _soundBuffers.Values)
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
                _soundBuffers.Clear();

                Console.WriteLine("AudioManager disposed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during AudioManager disposal: {ex.Message}");
            }
            finally
            {
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        // Backup
        ~AudioManager()
        {
            if (!_disposed)
            {
                Console.WriteLine("AudioManager finalizer called - dispose was not called properly!");
                Dispose();
            }
        }
    }
}