using System;
using Godot;
using NiceTouch.Sim;

namespace NiceTouch;

// Placeholder sound: four plain beeps made here in code, so there are no
// sound files to credit. Real sound arrives in M2.
public partial class Beeps : Node
{
    private const int SampleRate = 44100;

    private readonly AudioStreamPlayer _paddleHit = Player(pitch: 880, seconds: 0.07f);
    private readonly AudioStreamPlayer _wallBounce = Player(pitch: 440, seconds: 0.05f);
    private readonly AudioStreamPlayer _miss = Player(pitch: 150, seconds: 0.4f);
    private readonly AudioStreamPlayer _niceTouch = Player(pitch: 1320, seconds: 0.5f);

    public override void _Ready()
    {
        AddChild(_paddleHit);
        AddChild(_wallBounce);
        AddChild(_miss);
        AddChild(_niceTouch);
    }

    // Plays a beep for each thing the sim says happened this tick.
    public void Play(TickEvents events)
    {
        if (events.HasFlag(TickEvents.PaddleHit))
            _paddleHit.Play();
        if (events.HasFlag(TickEvents.WallBounce))
            _wallBounce.Play();
        if (events.HasFlag(TickEvents.Miss))
            _miss.Play();
        if (events.HasFlag(TickEvents.NiceTouch))
            _niceTouch.Play();
    }

    // A sine tone that fades out, as 16-bit samples.
    private static AudioStreamPlayer Player(float pitch, float seconds)
    {
        int count = (int)(SampleRate * seconds);
        byte[] data = new byte[count * 2];
        for (int i = 0; i < count; i++)
        {
            float fade = 1 - (float)i / count;
            float wave = MathF.Sin(MathF.Tau * pitch * i / SampleRate);
            BitConverter.TryWriteBytes(data.AsSpan(i * 2), (short)(wave * fade * 0.35f * short.MaxValue));
        }

        return new AudioStreamPlayer
        {
            Stream = new AudioStreamWav
            {
                Format = AudioStreamWav.FormatEnum.Format16Bits,
                MixRate = SampleRate,
                Data = data,
            },
        };
    }
}
