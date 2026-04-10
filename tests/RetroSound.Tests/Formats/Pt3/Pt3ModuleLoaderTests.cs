// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using RetroSound.Core.Formats.Pt3;
using Xunit;
namespace RetroSound.Tests.Formats.Pt3;

public sealed class Pt3ModuleLoaderTests
{
    /// <summary>
    /// Verifies that the loader parses the bundled minimal PT3 fixture into the expected domain model.
    /// </summary>
    [Fact]
    public void LoadFromFile_ParsesMinimalSampleModule()
    {
        var loader = new Pt3ModuleLoader();

        var module = loader.LoadFromFile(GetTestDataPath("minimal-valid.pt3"));

        Assert.Equal("6", module.Metadata.Version);
        Assert.Equal("Minimal PT3 Sample", module.Metadata.Title);
        Assert.Equal("RetroSound Tests", module.Metadata.Author);
        Assert.Equal(Pt3FrequencyTableKind.ProTracker, module.FrequencyTable);
        Assert.Equal(6, module.Tempo);
        Assert.Equal(0, module.RestartPositionIndex);
        Assert.Equal(new[] { 0 }, module.Order);

        var pattern = Assert.Single(module.Patterns);
        Assert.Equal(0, pattern.Index);
        Assert.Equal(new byte[] { 0x00 }, pattern.ChannelA.CommandStream.ToArray());
        Assert.Equal(new byte[] { 0x00 }, pattern.ChannelB.CommandStream.ToArray());
        Assert.Equal(new byte[] { 0x00 }, pattern.ChannelC.CommandStream.ToArray());
        Assert.Equal(209, pattern.ChannelA.DataOffset);
        Assert.Equal(210, pattern.ChannelB.DataOffset);
        Assert.Equal(211, pattern.ChannelC.DataOffset);

        var sample = Assert.Single(module.Samples);
        Assert.Equal(1, sample.Index);
        Assert.Equal(212, sample.DataOffset);
        Assert.Equal(0, sample.LoopPosition);
        var step = Assert.Single(sample.Steps);
        Assert.Equal(15, step.Volume);
        Assert.Equal(0, step.ToneOffset);
        Assert.True(step.UseEnvelope);

        var ornament = Assert.Single(module.Ornaments);
        Assert.Equal(0, ornament.Index);
        Assert.Equal(218, ornament.DataOffset);
        Assert.Equal(0, ornament.LoopPosition);
        Assert.Equal((sbyte)0, Assert.Single(ornament.ToneOffsets));
    }

    /// <summary>
    /// Verifies that stream-based loading produces the same parsed PT3 module data.
    /// </summary>
    [Fact]
    public void LoadFromStream_ParsesModule()
    {
        var loader = new Pt3ModuleLoader();
        var data = File.ReadAllBytes(GetTestDataPath("minimal-valid.pt3"));
        using var stream = new MemoryStream(data, writable: false);

        var module = loader.LoadFromStream(stream);

        Assert.Equal("Minimal PT3 Sample", module.Metadata.Title);
        Assert.Single(module.Patterns);
    }

    /// <summary>
    /// Verifies that stream-based loading produces the same parsed PT3 module data.
    /// </summary>
    [Fact]
    public async Task LoadAsync_FromStream_ParsesModule()
    {
        var loader = new Pt3ModuleLoader();
        var data = File.ReadAllBytes(GetTestDataPath("minimal-valid.pt3"));
        await using var stream = new MemoryStream(data, writable: false);

        var module = await loader.LoadAsync(stream);

        Assert.Equal("Minimal PT3 Sample", module.Metadata.Title);
        Assert.Single(module.Patterns);
    }

    /// <summary>
    /// Verifies that the loader rejects data with an invalid PT3 signature.
    /// </summary>
    [Fact]
    public void Load_RejectsInvalidSignature()
    {
        var loader = new Pt3ModuleLoader();
        var data = File.ReadAllBytes(GetTestDataPath("minimal-valid.pt3"));
        data[0] = (byte)'B';

        var exception = Assert.Throws<Pt3FormatException>(() => loader.Load(data));

        Assert.Equal(
            "The PT3 module signature is invalid. Expected ASCII 'ProTracker 3.' or 'Vortex Tracker II 1.0 module:'.",
            exception.Message);
    }

    /// <summary>
    /// Verifies that the loader rejects order entries that do not point to a pattern boundary.
    /// </summary>
    [Fact]
    public void Load_RejectsOrderEntryThatIsNotPatternAligned()
    {
        var loader = new Pt3ModuleLoader();
        var data = File.ReadAllBytes(GetTestDataPath("minimal-valid.pt3"));
        data[201] = 1;

        var exception = Assert.Throws<Pt3FormatException>(() => loader.Load(data));

        Assert.Equal("The PT3 order entry at offset 201 has value 1, which is not divisible by 3.", exception.Message);
    }

    /// <summary>
    /// Verifies that the loader reports a truncated sample header with a clear format error.
    /// </summary>
    [Fact]
    public void Load_RejectsTruncatedSampleHeader()
    {
        var loader = new Pt3ModuleLoader();
        var data = File.ReadAllBytes(GetTestDataPath("minimal-valid.pt3"));
        data[107] = 220;
        data[108] = 0;

        var exception = Assert.Throws<Pt3FormatException>(() => loader.Load(data));

        Assert.Equal("The PT3 sample 1 header at offset 220 is truncated.", exception.Message);
    }

    private static string GetTestDataPath(string fileName)
    {
        return Path.Combine(AppContext.BaseDirectory, "TestData", fileName);
    }
}
