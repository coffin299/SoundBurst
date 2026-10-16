namespace SoundBurst.Core.Models;

public sealed class SoundClip
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public float Volume { get; set; } = 1.0f;
    public string? Hotkey { get; set; }
}
