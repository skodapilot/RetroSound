// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using RetroSound.Core.Models;
using RetroSound.Core.Playback;

namespace RetroSound.Tests.TestDoubles;

internal sealed class TestTickPlayer : ITickPlayer
{
    private readonly int _tickCount;
    private int _nextTickIndex;

    public TestTickPlayer(int sampleRate, int tickCount)
    {
        if (tickCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tickCount), "Tick count cannot be negative.");
        }

        Timing = new PlaybackTiming(50, sampleRate);
        _tickCount = tickCount;
    }

    public PlaybackTiming Timing { get; }

    public int ProducedTickCount => _nextTickIndex;

    public bool IsEndOfStream => _nextTickIndex >= _tickCount;

    public void Reset()
    {
        _nextTickIndex = 0;
    }

    public bool TryAdvance(out AyRegisterFrame frame)
    {
        if (_nextTickIndex >= _tickCount)
        {
            frame = null!;
            return false;
        }

        Span<byte> registers = stackalloc byte[AyRegisterFrame.RegisterCount];
        var baseValue = _nextTickIndex * 16;

        for (var registerIndex = 0; registerIndex < registers.Length; registerIndex++)
        {
            registers[registerIndex] = unchecked((byte)(baseValue + registerIndex));
        }

        frame = new AyRegisterFrame(registers);
        _nextTickIndex++;
        return true;
    }
}
