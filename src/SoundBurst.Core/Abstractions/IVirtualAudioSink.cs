namespace SoundBurst.Core.Abstractions;

/// <summary>
/// Thin abstraction over the virtual audio output path (SoundBurst Speakers).
/// </summary>
public interface IVirtualAudioSink
{
    string PreferredDeviceName { get; }
    string PreferredCaptureDeviceName { get; }
    bool IsDriverPresent();
    bool IsTestSigningEnabled();
}
