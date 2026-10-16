using NAudio.CoreAudioApi;
using SoundBurst.Core.Abstractions;

namespace SoundBurst.Audio.VirtualDevice;

public sealed class SoundBurstVirtualSink : IVirtualAudioSink
{
    public const string SpeakersName = "SoundBurst Speakers";
    public const string MicrophoneName = "SoundBurst Microphone";

    public string PreferredDeviceName => SpeakersName;
    public string PreferredCaptureDeviceName => MicrophoneName;

    public bool IsDriverPresent()
    {
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            {
                if (device.FriendlyName.Contains(SpeakersName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }
        catch
        {
            // ignored
        }

        return false;
    }

    public bool IsTestSigningEnabled() => QueryBcdTestSigning();

    private static bool QueryBcdTestSigning()
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "bcdedit",
                Arguments = "/enum {current}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc is null)
            {
                return false;
            }

            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(3000);
            return output.Contains("testsigning", StringComparison.OrdinalIgnoreCase)
                   && output.Contains("Yes", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
