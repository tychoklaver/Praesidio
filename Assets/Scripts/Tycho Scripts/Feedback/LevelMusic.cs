using System;
using UnityEngine;

/// <summary>
/// Background music for the level. Put it ONLY in the level scene: the music then starts with the level
/// and ends when another scene loads (main menu, achievements, ...). It fades out and stops while the game
/// is paused or over (Time.timeScale is 0 then) and fades back in when play continues.
/// Assign your own track in <see cref="musicClip"/>. Left empty, a short medieval-style loop is generated in code.
/// </summary>
public class LevelMusic : MonoBehaviour
{
    [Tooltip("Your own looping track (e.g. a CC0 track from Kenney.nl or OpenGameArt). Empty = generated loop.")]
    [SerializeField] private AudioClip musicClip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.35f;
    [SerializeField, Min(0.05f)] private float fadeSeconds = 0.6f;

    private AudioSource _source;
    private float _fade; // 0 = silent, 1 = full volume.

    private void Awake()
    {
        _source = gameObject.AddComponent<AudioSource>();
        _source.clip = musicClip != null ? musicClip : GenerateLoop();
        _source.loop = true;
        _source.playOnAwake = false;
        _source.spatialBlend = 0f;
        _source.volume = 0f;
        _source.Play();
    }

    private void Update()
    {
        // timeScale 0 = pause menu or win/lose screen. Unscaled time, or the fade would freeze with the game.
        bool shouldPlay = Time.timeScale > 0f;

        _fade = Mathf.MoveTowards(_fade, shouldPlay ? 1f : 0f, Time.unscaledDeltaTime / fadeSeconds);
        _source.volume = _fade * volume;

        if (!shouldPlay && _fade <= 0f) _source.Pause();
        else if (shouldPlay) _source.UnPause();
    }

    // ---------- generated fallback loop ----------

    private const int SampleRate = 22050;
    private const float Bpm = 84f;

    // Chords as three frequencies (Hz), bass note last. Progression: Am F C G Am F G Am, twice.
    private static readonly float[][] Chords =
    {
        new[] { 220f, 261.63f, 329.63f, 110f },   // Am
        new[] { 174.61f, 220f, 261.63f, 87.31f }, // F
        new[] { 261.63f, 329.63f, 392f, 130.81f },// C
        new[] { 196f, 246.94f, 293.66f, 98f }     // G
    };
    private static readonly int[] Progression = { 0, 1, 2, 3, 0, 1, 3, 0 };
    private static readonly float[] Scale = { 440f, 523.25f, 587.33f, 659.25f, 783.99f, 880f }; // A minor pentatonic.

    private static AudioClip GenerateLoop()
    {
        float beat = 60f / Bpm;
        float bar = beat * 4f;
        int bars = Progression.Length * 2;
        int total = Mathf.CeilToInt(bars * bar * SampleRate);
        float[] data = new float[total];
        var random = new System.Random(7); // Fixed seed: the same melody every run.

        for (int b = 0; b < bars; b++)
        {
            float[] chord = Chords[Progression[b % Progression.Length]];
            float barStart = b * bar;

            // Pad: soft chord held for the bar, fading in and out so the loop point is silent.
            for (int i = 0; i < 3; i++)
                AddTone(data, barStart, bar, chord[i], 0.07f, Envelope.Pad);

            // Bass: beats 1 and 3.
            AddTone(data, barStart, beat * 1.8f, chord[3], 0.22f, Envelope.Pluck);
            AddTone(data, barStart + beat * 2f, beat * 1.8f, chord[3], 0.18f, Envelope.Pluck);

            // Melody: eighth notes, random walk over the scale, with rests.
            int step = random.Next(Scale.Length);
            for (int n = 0; n < 8; n++)
            {
                step = Mathf.Clamp(step + random.Next(-1, 2), 0, Scale.Length - 1);
                if (random.NextDouble() < 0.45) continue; // Rest.
                AddTone(data, barStart + n * beat * 0.5f, beat * 1.5f, Scale[step], 0.10f, Envelope.Pluck);
            }
        }

        // Keep the mix away from clipping.
        float peak = 0.0001f;
        foreach (float sample in data) peak = Mathf.Max(peak, Mathf.Abs(sample));
        float gain = 0.8f / peak;
        for (int i = 0; i < data.Length; i++) data[i] *= gain;

        AudioClip clip = AudioClip.Create("GeneratedLevelMusic", total, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private enum Envelope { Pad, Pluck }

    private static void AddTone(float[] pData, float pStartSeconds, float pLength, float pFrequency, float pAmplitude, Envelope pShape)
    {
        int start = Mathf.RoundToInt(pStartSeconds * SampleRate);
        int count = Mathf.RoundToInt(pLength * SampleRate);

        for (int i = 0; i < count; i++)
        {
            int index = start + i;
            if (index >= pData.Length) break; // Notes never wrap past the loop end.

            float t = i / (float)SampleRate;
            float progress = i / (float)count;

            float envelope = pShape == Envelope.Pad
                ? Mathf.Sin(Mathf.PI * progress)                               // Slow rise and fall.
                : Mathf.Exp(-4.5f * progress) * Mathf.Min(t * 200f, 1f);       // Quick attack, plucked decay.

            float wave = Mathf.Sin(2f * Mathf.PI * pFrequency * t)
                       + 0.3f * Mathf.Sin(4f * Mathf.PI * pFrequency * t);     // A touch of the 2nd harmonic.

            pData[index] += wave * envelope * pAmplitude;
        }
    }
}