// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using RetroSound.Ayumi;
using RetroSound.Core.Models;
using Xunit;
namespace RetroSound.Tests.Rendering;

public sealed class AyumiPcmRendererTests
{
    /// <summary>
    /// Verifies that initialization applies the configured output channel count.
    /// </summary>
    [Fact]
    public void AyumiPcmRenderer_InitializesConfiguredChannelCount()
    {
        using var backend = new AyumiPcmRenderer();

        backend.Initialize(new AyYmChipConfiguration(outputChannelCount: 2));

        Assert.Equal(2, backend.ChannelCount);
    }

    /// <summary>
    /// Verifies that rendering writes one full tick of audible PCM output.
    /// </summary>
    [Fact]
    public void AyumiPcmRenderer_RendersExpectedSampleCount()
    {
        using var backend = new AyumiPcmRenderer();
        backend.Initialize(new AyYmChipConfiguration(outputChannelCount: 2));

        var timing = new PlaybackTiming(50, 48_000);
        var buffer = new float[timing.GetSampleFramesPerTick() * 2];

        var written = backend.Render(CreateAudibleFrame(), timing, buffer);

        Assert.Equal(timing.GetSampleFramesPerTick(), written);
        Assert.Contains(buffer, sample => Math.Abs(sample) > 0.0001f);
    }

    /// <summary>
    /// Verifies that resetting the renderer restores deterministic output for the same frame.
    /// </summary>
    [Fact]
    public void AyumiPcmRenderer_ResetRestoresDeterministicOutput()
    {
        using var backend = new AyumiPcmRenderer();
        backend.Initialize(new AyYmChipConfiguration(outputChannelCount: 1));

        var timing = new PlaybackTiming(50, 48_000);
        var first = new float[timing.GetSampleFramesPerTick()];
        var second = new float[timing.GetSampleFramesPerTick()];

        backend.Render(CreateAudibleFrame(), timing, first);
        backend.Reset();
        backend.Render(CreateAudibleFrame(), timing, second);

        Assert.Equal(first, second);
    }

    /// <summary>
    /// Verifies that the renderer cannot be initialized more than once.
    /// </summary>
    [Fact]
    public void AyumiPcmRenderer_RejectsSecondInitialization()
    {
        using var backend = new AyumiPcmRenderer();
        backend.Initialize(new AyYmChipConfiguration(outputChannelCount: 1));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            backend.Initialize(new AyYmChipConfiguration(outputChannelCount: 1)));

