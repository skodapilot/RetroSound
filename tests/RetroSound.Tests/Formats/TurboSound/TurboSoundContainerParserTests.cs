// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using RetroSound.Core.Formats.TurboSound;
using RetroSound.Core.Models;
using Xunit;
namespace RetroSound.Tests.Formats.TurboSound;

public sealed class TurboSoundContainerParserTests
{
    /// <summary>
    /// Verifies that byte-array loading parses both container entries and preserves their payloads.
    /// </summary>
    [Fact]
    public void Load_ParsesBothLogicalModulesFromByteArray()
    {
        var parser = new TsContainerParser();
        var firstModule = new byte[] { (byte)'P', (byte)'T', (byte)'3', 0x21 };
        var secondModule = new byte[] { 0x10, 0x20, 0x30 };
        var containerBytes = CreateContainer(firstModule, secondModule);

        var container = parser.Load(containerBytes);

        Assert.Null(container.Title);
        Assert.Equal(TurboSoundContainer.ModuleCount, container.Entries.Count);
        Assert.Equal(0, container.FirstModule.SlotIndex);
        Assert.Equal(1, container.SecondModule.SlotIndex);
        Assert.Equal("PT3", container.FirstModule.FormatHint);
        Assert.Null(container.SecondModule.FormatHint);
        Assert.Equal(firstModule, container.FirstModule.Payload.ToArray());
        Assert.Equal(secondModule, container.SecondModule.Payload.ToArray());
    }

    /// <summary>
    /// Verifies that file-based loading parses the same container payloads as in-memory input.
    /// </summary>
    [Fact]
    public void LoadFromFile_ParsesContainer()
    {
        var parser = new TsContainerParser();
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ts");

        try
        {
            File.WriteAllBytes(filePath, CreateContainer(new byte[] { 7, 8 }, new byte[] { 9, 10, 11 }));

            var container = parser.LoadFromFile(filePath);

            Assert.Equal(new byte[] { 7, 8 }, container.FirstModule.Payload.ToArray());
            Assert.Equal(new byte[] { 9, 10, 11 }, container.SecondModule.Payload.ToArray());
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    /// <summary>
    /// Verifies that stream-based loading parses both container payloads correctly.
    /// </summary>
    [Fact]
    public async Task LoadAsync_ParsesContainerFromStream()
    {
        var parser = new TsContainerParser();
        var containerBytes = CreateContainer(new byte[] { 1, 2, 3 }, new byte[] { 4, 5, 6, 7 });
        await using var stream = new MemoryStream(containerBytes, writable: false);

        var container = await parser.LoadAsync(stream);

        Assert.Equal(new byte[] { 1, 2, 3 }, container.FirstModule.Payload.ToArray());
        Assert.Equal(new byte[] { 4, 5, 6, 7 }, container.SecondModule.Payload.ToArray());
    }

    /// <summary>
    /// Verifies that the parser rejects container data with an invalid TS signature.
    /// </summary>
    [Fact]
    public void Load_RejectsInvalidSignature()
    {
        var parser = new TsContainerParser();
        var data = CreateContainer(new byte[] { 1 }, new byte[] { 2 });
        data[0] = (byte)'B';

        var exception = Assert.Throws<TurboSoundContainerFormatException>(() => parser.Load(data));

        Assert.Equal("The TS container signature is invalid. Expected ASCII 'TS'.", exception.Message);
    }

    /// <summary>
    /// Verifies that the parser rejects truncated container payloads.
    /// </summary>
    [Fact]
    public void Load_RejectsTruncatedInput()
    {
        var parser = new TsContainerParser();
        var fullData = CreateContainer(new byte[] { 1, 2, 3 }, new byte[] { 4, 5, 6 });
        var truncatedData = fullData[..^2];

        var exception = Assert.Throws<TurboSoundContainerFormatException>(() => parser.Load(truncatedData));

        Assert.Equal("The TS container is truncated. Expected 16 bytes but found 14.", exception.Message);
    }

    /// <summary>
    /// Verifies that each container entry must contain a non-empty module payload.
    /// </summary>
    [Theory]
    [InlineData(0, 1, "The first TS module payload is empty.")]
    [InlineData(1, 0, "The second TS module payload is empty.")]
    public void Load_RejectsEmptyModulePayloads(
        int firstModuleLength,
        int secondModuleLength,
        string expectedMessage)
    {
        var parser = new TsContainerParser();
        var data = CreateContainer(new byte[firstModuleLength], new byte[secondModuleLength]);

        var exception = Assert.Throws<TurboSoundContainerFormatException>(() => parser.Load(data));

        Assert.Equal(expectedMessage, exception.Message);
    }

    /// <summary>
    /// Verifies that the parser rejects trailing bytes after the declared payload lengths.
    /// </summary>
    [Fact]
    public void Load_RejectsTrailingBytes()
    {
        var parser = new TsContainerParser();
        var data = CreateContainer(new byte[] { 0x01 }, new byte[] { 0x02, 0x03, 0x04, 0x05, 0x06 });
        Array.Resize(ref data, data.Length + 1);
        data[^1] = 0x99;

        var exception = Assert.Throws<TurboSoundContainerFormatException>(() => parser.Load(data));

        Assert.Equal("The TS container has 1 unexpected trailing bytes.", exception.Message);
    }

    /// <summary>
    /// Verifies that container entries reject negative slot indexes.
    /// </summary>
    [Fact]
    public void ContainerEntry_RejectsNegativeSlotIndex()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new TurboSoundContainerEntry(-1, formatHint: null, payload: new byte[] { 0x01 }));

        Assert.Equal("slotIndex", exception.ParamName);
    }

    /// <summary>
    /// Verifies that parsed container entries are exposed through a read-only collection.
    /// </summary>
    [Fact]
    public void Load_ReturnsReadOnlyEntryView()
    {
        var parser = new TsContainerParser();
        var container = parser.Load(CreateContainer(new byte[] { 1 }, new byte[] { 2 }));

        Assert.IsNotType<TurboSoundContainerEntry[]>(container.Entries);
    }

    private static byte[] CreateContainer(byte[] firstModule, byte[] secondModule)
    {
        var data = new byte[10 + firstModule.Length + secondModule.Length];
        data[0] = (byte)'T';
        data[1] = (byte)'S';
        BitConverter.GetBytes(firstModule.Length).CopyTo(data, 2);
        BitConverter.GetBytes(secondModule.Length).CopyTo(data, 6);
        firstModule.CopyTo(data, 10);
        secondModule.CopyTo(data, 10 + firstModule.Length);
        return data;
    }
}