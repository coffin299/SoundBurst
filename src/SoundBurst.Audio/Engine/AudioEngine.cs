using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using SoundBurst.Audio.Devices;
using SoundBurst.Audio.Mixing;

namespace SoundBurst.Audio.Engine;

public sealed class AudioEngine : IDisposable
{
    public const int SampleRate = 48000;
    public const int Channels = 2;

    private readonly AudioDeviceEnumeratorService _devices = new();
    private readonly object _clipGate = new();
    private readonly List<ClipPlaybackProvider> _activeClips = [];

    private WasapiCapture? _micCapture;
    private WasapiOut? _virtualOut;
    private WasapiOut? _monitorOut;
    private EngineMixer? _mixer;
    private BufferedMicProvider? _micProvider;
    private VolumeSampleProvider? _monitorVolume;
    private WaveFormat? _engineFormat;
    private string? _virtualDeviceId;
    private bool _running;
    private bool _disposed;
    private CancellationTokenSource? _levelCts;

    public bool IsRunning => _running;
    public float MicPeak => _micProvider?.LastPeak ?? 0f;
    public float MixPeak => _mixer?.MixPeak ?? 0f;
    public float MicVolume { get; private set; } = 1f;
    public float MonitorVolume { get; private set; } = 0.8f;
    public bool MonitorEnabled { get; private set; } = true;
    public float MasterClipVolume { get; private set; } = 1f;

    public event EventHandler? LevelsChanged;

    public void Start(Core.Models.AppSettings settings)
    {
        if (_running)
        {
            Stop();
        }

        _engineFormat = WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, Channels);
        _mixer = new EngineMixer(_engineFormat);
        _micProvider = new BufferedMicProvider(_engineFormat);
        _mixer.AddMixerInput(_micProvider);

        MicVolume = settings.MicVolume;
        MonitorVolume = settings.MonitorVolume;
        MonitorEnabled = settings.MonitorEnabled;
        MasterClipVolume = settings.MasterClipVolume;
        _micProvider.Volume = MicVolume;

        var micId = settings.MicDeviceId ?? _devices.ResolveDefaultCaptureId()
            ?? throw new InvalidOperationException("No capture (microphone) device is available.");

        _virtualDeviceId = settings.VirtualOutputDeviceId
                           ?? _devices.ResolveVirtualOutputDeviceId()
                           ?? _devices.ResolveDefaultRenderId()
                           ?? throw new InvalidOperationException("No render device is available.");

        StartMic(micId);

        var monitorId = settings.MonitorDeviceId ?? _devices.ResolveDefaultRenderId();
        var useSeparateMonitor = MonitorEnabled
                                 && monitorId is not null
                                 && !string.Equals(monitorId, _virtualDeviceId, StringComparison.Ordinal);

        if (useSeparateMonitor)
        {
            var tee = new TeeSampleProvider(_mixer, out var branch);
            _monitorVolume = new VolumeSampleProvider(branch)
            {
                Volume = MonitorEnabled ? MonitorVolume : 0f
            };

            InitRender(ref _virtualOut, _virtualDeviceId, tee.ToWaveProvider());
            InitRender(ref _monitorOut, monitorId!, _monitorVolume.ToWaveProvider());
        }
        else
        {
            InitRender(ref _virtualOut, _virtualDeviceId, _mixer.ToWaveProvider());
        }

