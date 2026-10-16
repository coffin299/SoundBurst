namespace SoundBurst.Core.Models;

public sealed class AudioDeviceInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public bool IsCapture { get; init; }
    public bool IsRender { get; init; }
}
