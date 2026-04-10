// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using System.Runtime.CompilerServices;
using RetroSound.Core.Models;
namespace RetroSound.Ayumi.Synthesis;

internal sealed class AyumiEngine
{
    private const int ChannelCount = 3;
    private const int DecimateFactor = 8;
    private const int FirSize = 192;
    private const int DcFilterSize = 1024;

    private static readonly double[] AyDacTable =
    [
        0.0, 0.0,
        0.00999465934234, 0.00999465934234,
        0.0144502937362, 0.0144502937362,
        0.0210574502174, 0.0210574502174,
        0.0307011520562, 0.0307011520562,
        0.0455481803616, 0.0455481803616,
        0.0644998855573, 0.0644998855573,
        0.107362478065, 0.107362478065,
        0.126588845655, 0.126588845655,
        0.20498970016, 0.20498970016,
        0.292210269322, 0.292210269322,
        0.372838941024, 0.372838941024,
        0.492530708782, 0.492530708782,
        0.635324635691, 0.635324635691,
        0.805584802014, 0.805584802014,
        1.0, 1.0,
    ];

    private static readonly double[] YmDacTable =
    [
        0.0, 0.0,
        0.00465400167849, 0.00772106507973,
        0.0109559777218, 0.0139620050355,
        0.0169985503929, 0.0200198367285,
        0.024368657969, 0.029694056611,
        0.0350652323186, 0.0403906309606,
        0.0485389486534, 0.0583352407111,
        0.0680552376593, 0.0777752346075,
        0.0925154497597, 0.111085679408,
        0.129747463188, 0.148485542077,
        0.17666895552, 0.211551079576,
        0.246387426566, 0.281101701381,
        0.333730067903, 0.400427252613,
        0.467383840696, 0.53443198291,
        0.635172045472, 0.75800717174,
        0.879926756695, 1.0,
    ];

    private static readonly EnvelopeAction[] EnvelopeShapes =
    [
        EnvelopeAction.SlideDown, EnvelopeAction.HoldBottom,  // shape 0
        EnvelopeAction.SlideDown, EnvelopeAction.HoldBottom,  // shape 1
        EnvelopeAction.SlideDown, EnvelopeAction.HoldBottom,  // shape 2
        EnvelopeAction.SlideDown, EnvelopeAction.HoldBottom,  // shape 3
        EnvelopeAction.SlideUp,   EnvelopeAction.HoldBottom,  // shape 4
        EnvelopeAction.SlideUp,   EnvelopeAction.HoldBottom,  // shape 5
        EnvelopeAction.SlideUp,   EnvelopeAction.HoldBottom,  // shape 6
        EnvelopeAction.SlideUp,   EnvelopeAction.HoldBottom,  // shape 7
        EnvelopeAction.SlideDown, EnvelopeAction.SlideDown,   // shape 8
        EnvelopeAction.SlideDown, EnvelopeAction.HoldBottom,  // shape 9
        EnvelopeAction.SlideDown, EnvelopeAction.SlideUp,     // shape 10
        EnvelopeAction.SlideDown, EnvelopeAction.HoldTop,     // shape 11
        EnvelopeAction.SlideUp,   EnvelopeAction.SlideUp,     // shape 12
        EnvelopeAction.SlideUp,   EnvelopeAction.HoldTop,     // shape 13
        EnvelopeAction.SlideUp,   EnvelopeAction.SlideDown,   // shape 14
        EnvelopeAction.SlideUp,   EnvelopeAction.HoldBottom,  // shape 15
    ];

