// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using RetroSound.Core.Formats.Pt3;
using Xunit;
namespace RetroSound.Tests.Formats.Pt3;

public sealed class Pt3TurboSoundModuleLoaderTests
{
    /// <summary>
    /// Verifies that a standalone PT3 module is rejected when no second embedded module is present.
    /// </summary>
    [Fact]
    public void Load_RejectsStandalonePt3WithoutSecondEmbeddedModule()
    {
        var loader = new Pt3TurboSoundModuleLoader();
        var data = File.ReadAllBytes(GetTestDataPath("minimal-valid.pt3"));

        var exception = Assert.Throws<Pt3FormatException>(() => loader.Load(data));

        Assert.Equal("The PT3 TurboSound module does not contain a valid second embedded PT3 module.", exception.Message);
    }

    /// <summary>
    /// Verifies that diagnostics still describe the discovered standalone PT3 candidate.
    /// </summary>
    [Fact]
    public void Analyze_ReturnsDiagnosticsForStandalonePt3()
    {
        var loader = new Pt3TurboSoundModuleLoader();
        var data = File.ReadAllBytes(GetTestDataPath("minimal-valid.pt3"));

        var diagnostics = loader.Analyze(data);

        Assert.Equal(data.Length, diagnostics.FileLength);
        Assert.Equal(1, diagnostics.DiscoveredModuleCount);
        Assert.Equal(1, diagnostics.ParsedModuleCount);
        Assert.Equal(1, diagnostics.UsedModuleCount);
        Assert.Equal(0, diagnostics.SkippedModuleCount);

        var candidate = Assert.Single(diagnostics.Candidates);
        Assert.Equal(0, candidate.Offset);
        Assert.Equal("Used as chip A", candidate.Usage);
        Assert.True(candidate.ParsedSuccessfully);
    }
    private static string GetTestDataPath(string fileName)
    {
        return Path.Combine(AppContext.BaseDirectory, "TestData", fileName);
    }
}