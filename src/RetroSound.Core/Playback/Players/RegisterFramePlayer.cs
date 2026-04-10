// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using RetroSound.Core.Models;
namespace RetroSound.Core.Playback;

/// <summary>
/// Plays a pre-decoded sequence of AY/YM register frames at a fixed 50 Hz music tick rate.
/// </summary>
/// <remarks>
/// This adapter is intentionally simple: each payload is expected to contain complete 14-register frames in hardware
/// order. It is useful for integration tests and demo scenarios until production format decoders are added.
/// </remarks>
public sealed class RegisterFramePlayer : ITickPlayer
{
    private readonly ReadOnlyMemory<byte> _frameData;
    private readonly int _frameCount;
    private int _nextFrameIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterFramePlayer"/> class.
    /// </summary>
    /// <param name="frameData">The raw frame bytes. The payload must contain a whole number of 14-register frames.</param>
    /// <param name="sampleRate">The PCM output sample rate.</param>
    public RegisterFramePlayer(ReadOnlyMemory<byte> frameData, int sampleRate)
    {
        if (sampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be greater than zero.");
        }

        if (frameData.Length % AyRegisterFrame.RegisterCount != 0)
        {
            throw new ArgumentException(
                $"The frame payload length must be a multiple of {AyRegisterFrame.RegisterCount} bytes.",
                nameof(frameData));
        }

        _frameData = frameData;
        _frameCount = frameData.Length / AyRegisterFrame.RegisterCount;
        Timing = new PlaybackTiming(50, sampleRate);
    }

    /// <summary>
    /// Gets the playback timing used by the frame sequence player.
    /// </summary>
    public PlaybackTiming Timing { get; }

    /// <summary>
    /// Gets a value indicating whether the player has reached the end of its frame sequence.
    /// </summary>
    public bool IsEndOfStream => _nextFrameIndex >= _frameCount;

    /// <summary>
    /// Resets playback back to the first frame in the sequence.
    /// </summary>
    public void Reset()
    {
        _nextFrameIndex = 0;
    }

    /// <summary>
    /// Produces the next AY/YM register frame from the sequence.
    /// </summary>
    /// <param name="frame">When this method returns <see langword="true"/>, contains the next register frame.</param>
    /// <returns><see langword="true"/> when a frame was produced; otherwise, <see langword="false"/>.</returns>
    public bool TryAdvance(out AyRegisterFrame frame)
    {
        if (IsEndOfStream)
        {
            frame = null!;
            return false;
        }

        var offset = _nextFrameIndex * AyRegisterFrame.RegisterCount;
        frame = new AyRegisterFrame(_frameData.Slice(offset, AyRegisterFrame.RegisterCount).ToArray());
        _nextFrameIndex++;
        return true;
    }
}