        _running = true;
        _levelCts = new CancellationTokenSource();
        _ = LevelPumpAsync(_levelCts.Token);
    }

    public void Stop()
    {
        _running = false;
        _levelCts?.Cancel();
        _levelCts = null;

        lock (_clipGate)
        {
            foreach (var clip in _activeClips)
            {
                clip.Dispose();
            }

            _activeClips.Clear();
        }

        if (_micCapture is not null)
        {
            _micCapture.DataAvailable -= OnMicData;
            try { _micCapture.StopRecording(); } catch { /* ignore */ }
            _micCapture.Dispose();
            _micCapture = null;
        }

        StopOut(ref _virtualOut);
        StopOut(ref _monitorOut);

        _mixer?.ClearMixerInputs();
        _mixer = null;
        _micProvider = null;
        _monitorVolume = null;
        _virtualDeviceId = null;
    }

    public void SetMicVolume(float volume)
    {
        MicVolume = Math.Clamp(volume, 0f, 2f);
        if (_micProvider is not null)
        {
            _micProvider.Volume = MicVolume;
        }
    }

    public void SetMonitorVolume(float volume)
    {
        MonitorVolume = Math.Clamp(volume, 0f, 2f);
        if (_monitorVolume is not null)
        {
            _monitorVolume.Volume = MonitorEnabled ? MonitorVolume : 0f;
        }
    }

    public void SetMonitorEnabled(bool enabled)
    {
        MonitorEnabled = enabled;
        if (_monitorVolume is not null)
        {
            _monitorVolume.Volume = enabled ? MonitorVolume : 0f;
        }
    }

    public void SetMasterClipVolume(float volume)
    {
        MasterClipVolume = Math.Clamp(volume, 0f, 2f);
    }

    public void PlayClip(Core.Models.SoundClip clip)
    {
        if (!_running || _mixer is null || _engineFormat is null)
        {
            return;
        }

        if (!File.Exists(clip.FilePath))
        {
            return;
        }

        try
        {
            var volume = Math.Clamp(clip.Volume * MasterClipVolume, 0f, 2f);
            var provider = new ClipPlaybackProvider(clip.FilePath, _engineFormat, volume)
            {
                ClipId = clip.Id
            };

            lock (_clipGate)
            {
                _activeClips.Add(provider);
            }

            _mixer.AddMixerInput(provider);
        }
        catch
        {
            // Ignore decode errors for individual clips.
        }
    }

    public void StopClip(Guid clipId)
    {
        if (_mixer is null)
        {
            return;
        }

        lock (_clipGate)
        {
            var matches = _activeClips.Where(c => c.ClipId == clipId).ToList();
            foreach (var clip in matches)
            {
                _mixer.RemoveMixerInput(clip);
                _activeClips.Remove(clip);
                clip.Dispose();
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        _disposed = true;
    }

    private void StartMic(string deviceId)
    {
        using var enumerator = new MMDeviceEnumerator();
        var device = enumerator.GetDevice(deviceId);
        _micCapture = new WasapiCapture(device) { ShareMode = AudioClientShareMode.Shared };
        _micCapture.DataAvailable += OnMicData;
        _micCapture.StartRecording();
    }

    private static void InitRender(ref WasapiOut? target, string deviceId, IWaveProvider provider)
    {
        using var enumerator = new MMDeviceEnumerator();
        var device = enumerator.GetDevice(deviceId);
        target = new WasapiOut(device, AudioClientShareMode.Shared, true, 40);
        target.Init(provider);
        target.Play();
    }

    private static void StopOut(ref WasapiOut? output)
    {
        if (output is null)
        {
            return;
        }

        try { output.Stop(); } catch { /* ignore */ }
        output.Dispose();
        output = null;
    }

    private void OnMicData(object? sender, WaveInEventArgs e)
    {
        if (_micProvider is null || _micCapture is null || e.BytesRecorded == 0)
        {
            return;
        }

        try
        {
            var sourceFormat = _micCapture.WaveFormat;
            var raw = new RawSourceWaveStream(e.Buffer, 0, e.BytesRecorded, sourceFormat);
            ISampleProvider samples;

            if (sourceFormat.Encoding == WaveFormatEncoding.IeeeFloat
                && sourceFormat.SampleRate == SampleRate
                && sourceFormat.Channels == Channels)
            {
                samples = raw.ToSampleProvider();
            }
            else
            {
                var target = WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, Channels);
                var resampler = new MediaFoundationResampler(raw, target) { ResamplerQuality = 60 };
                samples = resampler.ToSampleProvider();
            }

            var buffer = new float[Math.Max(1, e.BytesRecorded / 2)];
            var read = samples.Read(buffer, 0, buffer.Length);
            if (read > 0)
            {
                _micProvider.Write(buffer, read);
                _mixer?.ReportMicPeak(_micProvider.LastPeak);
            }
        }
        catch
        {
            // Avoid killing the capture callback on transient format issues.
        }

        CleanupFinishedClips();
    }

    private void CleanupFinishedClips()
    {
        if (_mixer is null)
        {
            return;
        }

        lock (_clipGate)
        {
            for (var i = _activeClips.Count - 1; i >= 0; i--)
            {
                var clip = _activeClips[i];
                if (!clip.IsFinished)
                {
                    continue;
                }

                _mixer.RemoveMixerInput(clip);
                _activeClips.RemoveAt(i);
                clip.Dispose();
            }
        }
    }

    private async Task LevelPumpAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested && _running)
            {
                LevelsChanged?.Invoke(this, EventArgs.Empty);
                await Task.Delay(50, token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // expected
        }
    }
}

internal sealed class TeeSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly BufferedMicProvider _branch;

    public TeeSampleProvider(ISampleProvider source, out ISampleProvider branch)
    {
        _source = source;
        _branch = new BufferedMicProvider(source.WaveFormat);
        branch = _branch;
    }

    public WaveFormat WaveFormat => _source.WaveFormat;

    public int Read(float[] buffer, int offset, int count)
    {
        var read = _source.Read(buffer, offset, count);
        if (read > 0)
        {
            var copy = new float[read];
            Array.Copy(buffer, offset, copy, 0, read);
            _branch.Write(copy, read);
        }

        return read;
    }
}
