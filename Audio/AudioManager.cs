using NAudio.Wave;

namespace VoxelGame.Audio
{
    public class AudioManager
    {
        private WaveOutEvent outputDevice;
        private AudioFileReader audioFile;
        private string currentFilePath;
        private bool shouldLoop = false;

        public AudioManager()
        {
            outputDevice = new WaveOutEvent();
        }

        public void PlayAudio(string filePath, bool loop = true)
        {
            currentFilePath = filePath;
            shouldLoop = loop;

            outputDevice = new WaveOutEvent();
            audioFile = new AudioFileReader(filePath);

            outputDevice.PlaybackStopped += playbackStopped;

            outputDevice.Init(audioFile);
            outputDevice.Play();
        }

        private void playbackStopped(object sender, StoppedEventArgs e)
        {

            if (shouldLoop && !string.IsNullOrEmpty(currentFilePath))
            {
                Console.WriteLine("Playback Stopped!");
                audioFile?.Dispose();
                audioFile = new AudioFileReader(currentFilePath);
                outputDevice.Init(audioFile);
                outputDevice.Play();
            }
        }

        public void Stop()
        {
            shouldLoop = false;
            outputDevice?.Stop();
            outputDevice?.Dispose();
            audioFile?.Dispose();
            outputDevice = null;
            audioFile = null;
        }

        public void SetVolume(float volume)
        {
            if (audioFile != null)
                audioFile.Volume = Math.Max(0f, Math.Min(1f, volume));
        }

        public void Dispose()
        {
            Stop();
        }
    }

}