    private static readonly double[] DecimateKernel =
    [
        -0.0000046183113992051936,
        -0.00001117761640887225,
        -0.000018610264502005432,
        -0.000025134586135631012,
        -0.000028494281690666197,
        -0.000026396828793275159,
        -0.000017094212558802156,
        0.0,
        0.000023798193576966866,
        0.000051281160242202183,
        0.00007762197826243427,
        0.000096759426664120416,
        0.00010240229300393402,
        0.000089344614218077106,
        0.000054875700118949183,
        0.0,
        -0.000069839082210680165,
        -0.0001447966132360757,
        -0.00021158452917708308,
        -0.00025535069106550544,
        -0.00026228714374322104,
        -0.00022258805927027799,
        -0.00013323230495695704,
        0.0,
        0.00016182578767055206,
        0.00032846175385096581,
        0.00047045611576184863,
        0.00055713851457530944,
        0.00056212565121518726,
        0.00046901918553962478,
        0.00027624866838952986,
        0.0,
        -0.00032564179486838622,
        -0.00065182310286710388,
        -0.00092127787309319298,
        -0.0010772534348943575,
        -0.0010737727700273478,
        -0.00088556645390392634,
        -0.00051581896090765534,
        0.0,
        0.00059548767193795277,
        0.0011803558710661009,
        0.0016527320270369871,
        0.0019152679330965555,
        0.0018927324805381538,
        0.0015481870327877937,
        0.00089470695834941306,
        0.0,
        -0.0010178225878206125,
        -0.0020037400552054292,
        -0.0027874356824117317,
        -0.003210329988021943,
        -0.0031540624117984395,
        -0.0025657163651900345,
        -0.0014750752642111449,
        0.0,
        0.0016624165446378462,
        0.0032591192839069179,
        0.0045165685815867747,
        0.0051838984346123896,
        0.0050774264697459933,
        0.0041192521414141585,
        0.0023628575417966491,
        0.0,
        -0.0026543507866759182,
        -0.0051990251084333425,
        -0.0072020238234656924,
        -0.0082672928192007358,
        -0.0081033739572956287,
        -0.006583111539570221,
        -0.0037839040415292386,
        0.0,
        0.0042781252851152507,
        0.0084176358598320178,
        0.01172566057463055,
        0.013550476647788672,
        0.013388189369997496,
        0.010979501242341259,
        0.006381274941685413,
        0.0,
        -0.007421229604153888,
        -0.01486456304340213,
        -0.021143584622178104,
        -0.02504275058758609,
        -0.025473530942547201,
        -0.021627310017882196,
        -0.013104323383225543,
        0.0,
        0.017065133989980476,
        0.036978919264451952,
        0.05823318062093958,
        0.079072012081405949,
        0.097675998716952317,
        0.11236045936950932,
        0.12176343577287731,
        0.125,
    ];

    private readonly ChannelState[] _channels = [new(), new(), new()];
    private readonly double[] _firLeft = new double[FirSize * 2];
    private readonly double[] _firRight = new double[FirSize * 2];
    private readonly DcFilterState _dcFilterLeft = new(DcFilterSize);
    private readonly DcFilterState _dcFilterRight = new(DcFilterSize);
    private readonly InterpolatorState _interpolatorLeft = new();
    private readonly InterpolatorState _interpolatorRight = new();

    private double[] _dacTable = YmDacTable;
    private double _step;
    private double _x;
    private double _left;
    private double _right;
    private int _noisePeriod;
    private int _noiseCounter;
    private int _noise;
    private int _envelopeCounter;
    private int _envelopePeriod;
    private int _envelopeShape;
    private int _envelopeSegment;
    private int _envelope;
    private int _firIndex;
    private int _dcIndex;

    public void Configure(AyYmChipType chipType, int chipClockHz, int sampleRate)
    {
        if (sampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be greater than zero.");
        }

        _dacTable = chipType == AyYmChipType.Ay38910 ? AyDacTable : YmDacTable;
        _step = chipClockHz / (sampleRate * 8d * DecimateFactor);
        if (_step >= 1d)
        {
            throw new InvalidOperationException(
                "The configured AY/YM sample rate is too low for ayumi oversampling. Increase the sample rate or lower the chip clock.");
        }
    }

