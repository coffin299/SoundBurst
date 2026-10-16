# SoundBurst branding for SysVAD

After `scripts/fetch-sysvad.ps1`, apply these renames (also automated by `apply-soundburst-branding.ps1`):

## Endpoint friendly names

Locate string tables / friendly names in SysVAD tablet sample and set:

| Role | Name |
|------|------|
| Render (Speakers) | `SoundBurst Speakers` |
| Capture (Microphone) | `SoundBurst Microphone` |

## Device / service

- Device description: `SoundBurst Virtual Audio Device`
- Service name: `SoundBurstVad`
- Binary: `SoundBurstVad.sys`
- Hardware ID: `ROOT\SoundBurstVad` (or keep SysVAD HID and only rename friendly names)

## Topology goal

Minimal configuration for SoundBurst:

1. One WaveRT render pin (speakers)
2. One WaveRT capture pin (microphone)
3. Shared circular buffer: render write → capture read (virtual cable loopback)
4. Fixed 48 kHz / stereo / 16-bit or float PCM negotiation preferred

Strip Bluetooth / headphone jack / keyword detector / APO samples if present to reduce attack surface for the OSS Test Mode build.
