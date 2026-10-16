using NAudio.Wave;

namespace SoundBurst.Audio.Mixing;

/// <summary>
/// Thread-safe mixer that accepts float samples from mic and clips.
/// </summary>
public sealed class EngineMixer : ISampleProvider
{
    private readonly object _gate = new();
    private readonly List<ISampleProvider> _sources = [];
    private readonly WaveFormat _waveFormat;
    private readonly float[] _sourceBuffer = new float[1024 * 8];

    public EngineMixer(WaveFormat waveFormat)
    {
        if (waveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
        {
            throw new ArgumentException("EngineMixer requires IEEE float format.", nameof(waveFormat));
        }

        _waveFormat = waveFormat;
    }

    public WaveFormat WaveFormat => _waveFormat;

    public float MicPeak { get; private set; }
    public float MixPeak { get; private set; }

    public void AddMixerInput(ISampleProvider input)
    {
        if (input.WaveFormat.SampleRate != _waveFormat.SampleRate
            || input.WaveFormat.Channels != _waveFormat.Channels)
        {
            throw new ArgumentException("Mixer input format mismatch.");
        }

        lock (_gate)
        {
            _sources.Add(input);
        }
    }

    public void RemoveMixerInput(ISampleProvider input)
    {
        lock (_gate)
        {
            _sources.Remove(input);
        }
    }

    public void ClearMixerInputs()
    {
        lock (_gate)
        {
            _sources.Clear();
        }
    }

    public int Read(float[] buffer, int offset, int count)
    {
        Array.Clear(buffer, offset, count);

        List<ISampleProvider> snapshot;
        lock (_gate)
        {
            snapshot = [.. _sources];
        }

        float mixPeak = 0;
        foreach (var source in snapshot)
        {
            var remaining = count;
            var outOffset = offset;
            while (remaining > 0)
            {
                var toRead = Math.Min(remaining, _sourceBuffer.Length);
                var read = source.Read(_sourceBuffer, 0, toRead);
                if (read == 0)
                {
                    lock (_gate)
                    {
                        _sources.Remove(source);
                    }
                    break;
                }

                for (var i = 0; i < read; i++)
                {
                    var sample = buffer[outOffset + i] + _sourceBuffer[i];
                    if (sample > 1f) sample = 1f;
                    if (sample < -1f) sample = -1f;
                    buffer[outOffset + i] = sample;
                    var abs = Math.Abs(sample);
                    if (abs > mixPeak) mixPeak = abs;
                }

                remaining -= read;
                outOffset += read;
            }
        }

        MixPeak = mixPeak;
        return count;
    }

    public void ReportMicPeak(float peak) => MicPeak = peak;
}
