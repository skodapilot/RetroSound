// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using RetroSound.Core.Models;
namespace RetroSound.Core.Playback;

/// <summary>
/// Coordinates two tick players so both AY/YM chips advance on the same logical tick.
/// </summary>
public class DualChipPlayer : ILoopingPlaybackController
{
    private const string DivergedPlayersMessage = "The two chip players diverged while advancing dual-chip playback.";

    /// <summary>
    /// Initializes a new instance of the <see cref="DualChipPlayer"/> class.
    /// </summary>
    /// <param name="chipAPlayer">The player responsible for the first AY/YM chip.</param>
    /// <param name="chipBPlayer">The player responsible for the second AY/YM chip.</param>
    public DualChipPlayer(ITickPlayer chipAPlayer, ITickPlayer chipBPlayer)
    {
        ChipAPlayer = chipAPlayer ?? throw new ArgumentNullException(nameof(chipAPlayer));
        ChipBPlayer = chipBPlayer ?? throw new ArgumentNullException(nameof(chipBPlayer));

        if (ChipAPlayer.Timing != ChipBPlayer.Timing)
        {
            throw new ArgumentException("Both chip players must use the same playback timing.", nameof(chipBPlayer));
        }

        Timing = ChipAPlayer.Timing;
    }

    /// <summary>
    /// Gets the player responsible for the first AY/YM chip.
    /// </summary>
    public ITickPlayer ChipAPlayer { get; }

    /// <summary>
    /// Gets the player responsible for the second AY/YM chip.
    /// </summary>
    public ITickPlayer ChipBPlayer { get; }

    /// <summary>
    /// Gets the playback timing shared by both chip players.
    /// </summary>
    public PlaybackTiming Timing { get; }

    /// <summary>
    /// Gets a value indicating whether both child players support loop playback control.
    /// </summary>
    public bool SupportsLooping =>
        ChipAPlayer is ILoopingPlaybackController &&
        ChipBPlayer is ILoopingPlaybackController;

    /// <summary>
    /// Gets or sets a value indicating whether both child players should loop when they reach the end of the order list.
    /// </summary>
    public bool IsLoopingEnabled
    {
        get
        {
            if (ChipAPlayer is not ILoopingPlaybackController chipALoopingController ||
                ChipBPlayer is not ILoopingPlaybackController chipBLoopingController)
            {
                return false;
            }

            return chipALoopingController.IsLoopingEnabled && chipBLoopingController.IsLoopingEnabled;
        }

        set
        {
            if (ChipAPlayer is not ILoopingPlaybackController chipALoopingController ||
                ChipBPlayer is not ILoopingPlaybackController chipBLoopingController)
            {
                throw new NotSupportedException("Loop playback is only available when both chip players support it.");
            }

            chipALoopingController.IsLoopingEnabled = value;
            chipBLoopingController.IsLoopingEnabled = value;
        }
    }

    /// <summary>
    /// Gets a value indicating whether either chip player has reached the end of its stream.
    /// Dual-chip playback can only continue while both players remain aligned.
    /// </summary>
    public bool IsEndOfStream => ChipAPlayer.IsEndOfStream || ChipBPlayer.IsEndOfStream;

    /// <summary>
    /// Resets both chip players back to their initial playback state.
    /// </summary>
    public void Reset()
    {
        ChipAPlayer.Reset();
        ChipBPlayer.Reset();
    }

    /// <summary>
    /// Advances both chip players by one logical tick and returns the pair of AY/YM register frames.
    /// </summary>
    /// <param name="frame">When this method returns <see langword="true"/>, contains the next dual-chip playback frame.</param>
    /// <returns><see langword="true"/> when both chip players produced a frame; otherwise, <see langword="false"/>.</returns>
    public bool TryAdvance(out DualChipPlaybackFrame frame)
    {
        if (ChipAPlayer.IsEndOfStream || ChipBPlayer.IsEndOfStream)
        {
            if (ChipAPlayer.IsEndOfStream != ChipBPlayer.IsEndOfStream)
            {
                throw new InvalidOperationException(DivergedPlayersMessage);
            }

            frame = default;
            return false;
        }

        var chipAAdvanced = ChipAPlayer.TryAdvance(out var chipAFrame);
        var chipBAdvanced = ChipBPlayer.TryAdvance(out var chipBFrame);

        if (chipAAdvanced != chipBAdvanced)
        {
            throw new InvalidOperationException(DivergedPlayersMessage);
        }

        if (!chipAAdvanced)
        {
            frame = default;
            return false;
        }

        frame = new DualChipPlaybackFrame(chipAFrame, chipBFrame);
        return true;
    }
}