    public void Reset()
    {
        _noisePeriod = 0;
        _noiseCounter = 0;
        _noise = 1;
        _envelopeCounter = 0;
        _envelopePeriod = 1;
        _envelopeShape = 0;
        _envelopeSegment = 0;
        _envelope = 31;
        _step = 0;
        _x = 0;
        _left = 0;
        _right = 0;
        _firIndex = 0;
        _dcIndex = 0;

        Array.Clear(_firLeft);
        Array.Clear(_firRight);
        _dcFilterLeft.Reset();
        _dcFilterRight.Reset();
        _interpolatorLeft.Reset();
        _interpolatorRight.Reset();

        for (var i = 0; i < _channels.Length; i++)
        {
            _channels[i].Reset();
        }

        SetTone(0, 1);
        SetTone(1, 1);
        SetTone(2, 1);
        SetPan(0, 0.1, equalPower: true);
        SetPan(1, 0.5, equalPower: true);
        SetPan(2, 0.9, equalPower: true);
    }

    public void SetTone(int index, int period)
    {
        period &= 0x0fff;
        _channels[index].TonePeriod = period == 0 ? 1 : period;
    }

    public void SetNoise(int period)
    {
        period &= 0x1f;
        _noisePeriod = period == 0 ? 1 : period;
    }

    public void SetMixer(int index, int toneDisabled, int noiseDisabled, int envelopeEnabled)
    {
        var channel = _channels[index];
        channel.ToneDisabled = toneDisabled & 1;
        channel.NoiseDisabled = noiseDisabled & 1;
        channel.EnvelopeEnabled = envelopeEnabled & 1;
    }

    public void SetVolume(int index, int volume)
    {
        _channels[index].Volume = volume & 0x0f;
    }

    public void SetEnvelope(int period)
    {
        period &= 0xffff;
        _envelopePeriod = period == 0 ? 1 : period;
    }

    public void SetEnvelopeShape(int shape)
    {
        _envelopeShape = shape & 0x0f;
        _envelopeCounter = 0;
        _envelopeSegment = 0;
        ResetSegment();
    }

    public (float Left, float Right) ProcessSample()
    {
        var firOffset = FirSize - (_firIndex * DecimateFactor);
        _firIndex = (_firIndex + 1) % ((FirSize / DecimateFactor) - 1);
        var leftInterpolator = _interpolatorLeft;
        var rightInterpolator = _interpolatorRight;

        for (var i = DecimateFactor - 1; i >= 0; i--)
        {
            _x += _step;
            if (_x >= 1d)
            {
                _x -= 1d;
                leftInterpolator.Y0 = leftInterpolator.Y1;
                leftInterpolator.Y1 = leftInterpolator.Y2;
                leftInterpolator.Y2 = leftInterpolator.Y3;
                rightInterpolator.Y0 = rightInterpolator.Y1;
                rightInterpolator.Y1 = rightInterpolator.Y2;
                rightInterpolator.Y2 = rightInterpolator.Y3;

                UpdateMixer();

                leftInterpolator.Y3 = _left;
                rightInterpolator.Y3 = _right;

                var leftY1 = leftInterpolator.Y2 - leftInterpolator.Y0;
                leftInterpolator.C0 = (0.5d * leftInterpolator.Y1) + (0.25d * (leftInterpolator.Y0 + leftInterpolator.Y2));
                leftInterpolator.C1 = 0.5d * leftY1;
                leftInterpolator.C2 = 0.25d * (leftInterpolator.Y3 - leftInterpolator.Y1 - leftY1);

                var rightY1 = rightInterpolator.Y2 - rightInterpolator.Y0;
                rightInterpolator.C0 = (0.5d * rightInterpolator.Y1) + (0.25d * (rightInterpolator.Y0 + rightInterpolator.Y2));
                rightInterpolator.C1 = 0.5d * rightY1;
                rightInterpolator.C2 = 0.25d * (rightInterpolator.Y3 - rightInterpolator.Y1 - rightY1);
            }

            _firLeft[firOffset + i] = ((leftInterpolator.C2 * _x) + leftInterpolator.C1) * _x + leftInterpolator.C0;
            _firRight[firOffset + i] = ((rightInterpolator.C2 * _x) + rightInterpolator.C1) * _x + rightInterpolator.C0;
        }

        _left = Decimate(_firLeft, firOffset);
        _right = Decimate(_firRight, firOffset);
        var left = ApplyDcFilter(_dcFilterLeft, _left);
        var right = ApplyDcFilter(_dcFilterRight, _right);
        _dcIndex = (_dcIndex + 1) & (DcFilterSize - 1);
        return ((float)left, (float)right);
    }

