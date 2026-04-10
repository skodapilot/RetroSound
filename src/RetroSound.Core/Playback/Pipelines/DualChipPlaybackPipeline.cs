// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using RetroSound.Core.Playback;
using RetroSound.Core.Rendering;
namespace RetroSound.Core.Playback.Pipelines;

/// <summary>
/// Coordinates dual-chip playback and mixes both chip renders into one stereo PCM stream.
/// </summary>
/// <remarks>
/// The pipeline advances the shared music tick once, renders one tick of audio from each chip emulator, and
/// mixes the two chip outputs into a deterministic stereo block. Keeping the work tick-sized makes the render loop
/// easy to reason about and keeps host pull sizes independent from tracker timing.
/// </remarks>
internal class DualChipPlaybackPipeline
{
    private const int OutputChannelCount = 2;
    private const float PerChipMixScale = 0.5f;
    private const float StereoWidth = 1.5f;
    private readonly float[] _chipABuffer;
    private readonly float[] _chipBBuffer;
    private readonly int _sampleFramesPerTick;

    /// <summary>
    /// Initializes a new instance of the <see cref="DualChipPlaybackPipeline"/> class.
    /// </summary>
    /// <param name="player">The dual-chip playback coordinator.</param>
    /// <param name="chipAEmulator">The emulator that renders the first chip.</param>
    /// <param name="chipBEmulator">The emulator that renders the second chip.</param>
    public DualChipPlaybackPipeline(
        DualChipPlayer player,
        IChipEmulator chipAEmulator,
        IChipEmulator chipBEmulator)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
        ChipAEmulator = chipAEmulator ?? throw new ArgumentNullException(nameof(chipAEmulator));
        ChipBEmulator = chipBEmulator ?? throw new ArgumentNullException(nameof(chipBEmulator));

        ValidateChannelCount(chipAEmulator, nameof(chipAEmulator));
        ValidateChannelCount(chipBEmulator, nameof(chipBEmulator));

        _sampleFramesPerTick = player.Timing.GetSampleFramesPerTick();
        if (_sampleFramesPerTick <= 0)
        {
            throw new InvalidOperationException("Playback timing must produce at least one sample frame per tick.");
        }

        _chipABuffer = new float[_sampleFramesPerTick * chipAEmulator.ChannelCount];
        _chipBBuffer = new float[_sampleFramesPerTick * chipBEmulator.ChannelCount];
    }

    /// <summary>
    /// Gets the dual-chip playback coordinator.
    /// </summary>
    public DualChipPlayer Player { get; }

    /// <summary>
    /// Gets the emulator responsible for the first AY/YM chip.
    /// </summary>
    public IChipEmulator ChipAEmulator { get; }

    /// <summary>
    /// Gets the emulator responsible for the second AY/YM chip.
    /// </summary>
    public IChipEmulator ChipBEmulator { get; }

    /// <summary>
    /// Gets the stereo channel count produced by the mixed output.
    /// </summary>
    public int ChannelCount => OutputChannelCount;

    /// <summary>
    /// Resets the player and both chip emulators back to the start of playback.
    /// </summary>
    public void Reset()
    {
        Player.Reset();
        ChipAEmulator.Reset();
        ChipBEmulator.Reset();
    }

    /// <summary>
    /// Advances the dual-chip player by one logical tick and renders the mixed stereo PCM block for that tick.
    /// </summary>
    /// <param name="destination">The destination buffer for interleaved stereo samples.</param>
    /// <param name="sampleFramesWritten">The number of stereo sample frames written.</param>
    /// <returns><see langword="true"/> when a tick was rendered; otherwise, <see langword="false"/>.</returns>
    public bool TryRenderNextTick(Span<float> destination, out int sampleFramesWritten)
    {
        if (!Player.TryAdvance(out var frame))
        {
            sampleFramesWritten = 0;
            return false;
        }

        var requiredDestinationValueCount = _sampleFramesPerTick * OutputChannelCount;
        if (destination.Length < requiredDestinationValueCount)
        {
            throw new ArgumentException("Destination buffer is too small for one rendered dual-chip tick.", nameof(destination));
        }

        var chipAFramesWritten = ChipAEmulator.Render(frame.ChipA, Player.Timing, _chipABuffer);
        var chipBFramesWritten = ChipBEmulator.Render(frame.ChipB, Player.Timing, _chipBBuffer);

        if (chipAFramesWritten != _sampleFramesPerTick || chipBFramesWritten != _sampleFramesPerTick)
        {
            throw new InvalidOperationException("Each chip emulator must render exactly one logical tick of audio.");
        }

        MixStereo(destination[..requiredDestinationValueCount]);
        sampleFramesWritten = _sampleFramesPerTick;
        return true;
    }

    private void MixStereo(Span<float> destination)
    {
        for (var frameIndex = 0; frameIndex < _sampleFramesPerTick; frameIndex++)
        {
            var mixedLeft = PerChipMixScale * (ReadChipOutputSample(_chipABuffer, ChipAEmulator.ChannelCount, frameIndex, 0)
                                               + ReadChipOutputSample(_chipBBuffer, ChipBEmulator.ChannelCount, frameIndex, 0));
            var mixedRight = PerChipMixScale * (ReadChipOutputSample(_chipABuffer, ChipAEmulator.ChannelCount, frameIndex, 1)
                                                + ReadChipOutputSample(_chipBBuffer, ChipBEmulator.ChannelCount, frameIndex, 1));
            var (widenedLeft, widenedRight) = ApplyStereoWidth(mixedLeft, mixedRight);

            var destinationIndex = frameIndex * OutputChannelCount;
            destination[destinationIndex] = widenedLeft;
            destination[destinationIndex + 1] = widenedRight;
        }
    }

    private static (float Left, float Right) ApplyStereoWidth(float left, float right)
    {
        var mid = (left + right) * 0.5f;
        var side = (left - right) * 0.5f * StereoWidth;
        return (mid + side, mid - side);
    }

    private static float ReadChipOutputSample(
        ReadOnlySpan<float> source,
        int sourceChannelCount,
        int frameIndex,
        int channelIndex)
    {
        var sourceIndex = sourceChannelCount == 1
            ? frameIndex
            : (frameIndex * sourceChannelCount) + channelIndex;

        return source[sourceIndex];
    }

    private static void ValidateChannelCount(IChipEmulator emulator, string paramName)
    {
        if (emulator.ChannelCount is < 1 or > 2)
        {
            throw new ArgumentException("Each chip emulator must produce mono or stereo PCM output.", paramName);
        }
    }
}
