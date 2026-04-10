// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

namespace RetroSound.Core.Models;

/// <summary>
/// Represents one logical module payload stored inside a TurboSound container.
/// </summary>
public sealed record TurboSoundContainerEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TurboSoundContainerEntry"/> class.
    /// </summary>
    /// <param name="slotIndex">The zero-based slot index inside the container.</param>
    /// <param name="formatHint">The optional format hint for the payload.</param>
    /// <param name="payload">The raw payload bytes for the slot.</param>
    public TurboSoundContainerEntry(int slotIndex, string? formatHint, ReadOnlyMemory<byte> payload)
    {
        if (slotIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(slotIndex), "Slot index cannot be negative.");
        }

        SlotIndex = slotIndex;
        FormatHint = formatHint;
        Payload = payload;
    }

    /// <summary>
    /// Gets the zero-based slot index inside the container.
    /// </summary>
    public int SlotIndex { get; }

    /// <summary>
    /// Gets the optional format hint for the payload.
    /// </summary>
    public string? FormatHint { get; }

    /// <summary>
    /// Gets the raw payload bytes for the slot.
    /// </summary>
    public ReadOnlyMemory<byte> Payload { get; }
}