        Assert.Equal("The Ayumi PCM renderer has already been initialized.", exception.Message);
    }

    /// <summary>
    /// Verifies that a zero noise period is treated the same as a period of one.
    /// </summary>
    [Fact]
    public void AyumiPcmRenderer_TreatsZeroNoisePeriodLikeAyumi()
    {
        using var zeroNoiseBackend = new AyumiPcmRenderer();
        using var oneNoiseBackend = new AyumiPcmRenderer();
        zeroNoiseBackend.Initialize(new AyYmChipConfiguration(outputChannelCount: 1));
        oneNoiseBackend.Initialize(new AyYmChipConfiguration(outputChannelCount: 1));

        var timing = new PlaybackTiming(50, 48_000);
        var zeroNoise = new float[timing.GetSampleFramesPerTick()];
        var oneNoise = new float[timing.GetSampleFramesPerTick()];

        zeroNoiseBackend.Render(CreateNoiseOnlyFrame(noisePeriod: 0), timing, zeroNoise);
        oneNoiseBackend.Render(CreateNoiseOnlyFrame(noisePeriod: 1), timing, oneNoise);

        Assert.Equal(oneNoise, zeroNoise);
    }

    /// <summary>
    /// Verifies that AY and YM DAC curves produce different PCM output.
    /// </summary>
    [Fact]
    public void AyumiPcmRenderer_UsesConfiguredChipDacCurve()
    {
        using var ayBackend = new AyumiPcmRenderer();
        using var ymBackend = new AyumiPcmRenderer();
        ayBackend.Initialize(new AyYmChipConfiguration(chipType: AyYmChipType.Ay38910, outputChannelCount: 1));
        ymBackend.Initialize(new AyYmChipConfiguration(chipType: AyYmChipType.Ym2149, outputChannelCount: 1));

        var timing = new PlaybackTiming(50, 48_000);
        var ay = new float[timing.GetSampleFramesPerTick()];
        var ym = new float[timing.GetSampleFramesPerTick()];
        var frame = CreateConstantToneFrame(volume: 0x08);

        ayBackend.Render(frame, timing, ay);
        ymBackend.Render(frame, timing, ym);

        Assert.NotEqual(ay, ym);
    }

    /// <summary>
    /// Verifies that stereo rendering applies non-identical channel panning.
    /// </summary>
    [Fact]
    public void AyumiPcmRenderer_UsesReferenceLinearStereoPanning()
    {
        using var backend = new AyumiPcmRenderer();
        backend.Initialize(new AyYmChipConfiguration(outputChannelCount: 2));

        var timing = new PlaybackTiming(50, 48_000);
        var buffer = new float[timing.GetSampleFramesPerTick() * 2];

        backend.Render(CreateConstantToneFrame(volume: 0x08), timing, buffer);

        var leftPeak = GetPeak(buffer, channelIndex: 0, channelCount: 2);
        var rightPeak = GetPeak(buffer, channelIndex: 1, channelCount: 2);

        Assert.True(leftPeak > 0f);
        Assert.True(rightPeak > 0f);
        Assert.True(leftPeak > rightPeak);
    }

    /// <summary>
    /// Verifies that rewriting the envelope shape restarts the envelope progression.
    /// </summary>
    [Fact]
    public void AyumiPcmRenderer_RewritingSameEnvelopeShapeRestartsEnvelope()
    {
        using var withoutRewrite = new AyumiPcmRenderer();
        using var withRewrite = new AyumiPcmRenderer();
        withoutRewrite.Initialize(new AyYmChipConfiguration(outputChannelCount: 1));
        withRewrite.Initialize(new AyYmChipConfiguration(outputChannelCount: 1));

        var timing = new PlaybackTiming(50, 48_000);
        var withoutRewriteSecond = new float[timing.GetSampleFramesPerTick()];
        var withRewriteSecond = new float[timing.GetSampleFramesPerTick()];

        withoutRewrite.Render(CreateEnvelopeFrame(envelopeShapeWritten: true), timing, new float[timing.GetSampleFramesPerTick()]);
        withRewrite.Render(CreateEnvelopeFrame(envelopeShapeWritten: true), timing, new float[timing.GetSampleFramesPerTick()]);

        withoutRewrite.Render(CreateEnvelopeFrame(envelopeShapeWritten: false), timing, withoutRewriteSecond);
        withRewrite.Render(CreateEnvelopeFrame(envelopeShapeWritten: true), timing, withRewriteSecond);

        Assert.NotEqual(withoutRewriteSecond, withRewriteSecond);
    }

    /// <summary>
    /// Verifies that rendering rejects sample rates that violate the Ayumi oversampling constraint.
    /// </summary>
    [Fact]
    public void AyumiPcmRenderer_RejectsSampleRateThatViolatesAyumiOversamplingConstraint()
    {
        using var backend = new AyumiPcmRenderer();
        backend.Initialize(new AyYmChipConfiguration(outputChannelCount: 1));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            backend.Render(CreateAudibleFrame(), new PlaybackTiming(50, 8_000), new float[160]));

        Assert.Equal(
            "The configured AY/YM sample rate is too low for ayumi oversampling. Increase the sample rate or lower the chip clock.",
            exception.Message);
    }

    private static AyRegisterFrame CreateAudibleFrame()
    {
        return new AyRegisterFrame(
        [
            0x40, 0x01,
            0x00, 0x02,
            0x80, 0x02,
            0x04,
            0b0011_1000,
            0x0f,
            0x08,
            0x06,
            0x20,
            0x00,
            0x0a,
        ]);
    }

    private static AyRegisterFrame CreateNoiseOnlyFrame(byte noisePeriod)
    {
        return new AyRegisterFrame(
        [
            0x01, 0x00,
            0x01, 0x00,
            0x01, 0x00,
            noisePeriod,
            0b0000_0111,
            0x0F,
            0x00,
            0x00,
            0x01,
            0x00,
            0x00,
        ]);
    }

    private static AyRegisterFrame CreateConstantToneFrame(byte volume)
    {
        return new AyRegisterFrame(
        [
            0x01, 0x00,
            0x01, 0x00,
            0x01, 0x00,
            0x01,
            0b0011_1110,
            volume,
            0x00,
            0x00,
            0x01,
            0x00,
            0x00,
        ]);
    }

    private static AyRegisterFrame CreateEnvelopeFrame(bool envelopeShapeWritten)
    {
        return new AyRegisterFrame(
        [
            0x20, 0x00,
            0x00, 0x00,
            0x00, 0x00,
            0x01,
            0b0011_1110,
            0x10,
            0x00,
            0x00,
            0x02,
            0x00,
            0x0A,
        ],
        envelopeShapeWritten);
    }

    private static float GetPeak(float[] samples, int channelIndex, int channelCount)
    {
        var peak = 0f;
        for (var i = channelIndex; i < samples.Length; i += channelCount)
        {
            peak = Math.Max(peak, Math.Abs(samples[i]));
        }

        return peak;
    }
}