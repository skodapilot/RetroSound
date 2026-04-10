// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

namespace RetroSound.Core.Models;

/// <summary>
/// Represents parsed TurboSound container data before tracker playback begins.
/// </summary>
public sealed class TurboSoundContainer
{
    private readonly IReadOnlyList<TurboSoundContainerEntry> _entries;

    /// <summary>
    /// Gets the fixed number of logical modules stored by a TurboSound container.
    /// </summary>
    public const int ModuleCount = 2;

    /// <summary>
    /// Initializes a new instance of the <see cref="TurboSoundContainer"/> class.
    /// </summary>
    /// <param name="title">The optional human-readable title of the container, if available from the source format.</param>
    /// <param name="firstModule">The first logical module payload.</param>
    /// <param name="secondModule">The second logical module payload.</param>
    public TurboSoundContainer(
        string? title,
        TurboSoundContainerEntry firstModule,
        TurboSoundContainerEntry secondModule)
    {
        ArgumentNullException.ThrowIfNull(firstModule);
        ArgumentNullException.ThrowIfNull(secondModule);

        Title = title;
        FirstModule = firstModule;
        SecondModule = secondModule;
        _entries = Array.AsReadOnly([firstModule, secondModule]);
    }

    /// <summary>
    /// Gets the optional human-readable title of the container.
    /// The current TS parser does not extract this value yet.
    /// </summary>
    public string? Title { get; }

    /// <summary>
    /// Gets the first logical module payload.
    /// </summary>
    public TurboSoundContainerEntry FirstModule { get; }

    /// <summary>
    /// Gets the second logical module payload.
    /// </summary>
    public TurboSoundContainerEntry SecondModule { get; }

    /// <summary>
    /// Gets the logical module payloads in playback order.
    /// </summary>
    public IReadOnlyList<TurboSoundContainerEntry> Entries => _entries;
}