// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

namespace RetroSound.Core.Formats.Pt3;

/// <summary>
/// Represents one four-byte PT3 sample step.
/// </summary>
public sealed class Pt3SampleStep
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Pt3SampleStep"/> class.
    /// </summary>
    /// <param name="flags">The first raw sample step byte.</param>
    /// <param name="mixerAndVolume">The second raw sample step byte.</param>
    /// <param name="toneOffset">The signed tone offset for the step.</param>
    public Pt3SampleStep(byte flags, byte mixerAndVolume, short toneOffset)
    {
        Flags = flags;
        MixerAndVolume = mixerAndVolume;
        ToneOffset = toneOffset;
    }

    /// <summary>
    /// Gets the first raw sample step byte.
    /// </summary>
    public byte Flags { get; }

    /// <summary>
    /// Gets the second raw sample step byte.
    /// </summary>
    public byte MixerAndVolume { get; }

    /// <summary>
    /// Gets the tone offset encoded by the step.
    /// </summary>
    public short ToneOffset { get; }

    /// <summary>
    /// Gets a value indicating whether the step masks AY envelope usage.
    /// </summary>
    public bool EnvelopeMasked => (Flags & 0x01) != 0;

    /// <summary>
    /// Gets a value indicating whether the step enables AY envelope usage.
    /// </summary>
    public bool UseEnvelope => !EnvelopeMasked;

    /// <summary>
    /// Gets the signed 5-bit noise or envelope offset stored in the step.
    /// </summary>
    public int NoiseOrEnvelopeOffset
    {
        get
        {
            var raw = (Flags >> 1) & 0x1F;
            return (raw & 0x10) != 0 ? raw - 0x20 : raw;
        }
    }

    /// <summary>
    /// Gets the low 4-bit volume nibble stored in the step.
    /// </summary>
    public byte Volume => (byte)(MixerAndVolume & 0x0F);

    /// <summary>
    /// Gets a value indicating whether the step masks noise output.
    /// </summary>
    public bool NoiseMasked => (MixerAndVolume & 0x80) != 0;

    /// <summary>
    /// Gets a value indicating whether tone accumulation is requested by this step.
    /// </summary>
    public bool AccumulateTone => (MixerAndVolume & 0x40) != 0;

    /// <summary>
    /// Gets a value indicating whether noise or envelope offset accumulation is requested by this step.
    /// </summary>
    public bool AccumulateNoiseOrEnvelope => (MixerAndVolume & 0x20) != 0;

    /// <summary>
    /// Gets a value indicating whether the step masks tone output.
    /// </summary>
    public bool ToneMasked => (MixerAndVolume & 0x10) != 0;
}