    private double ApplyDcFilter(DcFilterState filter, double value)
    {
        filter.Sum += -filter.Delay[_dcIndex] + value;
        filter.Delay[_dcIndex] = value;
        return value - (filter.Sum / DcFilterSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateMixer()
    {
        var noise = UpdateNoise();
        var envelope = UpdateEnvelope();
        var left = 0d;
        var right = 0d;

        var channel0 = _channels[0];
        var output0 = (UpdateTone(channel0) | channel0.ToneDisabled) & (noise | channel0.NoiseDisabled);
        output0 *= channel0.EnvelopeEnabled != 0 ? envelope : (channel0.Volume * 2) + 1;
        var dacValue0 = _dacTable[output0 & 31];
        left += dacValue0 * channel0.PanLeft;
        right += dacValue0 * channel0.PanRight;

        var channel1 = _channels[1];
        var output1 = (UpdateTone(channel1) | channel1.ToneDisabled) & (noise | channel1.NoiseDisabled);
        output1 *= channel1.EnvelopeEnabled != 0 ? envelope : (channel1.Volume * 2) + 1;
        var dacValue1 = _dacTable[output1 & 31];
        left += dacValue1 * channel1.PanLeft;
        right += dacValue1 * channel1.PanRight;

        var channel2 = _channels[2];
        var output2 = (UpdateTone(channel2) | channel2.ToneDisabled) & (noise | channel2.NoiseDisabled);
        output2 *= channel2.EnvelopeEnabled != 0 ? envelope : (channel2.Volume * 2) + 1;
        var dacValue2 = _dacTable[output2 & 31];
        left += dacValue2 * channel2.PanLeft;
        right += dacValue2 * channel2.PanRight;

        _left = left;
        _right = right;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int UpdateTone(ChannelState channel)
    {
        channel.ToneCounter++;
        if (channel.ToneCounter >= channel.TonePeriod)
        {
            channel.ToneCounter = 0;
            channel.Tone ^= 1;
        }

        return channel.Tone;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int UpdateNoise()
    {
        _noiseCounter++;
        if (_noiseCounter >= (_noisePeriod << 1))
        {
            _noiseCounter = 0;
            var bit0X3 = (_noise ^ (_noise >> 3)) & 1;
            _noise = (_noise >> 1) | (bit0X3 << 16);
        }

        return _noise & 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int UpdateEnvelope()
    {
        _envelopeCounter++;
        if (_envelopeCounter >= _envelopePeriod)
        {
            _envelopeCounter = 0;
            switch (EnvelopeShapes[(_envelopeShape << 1) | _envelopeSegment])
            {
                case EnvelopeAction.SlideUp:
                    SlideUp();
                    break;
                case EnvelopeAction.SlideDown:
                    SlideDown();
                    break;
                case EnvelopeAction.HoldTop:
                case EnvelopeAction.HoldBottom:
                    break;
            }
        }

        return _envelope;
    }

    private void SlideUp()
    {
        _envelope++;
        if (_envelope > 31)
        {
            _envelopeSegment ^= 1;
            ResetSegment();
        }
    }

    private void SlideDown()
    {
        _envelope--;
        if (_envelope < 0)
        {
            _envelopeSegment ^= 1;
            ResetSegment();
        }
    }

    private void ResetSegment()
    {
        var action = EnvelopeShapes[(_envelopeShape << 1) | _envelopeSegment];
        _envelope = action is EnvelopeAction.SlideDown or EnvelopeAction.HoldTop ? 31 : 0;
    }

    private void SetPan(int index, double pan, bool equalPower = false)
    {
        if (equalPower)
        {
            _channels[index].PanLeft = Math.Sqrt(1d - pan);
            _channels[index].PanRight = Math.Sqrt(pan);
        }
        else
        {
            _channels[index].PanLeft = 1d - pan;
            _channels[index].PanRight = pan;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double Decimate(double[] buffer, int offset)
    {
        var y = 0d;
        for (var kernelIndex = 0; kernelIndex < FirSize / 2; kernelIndex += DecimateFactor)
        {
            var sampleOffset = kernelIndex + 1;
            y += DecimateKernel[kernelIndex] * (buffer[offset + sampleOffset] + buffer[offset + (FirSize - sampleOffset)]);
            y += DecimateKernel[kernelIndex + 1] * (buffer[offset + sampleOffset + 1] + buffer[offset + (FirSize - sampleOffset - 1)]);
            y += DecimateKernel[kernelIndex + 2] * (buffer[offset + sampleOffset + 2] + buffer[offset + (FirSize - sampleOffset - 2)]);
            y += DecimateKernel[kernelIndex + 3] * (buffer[offset + sampleOffset + 3] + buffer[offset + (FirSize - sampleOffset - 3)]);
            y += DecimateKernel[kernelIndex + 4] * (buffer[offset + sampleOffset + 4] + buffer[offset + (FirSize - sampleOffset - 4)]);
            y += DecimateKernel[kernelIndex + 5] * (buffer[offset + sampleOffset + 5] + buffer[offset + (FirSize - sampleOffset - 5)]);
            y += DecimateKernel[kernelIndex + 6] * (buffer[offset + sampleOffset + 6] + buffer[offset + (FirSize - sampleOffset - 6)]);
        }

        y += DecimateKernel[^1] * buffer[offset + (FirSize / 2)];

        var copyDest = offset + FirSize - DecimateFactor;
        buffer[copyDest] = buffer[offset];
        buffer[copyDest + 1] = buffer[offset + 1];
        buffer[copyDest + 2] = buffer[offset + 2];
        buffer[copyDest + 3] = buffer[offset + 3];
        buffer[copyDest + 4] = buffer[offset + 4];
        buffer[copyDest + 5] = buffer[offset + 5];
        buffer[copyDest + 6] = buffer[offset + 6];
        buffer[copyDest + 7] = buffer[offset + 7];

        return y;
    }

    private enum EnvelopeAction
    {
        SlideUp,
        SlideDown,
        HoldTop,
        HoldBottom,
    }

    private sealed class ChannelState
    {
        // Fields keep the hot mixer path free of property accessor overhead.
        public int ToneCounter;
        public int TonePeriod;
        public int Tone;
        public int ToneDisabled;
        public int NoiseDisabled;
        public int EnvelopeEnabled;
        public int Volume;
        public double PanLeft = 1d;
        public double PanRight = 1d;

        public void Reset()
        {
            ToneCounter = 0;
            TonePeriod = 0;
            Tone = 0;
            ToneDisabled = 0;
            NoiseDisabled = 0;
            EnvelopeEnabled = 0;
            Volume = 0;
            PanLeft = 1d;
            PanRight = 1d;
        }
    }

    private sealed class InterpolatorState
    {
        // Fields avoid accessor overhead in the interpolation hot path.
        public double C0;
        public double C1;
        public double C2;
        public double Y0;
        public double Y1;
        public double Y2;
        public double Y3;

        public void Reset()
        {
            C0 = 0d;
            C1 = 0d;
            C2 = 0d;
            Y0 = 0d;
            Y1 = 0d;
            Y2 = 0d;
            Y3 = 0d;
        }
    }

    private sealed class DcFilterState(int size)
    {
        public double Sum { get; set; }

        public double[] Delay { get; } = new double[size];

        public void Reset()
        {
            Sum = 0;
            Array.Clear(Delay);
        }
    }
}