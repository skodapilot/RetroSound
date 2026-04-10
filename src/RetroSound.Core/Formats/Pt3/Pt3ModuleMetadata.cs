// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

namespace RetroSound.Core.Formats.Pt3;

/// <summary>
/// Represents human-readable metadata stored in a PT3 module header.
/// </summary>
/// <param name="Version">The PT3 sub-version text extracted from the header.</param>
/// <param name="Title">The module title.</param>
/// <param name="Author">The author name.</param>
public sealed record Pt3ModuleMetadata(
    string Version,
    string Title,
    string Author);