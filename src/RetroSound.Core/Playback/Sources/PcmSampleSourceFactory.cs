// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using RetroSound.Core.Audio;
using RetroSound.Core.Playback;
using RetroSound.Core.Playback.Pipelines;
using RetroSound.Core.Playback.Sources;
using RetroSound.Core.Rendering;
namespace RetroSound.Core.Audio;

/// <summary>
/// Creates host-neutral PCM sample sources from RetroSound playback components.
/// </summary>
public static class PcmSampleSourceFactory
{
    /// <summary>
    /// Creates a pull-based PCM sample source for single-chip playback.
    /// </summary>
    /// <param name="player">The tick player that produces AY/YM register frames.</param>
    /// <param name="emulator">The emulator that renders PCM samples for each tick.</param>
    /// <returns>A PCM sample source that pulls audio from the provided playback graph.</returns>
    public static IPcmSampleSource Create(ITickPlayer player, IChipEmulator emulator)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(emulator);

        return new PipelinePcmSampleSource(new TickPlaybackPipeline(player, emulator));
    }

    /// <summary>
    /// Creates a pull-based PCM sample source for dual-chip playback.
    /// </summary>
    /// <param name="player">The dual-chip player that advances both AY/YM chips in lockstep.</param>
    /// <param name="chipAEmulator">The emulator that renders the first chip.</param>
    /// <param name="chipBEmulator">The emulator that renders the second chip.</param>
    /// <returns>A stereo PCM sample source that mixes both chip outputs.</returns>
    public static IPcmSampleSource Create(
        DualChipPlayer player,
        IChipEmulator chipAEmulator,
        IChipEmulator chipBEmulator)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(chipAEmulator);
        ArgumentNullException.ThrowIfNull(chipBEmulator);

        return new DualChipPcmSampleSource(new DualChipPlaybackPipeline(player, chipAEmulator, chipBEmulator));
    }
}
