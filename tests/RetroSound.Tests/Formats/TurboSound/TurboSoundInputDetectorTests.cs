// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using RetroSound.Core.Formats.TurboSound;
using Xunit;
namespace RetroSound.Tests.Formats.TurboSound;

public sealed class TurboSoundInputDetectorTests
{
    /// <summary>
    /// Verifies that the detector recognizes the short TS container signature.
    /// </summary>
    [Fact]
    public void Detect_ReturnsTsContainer_ForTsSignature()
    {
        var data = new byte[] { (byte)'T', (byte)'S', 0x01, 0x02 };

        var inputKind = TurboSoundInputDetector.Detect(data);

        Assert.Equal(TurboSoundInputKind.TsContainer, inputKind);
    }

    /// <summary>
    /// Verifies that the detector recognizes the short PT3 signature.
    /// </summary>
    [Fact]
    public void Detect_ReturnsPt3Module_ForPt3Signature()
    {
        var data = new byte[] { (byte)'P', (byte)'T', (byte)'3', 0x21 };

        var inputKind = TurboSoundInputDetector.Detect(data);

        Assert.Equal(TurboSoundInputKind.Pt3Module, inputKind);
    }

    /// <summary>
    /// Verifies that the detector recognizes the standard ProTracker PT3 header prefix.
    /// </summary>
    [Fact]
    public void Detect_ReturnsPt3Module_ForStandardPt3Header()
    {
        var data = "ProTracker 3.6 compilation of "u8.ToArray();

        var inputKind = TurboSoundInputDetector.Detect(data);

        Assert.Equal(TurboSoundInputKind.Pt3Module, inputKind);
    }

    /// <summary>
    /// Verifies that the detector recognizes the Vortex Tracker PT3 header prefix.
    /// </summary>
    [Fact]
    public void Detect_ReturnsPt3Module_ForVortexTrackerHeader()
    {
        var data = "Vortex Tracker II 1.0 module: Example module data"u8.ToArray();

        var inputKind = TurboSoundInputDetector.Detect(data);

        Assert.Equal(TurboSoundInputKind.Pt3Module, inputKind);
    }

    /// <summary>
    /// Verifies that unrecognized bytes are classified as unknown input.
    /// </summary>
    [Fact]
    public void Detect_ReturnsUnknown_ForUnrecognizedSignature()
    {
        var data = new byte[] { (byte)'A', (byte)'B', (byte)'C' };

        var inputKind = TurboSoundInputDetector.Detect(data);

        Assert.Equal(TurboSoundInputKind.Unknown, inputKind);
    }

    /// <summary>
    /// Verifies that empty input is classified as unknown.
    /// </summary>
    [Fact]
    public void Detect_ReturnsUnknown_ForEmptyInput()
    {
        var inputKind = TurboSoundInputDetector.Detect(Array.Empty<byte>());

        Assert.Equal(TurboSoundInputKind.Unknown, inputKind);
    }

    /// <summary>
    /// Verifies that stream detection recognizes the ProTracker header and preserves the stream position.
    /// </summary>
    [Fact]
    public void Detect_Stream_ReturnsPt3Module_ForStandardPt3HeaderPrefix()
    {
        var data = "ProTracker 3.7 compilation of "u8.ToArray();
        using var stream = new MemoryStream(data, writable: false);

        var inputKind = TurboSoundInputDetector.Detect(stream);

        Assert.Equal(TurboSoundInputKind.Pt3Module, inputKind);
        Assert.Equal(0, stream.Position);
    }

    /// <summary>
    /// Verifies that stream detection recognizes the Vortex Tracker header and preserves the stream position.
    /// </summary>
    [Fact]
    public void Detect_Stream_ReturnsPt3Module_ForVortexTrackerHeaderPrefix()
    {
        var data = "Vortex Tracker II 1.0 module: Example module data"u8.ToArray();
        using var stream = new MemoryStream(data, writable: false);

        var inputKind = TurboSoundInputDetector.Detect(stream);

        Assert.Equal(TurboSoundInputKind.Pt3Module, inputKind);
        Assert.Equal(0, stream.Position);
    }

    /// <summary>
    /// Verifies that stream detection recognizes the TS signature and rewinds the stream.
    /// </summary>
    [Fact]
    public void Detect_Stream_ReturnsTsContainer_ForShortSignature()
    {
        var data = new byte[] { (byte)'T', (byte)'S', 0x01, 0x02 };
        using var stream = new MemoryStream(data, writable: false);

        var inputKind = TurboSoundInputDetector.Detect(stream);

        Assert.Equal(TurboSoundInputKind.TsContainer, inputKind);
        Assert.Equal(0, stream.Position);
    }
}