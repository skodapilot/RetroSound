// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using RetroSound.Core.Models;
namespace RetroSound.Core.Playback.Tracker.Internal;

internal static class AyRegisterFrameAssembler
{
    public static AyRegisterFrame Assemble(TrackerChipState chipState)
    {
        Span<byte> registers = stackalloc byte[AyRegisterFrame.RegisterCount];

        WriteTone(registers, 0, chipState.ChannelStates[0].TonePeriod);
        WriteTone(registers, 2, chipState.ChannelStates[1].TonePeriod);
        WriteTone(registers, 4, chipState.ChannelStates[2].TonePeriod);
        registers[6] = (byte)(chipState.NoisePeriod & 0x1F);
        registers[7] = BuildMixer(chipState.ChannelStates);
        registers[8] = BuildVolume(chipState.ChannelStates[0]);
        registers[9] = BuildVolume(chipState.ChannelStates[1]);
        registers[10] = BuildVolume(chipState.ChannelStates[2]);
        registers[11] = (byte)(chipState.EnvelopePeriod & 0xFF);
        registers[12] = (byte)((chipState.EnvelopePeriod >> 8) & 0xFF);
        registers[13] = (byte)(chipState.EnvelopeShape & 0xFF);

        var frame = new AyRegisterFrame(registers, chipState.EnvelopeShapeWritten);
        chipState.EnvelopeShapeWritten = false;
        return frame;
    }

    private static void WriteTone(Span<byte> registers, int startIndex, int tonePeriod)
    {
        registers[startIndex] = (byte)(tonePeriod & 0xFF);
        registers[startIndex + 1] = (byte)((tonePeriod >> 8) & 0x0F);
    }

    private static byte BuildMixer(IReadOnlyList<TrackerChannelState> channels)
    {
        byte mixer = 0;

        for (var channelIndex = 0; channelIndex < channels.Count; channelIndex++)
        {
            if (!channels[channelIndex].ToneEnabled)
            {
                mixer |= (byte)(1 << channelIndex);
            }

            if (!channels[channelIndex].NoiseEnabled)
            {
                mixer |= (byte)(1 << (channelIndex + 3));
            }
        }

        return mixer;
    }

    private static byte BuildVolume(TrackerChannelState channel)
    {
        var volume = (byte)(channel.Volume & 0x0F);
        return channel.UseEnvelope ? (byte)(volume | 0x10) : volume;
    }
}