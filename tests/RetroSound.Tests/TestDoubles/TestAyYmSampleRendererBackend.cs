// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using RetroSound.Core.Models;
using RetroSound.Core.Rendering;

namespace RetroSound.Tests.TestDoubles;

internal sealed class TestAyYmSampleRendererBackend : IAyYmSampleRendererBackend
{
    private AyYmChipConfiguration? _configuration;

    public int ChannelCount => _configuration?.OutputChannelCount ?? 0;

    public void Initialize(AyYmChipConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public void Reset()
    {
    }

    public int Render(AyRegisterFrame frame, PlaybackTiming timing, Span<float> destination)
    {
        EnsureInitialized();
        return DeterministicChipSampleRenderer.Render(frame, timing, ChannelCount, destination);
    }

    public void Dispose()
    {
    }

    private void EnsureInitialized()
    {
        if (_configuration is null)
        {
            throw new InvalidOperationException("The test AY/YM renderer backend must be initialized before use.");
        }
    }
}
