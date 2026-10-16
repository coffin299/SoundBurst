using NAudio.CoreAudioApi;
using SoundBurst.Core.Models;
using SoundBurst.Audio.VirtualDevice;

namespace SoundBurst.Audio.Devices;

public sealed class AudioDeviceEnumeratorService
{
    public IReadOnlyList<AudioDeviceInfo> ListCaptureDevices()
    {
        return ListDevices(DataFlow.Capture);
    }

    public IReadOnlyList<AudioDeviceInfo> ListRenderDevices()
    {
        return ListDevices(DataFlow.Render);
    }

    public string? FindDeviceIdByName(DataFlow flow, string nameContains)
    {
        using var enumerator = new MMDeviceEnumerator();
        foreach (var device in enumerator.EnumerateAudioEndPoints(flow, DeviceState.Active))
        {
            if (device.FriendlyName.Contains(nameContains, StringComparison.OrdinalIgnoreCase))
            {
                return device.ID;
            }
        }

        return null;
    }

    public string? ResolveVirtualOutputDeviceId()
    {
        return FindDeviceIdByName(DataFlow.Render, SoundBurstVirtualSink.SpeakersName);
    }

    public string? ResolveDefaultCaptureId()
    {
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            return enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications).ID;
        }
        catch
        {
            return ListCaptureDevices().FirstOrDefault()?.Id;
        }
    }

    public string? ResolveDefaultRenderId()
    {
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            return enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).ID;
        }
        catch
        {
            return ListRenderDevices().FirstOrDefault()?.Id;
        }
    }

    private static IReadOnlyList<AudioDeviceInfo> ListDevices(DataFlow flow)
    {
        var list = new List<AudioDeviceInfo>();
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            foreach (var device in enumerator.EnumerateAudioEndPoints(flow, DeviceState.Active))
            {
                list.Add(new AudioDeviceInfo
                {
                    Id = device.ID,
                    Name = device.FriendlyName,
                    IsCapture = flow == DataFlow.Capture,
                    IsRender = flow == DataFlow.Render
                });
            }
        }
        catch
        {
            // ignored
        }

        return list;
    }
}
