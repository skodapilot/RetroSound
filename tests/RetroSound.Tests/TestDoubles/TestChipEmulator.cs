// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using RetroSound.Core.Models;
using RetroSound.Core.Rendering;

namespace RetroSound.Tests.TestDoubles;

internal sealed class TestChipEmulator : IChipEmulator
{
    public int ChannelCount => 1;

    public void Reset()
    {
    }

    public int Render(AyRegisterFrame frame, PlaybackTiming timing, Span<float> destination)
    {
        return DeterministicChipSampleRenderer.Render(frame, timing, ChannelCount, destination);
    }
}
