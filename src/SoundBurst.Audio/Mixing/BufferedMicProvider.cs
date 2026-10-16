using NAudio.Wave;

namespace SoundBurst.Audio.Mixing;

/// <summary>
/// Pulls float samples written by the WASAPI capture callback.
/// </summary>
public sealed class BufferedMicProvider : ISampleProvider
{
    private readonly WaveFormat _format;
    private readonly float[] _ring;
    private int _write;
    private int _read;
    private int _available;
    private readonly object _gate = new();

    public BufferedMicProvider(WaveFormat format, int capacitySamples = 48000 * 2)
    {
        _format = format;
        _ring = new float[capacitySamples];
    }

    public WaveFormat WaveFormat => _format;
    public float Volume { get; set; } = 1f;
    public float LastPeak { get; private set; }

    public void Write(float[] samples, int count)
    {
        lock (_gate)
        {
            for (var i = 0; i < count; i++)
            {
                _ring[_write] = samples[i] * Volume;
                _write = (_write + 1) % _ring.Length;
                if (_available < _ring.Length)
                {
                    _available++;
                }
                else
                {
                    _read = (_read + 1) % _ring.Length;
                }
            }
        }
    }

    public int Read(float[] buffer, int offset, int count)
    {
        var peak = 0f;
        lock (_gate)
        {
            var toCopy = Math.Min(count, _available);
            for (var i = 0; i < toCopy; i++)
            {
                var sample = _ring[_read];
                buffer[offset + i] = sample;
                _read = (_read + 1) % _ring.Length;
                _available--;
                var abs = Math.Abs(sample);
                if (abs > peak) peak = abs;
            }

            for (var i = toCopy; i < count; i++)
            {
                buffer[offset + i] = 0f;
            }
        }

        LastPeak = peak;
        return count;
    }
}
