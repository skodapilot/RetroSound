// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using RetroSound.Core.Formats.Pt3;
namespace RetroSound.Core.Playback.Pt3.Internal;

internal sealed class Pt3PatternPlaybackCursor(Pt3Module module, Pt3ChannelPlaybackState[] channels)
{
    private readonly Pt3Module _module = module ?? throw new ArgumentNullException(nameof(module));
    private readonly Pt3ChannelPlaybackState[] _channels = channels ?? throw new ArgumentNullException(nameof(channels));

    private int OrderIndex { get; set; }

    private Pt3Pattern CurrentPattern => _module.Patterns[_module.Order[OrderIndex]];

    public bool IsAtLastOrder => OrderIndex >= _module.Order.Count - 1;

    public void Reset()
    {
        OrderIndex = 0;
        AssignCurrentPattern();
    }

    public void AdvancePattern()
    {
        if (OrderIndex + 1 < _module.Order.Count)
        {
            OrderIndex++;
        }
        else
        {
            OrderIndex = _module.RestartPositionIndex;
        }

        AssignCurrentPattern();
    }

    private void AssignCurrentPattern()
    {
        var pattern = CurrentPattern;
        _channels[0].AssignPattern(pattern.ChannelA);
        _channels[1].AssignPattern(pattern.ChannelB);
        _channels[2].AssignPattern(pattern.ChannelC);
    }
}