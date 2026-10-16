using SoundBurst.Audio.VirtualDevice;

namespace SoundBurst.App.Services;

public sealed class DriverStatusService
{
    private readonly SoundBurstVirtualSink _sink = new();

    public DriverStatus GetStatus() => new(
        _sink.IsDriverPresent(),
        _sink.IsTestSigningEnabled(),
        _sink.PreferredDeviceName,
        _sink.PreferredCaptureDeviceName);
}

public readonly record struct DriverStatus(
    bool DriverPresent,
    bool TestSigningEnabled,
    string SpeakersName,
    string MicrophoneName);
