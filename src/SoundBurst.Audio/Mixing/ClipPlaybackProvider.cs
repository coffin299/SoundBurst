using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace SoundBurst.Audio.Mixing;

public sealed class ClipPlaybackProvider : ISampleProvider, IDisposable
{
    private readonly ISampleProvider _source;
    private readonly AudioFileReader _reader;
    private bool _finished;

    public ClipPlaybackProvider(string filePath, WaveFormat targetFormat, float volume)
    {
        _reader = new AudioFileReader(filePath) { Volume = volume };
        ISampleProvider provider = _reader;

        if (provider.WaveFormat.Channels == 1 && targetFormat.Channels == 2)
        {
            provider = new MonoToStereoSampleProvider(provider);
        }
        else if (provider.WaveFormat.Channels == 2 && targetFormat.Channels == 1)
        {
            provider = new StereoToMonoSampleProvider(provider);
        }

        if (provider.WaveFormat.SampleRate != targetFormat.SampleRate)
        {
            provider = new WdlResamplingSampleProvider(provider, targetFormat.SampleRate);
        }

        _source = provider;
    }

    public WaveFormat WaveFormat => _source.WaveFormat;
    public bool IsFinished => _finished;
    public Guid ClipId { get; init; }

    public int Read(float[] buffer, int offset, int count)
    {
        if (_finished)
        {
            Array.Clear(buffer, offset, count);
            return 0;
        }

        var read = _source.Read(buffer, offset, count);
        if (read == 0)
        {
            _finished = true;
            return 0;
        }

        if (read < count)
        {
            _finished = true;
            Array.Clear(buffer, offset + read, count - read);
        }

        return read;
    }

    public void Dispose() => _reader.Dispose();
}
