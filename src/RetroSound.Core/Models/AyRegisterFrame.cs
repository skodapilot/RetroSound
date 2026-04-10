// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

namespace RetroSound.Core.Models;

/// <summary>
/// Represents one logical AY/YM register state containing the 14 standard chip registers.
/// </summary>
public sealed class AyRegisterFrame
{
    private readonly byte[] _registers;

    /// <summary>
    /// Initializes a new instance of the <see cref="AyRegisterFrame"/> class.
    /// </summary>
    /// <param name="registers">
    /// The register values in hardware order: R0-R5 tone periods, R6 noise period, R7 mixer, R8-R10 channel
    /// amplitudes, R11-R12 envelope period, and R13 envelope shape.
    /// </param>
    /// <param name="envelopeShapeWritten">
    /// Indicates whether register R13 was explicitly written for this frame. This matters because rewriting the same
    /// envelope shape value still restarts the AY/YM envelope generator.
    /// </param>
    public AyRegisterFrame(ReadOnlySpan<byte> registers, bool envelopeShapeWritten = false)
    {
        if (registers.Length != RegisterCount)
        {
            throw new ArgumentException($"An AY/YM frame must contain exactly {RegisterCount} registers.", nameof(registers));
        }

        _registers = registers.ToArray();
        EnvelopeShapeWritten = envelopeShapeWritten;
    }

    /// <summary>
    /// Gets the number of standard AY/YM registers represented by a frame.
    /// </summary>
    public const int RegisterCount = 14;

    /// <summary>
    /// Gets the register values in hardware order.
    /// </summary>
    public ReadOnlyMemory<byte> Registers => _registers;

    /// <summary>
    /// Gets a value indicating whether register R13 was explicitly written for this frame.
    /// </summary>
    public bool EnvelopeShapeWritten { get; }

    /// <summary>
    /// Gets the value of a register by zero-based index.
    /// </summary>
    /// <param name="registerIndex">The zero-based register index.</param>
    /// <returns>The value stored in the selected register.</returns>
    public byte this[int registerIndex] => _registers[registerIndex];

    /// <summary>
    /// Creates a defensive copy of the register values.
    /// </summary>
    /// <returns>A new array containing the 14 register values.</returns>
    public byte[] ToArray() => _registers.ToArray();
}