namespace SoundBurst.Core.Models;

public sealed class AppSettings
{
    public string? MicDeviceId { get; set; }
    public string? VirtualOutputDeviceId { get; set; }
    public string? MonitorDeviceId { get; set; }
    public float MicVolume { get; set; } = 1.0f;
    public float MonitorVolume { get; set; } = 0.8f;
    public bool MonitorEnabled { get; set; } = true;
    public float MasterClipVolume { get; set; } = 1.0f;
    public List<SoundClip> Clips { get; set; } = [];
    public string GitHubRepoUrl { get; set; } = "https://github.com/coffin299/SoundBurst";
}
