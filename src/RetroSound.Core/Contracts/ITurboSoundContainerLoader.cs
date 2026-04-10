// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using RetroSound.Core.Models;
namespace RetroSound.Core.Formats.TurboSound;

/// <summary>
/// Loads and parses TurboSound container data from a binary source.
/// </summary>
public interface ITurboSoundContainerLoader
{
    /// <summary>
    /// Loads a TurboSound container from the provided byte array.
    /// </summary>
    /// <param name="data">The in-memory TS container bytes.</param>
    /// <returns>The parsed container model.</returns>
    TurboSoundContainer Load(byte[] data);

    /// <summary>
    /// Loads a TurboSound container from the provided stream.
    /// </summary>
    /// <param name="source">The readable stream that contains TS container bytes.</param>
    /// <returns>The parsed container model.</returns>
    TurboSoundContainer Load(Stream source);

    /// <summary>
    /// Loads a TurboSound container from the provided file path.
    /// </summary>
    /// <param name="filePath">The file system path that points to the container file.</param>
    /// <returns>The parsed container model.</returns>
    TurboSoundContainer LoadFromFile(string filePath);

    /// <summary>
    /// Loads a TurboSound container from the provided stream.
    /// </summary>
    /// <param name="source">The readable stream that contains TS container bytes.</param>
    /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
    /// <returns>The parsed container model.</returns>
    ValueTask<TurboSoundContainer> LoadAsync(Stream source, CancellationToken cancellationToken = default);
}
