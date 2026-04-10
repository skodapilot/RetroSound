// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

namespace RetroSound.Core.Playback.Tracker;

/// <summary>
/// Represents one channel command applied when a pattern row begins.
/// </summary>
internal sealed class SingleChipChannelCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SingleChipChannelCommand"/> class.
    /// </summary>
    /// <param name="tonePeriod">The optional 12-bit tone period to assign to the channel.</param>
    /// <param name="volume">The optional 4-bit volume level to assign to the channel.</param>
    /// <param name="toneEnabled">The optional tone enable state for the channel.</param>
    /// <param name="noiseEnabled">The optional noise enable state for the channel.</param>
    /// <param name="useEnvelope">The optional envelope flag for the channel volume register.</param>
    /// <param name="effect">The per-tick effect to apply after each emitted tick.</param>
    public SingleChipChannelCommand(
        int? tonePeriod = null,
        int? volume = null,
        bool? toneEnabled = null,
        bool? noiseEnabled = null,
        bool? useEnvelope = null,
        SingleChipChannelEffect effect = default)
    {
        if (tonePeriod is < 0 or > 0x0FFF)
        {
            throw new ArgumentOutOfRangeException(nameof(tonePeriod), "Tone period must be between 0 and 4095.");
        }

        if (volume is < 0 or > 0x0F)
        {
            throw new ArgumentOutOfRangeException(nameof(volume), "Volume must be between 0 and 15.");
        }

        TonePeriod = tonePeriod;
        Volume = volume;
        ToneEnabled = toneEnabled;
        NoiseEnabled = noiseEnabled;
        UseEnvelope = useEnvelope;
        Effect = effect;
    }

    /// <summary>
    /// Gets the optional tone period assigned when the row starts.
    /// </summary>
    public int? TonePeriod { get; }

    /// <summary>
    /// Gets the optional volume assigned when the row starts.
    /// </summary>
    public int? Volume { get; }

    /// <summary>
    /// Gets the optional tone enable state assigned when the row starts.
    /// </summary>
    public bool? ToneEnabled { get; }

    /// <summary>
    /// Gets the optional noise enable state assigned when the row starts.
    /// </summary>
    public bool? NoiseEnabled { get; }

    /// <summary>
    /// Gets the optional envelope flag assigned when the row starts.
    /// </summary>
    public bool? UseEnvelope { get; }

    /// <summary>
    /// Gets the per-tick effect applied after each emitted tick.
    /// </summary>
    public SingleChipChannelEffect Effect { get; }
}
