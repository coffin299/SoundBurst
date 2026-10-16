/*
 * SoundBurstVad — loopback design notes (SysVAD integration point)
 *
 * The production driver is built from Microsoft SysVAD (MIT license sample)
 * with SoundBurst branding. This header documents the intended data path
 * for contributors implementing / verifying the loopback path.
 *
 *   App (WASAPI) --> SoundBurst Speakers (render pin)
 *                         |
 *                    shared ring buffer (KSPIN / WaveRT)
 *                         |
 *                         v
 *   Discord etc. <-- SoundBurst Microphone (capture pin)
 *
 * Clocking: prefer a single master clock on the render side; capture
 * consumers read with light underrun zero-fill to tolerate drift.
 */

#pragma once

#define SOUNDBURST_SAMPLE_RATE   48000
#define SOUNDBURST_CHANNELS      2
#define SOUNDBURST_RING_MS       100
#define SOUNDBURST_FRIENDLY_RENDER   L"SoundBurst Speakers"
#define SOUNDBURST_FRIENDLY_CAPTURE  L"SoundBurst Microphone"
