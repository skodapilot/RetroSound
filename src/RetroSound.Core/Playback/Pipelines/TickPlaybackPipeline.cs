// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using RetroSound.Core.Playback;
using RetroSound.Core.Rendering;
namespace RetroSound.Core.Playback.Pipelines;

/// <summary>
/// Coordinates single-chip tick-based playback and PCM rendering for one logical tick at a time.
/// </summary>
internal sealed class TickPlaybackPipeline
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TickPlaybackPipeline"/> class.
    /// </summary>
    /// <param name="player">The tick-based playback source.</param>
    /// <param name="emulator">The chip emulator used for PCM rendering.</param>
    public TickPlaybackPipeline(ITickPlayer player, IChipEmulator emulator)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
        Emulator = emulator ?? throw new ArgumentNullException(nameof(emulator));
    }

    /// <summary>
    /// Gets the tick-based playback source.
    /// </summary>
    public ITickPlayer Player { get; }

    /// <summary>
    /// Gets the chip emulator used for PCM rendering.
    /// </summary>
    public IChipEmulator Emulator { get; }

    /// <summary>
    /// Resets the player and emulator to their initial state.
    /// </summary>
    public void Reset()
    {
        Player.Reset();
        Emulator.Reset();
    }

    /// <summary>
    /// Advances the player by one tick and renders the resulting PCM sample frames.
    /// </summary>
    /// <param name="destination">The destination buffer for interleaved PCM samples.</param>
    /// <param name="sampleFramesWritten">The number of PCM sample frames written.</param>
    /// <returns><see langword="true"/> when a playback tick was rendered; otherwise, <see langword="false"/>.</returns>
    public bool TryRenderNextTick(Span<float> destination, out int sampleFramesWritten)
    {
        if (!Player.TryAdvance(out var frame))
        {
            sampleFramesWritten = 0;
            return false;
        }

        sampleFramesWritten = Emulator.Render(frame, Player.Timing, destination);
        return true;
    }
}
