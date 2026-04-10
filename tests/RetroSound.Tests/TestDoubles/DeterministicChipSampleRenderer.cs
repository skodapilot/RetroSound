// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using RetroSound.Core.Models;

namespace RetroSound.Tests.TestDoubles;

internal static class DeterministicChipSampleRenderer
{
    public static int Render(AyRegisterFrame frame, PlaybackTiming timing, int channelCount, Span<float> destination)
    {
        ArgumentNullException.ThrowIfNull(frame);

        var sampleFramesToWrite = (int)Math.Round(timing.SamplesPerTick, MidpointRounding.AwayFromZero);
        if (sampleFramesToWrite <= 0)
        {
            throw new InvalidOperationException("Playback timing must produce at least one sample frame per tick.");
        }

        if (destination.Length < sampleFramesToWrite * channelCount)
        {
            throw new ArgumentException("Destination buffer is too small for one rendered tick.", nameof(destination));
        }

        var checksum = ComputeRegisterChecksum(frame);
        for (var sampleIndex = 0; sampleIndex < sampleFramesToWrite; sampleIndex++)
        {
            var baseSample = ((checksum + sampleIndex) % 2048) / 2048f;
            var destinationOffset = sampleIndex * channelCount;

            for (var channelIndex = 0; channelIndex < channelCount; channelIndex++)
            {
                destination[destinationOffset + channelIndex] = baseSample + (channelIndex / 4096f);
            }
        }

        return sampleFramesToWrite;
    }

    private static int ComputeRegisterChecksum(AyRegisterFrame frame)
    {
        var checksum = 0;
        for (var registerIndex = 0; registerIndex < AyRegisterFrame.RegisterCount; registerIndex++)
        {
            checksum += frame[registerIndex] * (registerIndex + 1);
        }

        return checksum;
    }
}