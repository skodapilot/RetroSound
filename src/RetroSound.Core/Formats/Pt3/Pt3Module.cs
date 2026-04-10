// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

namespace RetroSound.Core.Formats.Pt3;

/// <summary>
/// Represents a parsed PT3 tracker module.
/// </summary>
public sealed class Pt3Module
{
    private readonly IReadOnlyList<int> _order;
    private readonly IReadOnlyList<Pt3Pattern> _patterns;
    private readonly IReadOnlyList<Pt3Sample> _samples;
    private readonly IReadOnlyList<Pt3Ornament> _ornaments;

    /// <summary>
    /// Initializes a new instance of the <see cref="Pt3Module"/> class.
    /// </summary>
    /// <param name="metadata">The module metadata.</param>
    /// <param name="frequencyTable">The declared tone table.</param>
    /// <param name="tempo">The module tempo value from the PT3 header.</param>
    /// <param name="restartPositionIndex">The zero-based order index used when the song loops.</param>
    /// <param name="order">The zero-based pattern order list.</param>
    /// <param name="patterns">The parsed patterns referenced by the order list.</param>
    /// <param name="samples">The parsed sample definitions present in the file.</param>
    /// <param name="ornaments">The parsed ornament definitions present in the file.</param>
    public Pt3Module(
        Pt3ModuleMetadata metadata,
        Pt3FrequencyTableKind frequencyTable,
        byte tempo,
        int restartPositionIndex,
        IEnumerable<int> order,
        IEnumerable<Pt3Pattern> patterns,
        IEnumerable<Pt3Sample> samples,
        IEnumerable<Pt3Ornament> ornaments)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(patterns);
        ArgumentNullException.ThrowIfNull(samples);
        ArgumentNullException.ThrowIfNull(ornaments);

        var orderArray = order.ToArray();
        var patternArray = patterns.ToArray();
        var sampleArray = samples.ToArray();
        var ornamentArray = ornaments.ToArray();

        if (orderArray.Length == 0)
        {
            throw new ArgumentException("A PT3 module must contain at least one order entry.", nameof(order));
        }

        if (tempo == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tempo), "The PT3 tempo must be greater than zero.");
        }

        if (restartPositionIndex < 0 || restartPositionIndex >= orderArray.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(restartPositionIndex), "The restart position must point to an existing order entry.");
        }

        Metadata = metadata;
        FrequencyTable = frequencyTable;
        Tempo = tempo;
        RestartPositionIndex = restartPositionIndex;
        _order = Array.AsReadOnly(orderArray);
        _patterns = Array.AsReadOnly(patternArray);
        _samples = Array.AsReadOnly(sampleArray);
        _ornaments = Array.AsReadOnly(ornamentArray);
    }

    /// <summary>
    /// Gets the human-readable module metadata.
    /// </summary>
    public Pt3ModuleMetadata Metadata { get; }

    /// <summary>
    /// Gets the declared tone table.
    /// </summary>
    public Pt3FrequencyTableKind FrequencyTable { get; }

    /// <summary>
    /// Gets the tempo value stored in the PT3 header.
    /// </summary>
    public byte Tempo { get; }

    /// <summary>
    /// Gets the zero-based order index used when the song loops.
    /// </summary>
    public int RestartPositionIndex { get; }

    /// <summary>
    /// Gets the zero-based pattern order list.
    /// </summary>
    public IReadOnlyList<int> Order => _order;

    /// <summary>
    /// Gets the parsed patterns referenced by the order list.
    /// </summary>
    public IReadOnlyList<Pt3Pattern> Patterns => _patterns;

    /// <summary>
    /// Gets the parsed sample definitions present in the file.
    /// </summary>
    public IReadOnlyList<Pt3Sample> Samples => _samples;

    /// <summary>
    /// Gets the parsed ornament definitions present in the file.
    /// </summary>
    public IReadOnlyList<Pt3Ornament> Ornaments => _ornaments;
}