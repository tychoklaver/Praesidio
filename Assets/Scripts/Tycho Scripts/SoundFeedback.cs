using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Procedural sound effects (no audio files needed). Watches the game the same way
/// GameFeedback does and plays: enemy hit/death, building placed/hit/destroyed,
/// keep hit + low-health warning, wave horn, defeat sting.
/// Does not change any teammate's code.
/// </summary>
public class SoundFeedback : MonoBehaviour
{
    [SerializeField] DBTest keep;
    [SerializeField, Range(0f, 1f)] float volume = 0.6f;
    [SerializeField, Range(0f, 1f)] float lowHealthFraction = 0.35f;

    static readonly FieldInfo UnitHealthField = typeof(UnitHealth).GetField("health", BindingFlags.Instance | BindingFlags.NonPublic);
    static readonly FieldInfo KeepHealthField = typeof(DBTest).GetField("health", BindingFlags.Instance | BindingFlags.NonPublic);

    const int Rate = 44100;

    AudioSource _source;
    AudioClip _hit, _death, _place, _buildingHit, _buildingDestroyed, _keepHit, _horn, _warning, _defeat;

    readonly Dictionary<UnitHealth, float> _last = new Dictionary<UnitHealth, float>();
    readonly HashSet<UnitHealth> _dead = new HashSet<UnitHealth>();
    readonly HashSet<BuildingHealth> _hooked = new HashSet<BuildingHealth>();
    readonly Dictionary<string, float> _cooldown = new Dictionary<string, float>();
    readonly List<UnitHealth> _remove = new List<UnitHealth>();

    float _keepLast, _keepMax, _scanTimer, _warnTimer;
    bool _firstScan = true, _hadEnemies, _defeatPlayed;

    void Awake()
    {
        _source = gameObject.AddComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.spatialBlend = 0f;

        _hit = Make(0.08f, t => Noise() * Env(t, 0.08f, 25f) * 0.7f + Mathf.Sin(2f * Mathf.PI * 220f * t) * Env(t, 0.08f, 30f) * 0.4f);
        _death = Make(0.3f, t => Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(420f, 90f, t / 0.3f) * t) * Env(t, 0.3f, 8f) * 0.8f + Noise() * Env(t, 0.3f, 18f) * 0.3f);
        _place = Make(0.22f, t => Mathf.Sin(2f * Mathf.PI * 110f * t) * Env(t, 0.22f, 14f) + Noise() * Env(t, 0.22f, 40f) * 0.5f);
        _buildingHit = Make(0.1f, t => Square(300f, t) * Env(t, 0.1f, 28f) * 0.35f + Noise() * Env(t, 0.1f, 30f) * 0.5f);
        _buildingDestroyed = Make(0.6f, t => Noise() * Env(t, 0.6f, 6f) * 0.8f + Mathf.Sin(2f * Mathf.PI * 60f * t) * Env(t, 0.6f, 7f));
        _keepHit = Make(0.45f, t => Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(150f, 50f, t / 0.45f) * t) * Env(t, 0.45f, 6f) + Noise() * Env(t, 0.45f, 20f) * 0.4f);
        _horn = Make(1.1f, t => (Saw(196f, t) + Saw(247f, t) * 0.8f) * 0.35f * Mathf.Min(t * 12f, 1f) * Mathf.Clamp01((1.1f - t) * 5f));
        _warning = Make(0.18f, t => Mathf.Sin(2f * Mathf.PI * 880f * t) * Env(t, 0.18f, 14f) * 0.5f);
        _defeat = Make(1.6f, t => Saw(Mathf.Lerp(220f, 55f, t / 1.6f), t) * 0.4f * Mathf.Clamp01((1.6f - t) * 2f));

        if (keep != null) _keepLast = _keepMax = (float)KeepHealthField.GetValue(keep);
    }

    void Update()
    {
        PollEnemies();
        PollBuildings();
        PollKeep();
    }

    // ---------- watching the game ----------

    void PollEnemies()
    {
        int alive = 0;
        foreach (UnitHealth unit in UnitHealth.all)
        {
            if (unit == null) continue;
            float hp = (float)UnitHealthField.GetValue(unit);

            if (unit.isDead)
            {
                if (_dead.Add(unit)) Play(_death, "death", 0.05f, 0.9f);
                continue;
            }
            alive++;
            if (_last.TryGetValue(unit, out float last) && hp < last - 0.01f) Play(_hit, "hit", 0.04f, 0.6f);
            _last[unit] = hp;
        }

        // wave start = several units appear when there were none
        if (alive >= 3 && !_hadEnemies) Play(_horn, "horn", 1f, 1f);
        if (alive == 0) _hadEnemies = false; else if (alive >= 3) _hadEnemies = true;

        _remove.Clear();
        foreach (var pair in _last) if (pair.Key == null) _remove.Add(pair.Key);
        foreach (UnitHealth gone in _remove) { _last.Remove(gone); _dead.Remove(gone); }
    }

    void PollBuildings()
    {
        _scanTimer -= Time.unscaledDeltaTime;
        if (_scanTimer > 0f) return;
        _scanTimer = 0.2f;

        foreach (BuildingHealth b in FindObjectsByType<BuildingHealth>(FindObjectsSortMode.None))
        {
            if (!_hooked.Add(b)) continue;
            if (!_firstScan) Play(_place, "place", 0.05f, 1f);

            float last = b.Health;
            b.OnHealthChanged += (hp, max) =>
            {
                if (hp < last - 0.01f) Play(_buildingHit, "bhit", 0.08f, 0.6f);
                last = hp;
            };
            b.OnDied += () => Play(_buildingDestroyed, "bdead", 0.1f, 1f);
        }
        _firstScan = false;
    }

    void PollKeep()
    {
        if (keep == null)
        {
            if (_keepMax > 0f && !_defeatPlayed) { _defeatPlayed = true; Play(_defeat, "defeat", 0f, 1f); }
            return;
        }

        float hp = (float)KeepHealthField.GetValue(keep);
        if (hp > _keepMax) _keepMax = hp;
        if (hp < _keepLast - 0.01f) Play(_keepHit, "keep", 0.1f, 1f);
        _keepLast = hp;

        if (_keepMax > 0f && hp / _keepMax < lowHealthFraction)
        {
            _warnTimer -= Time.unscaledDeltaTime;
            if (_warnTimer <= 0f) { Play(_warning, "warn", 0f, 0.5f); _warnTimer = 1f; }
        }
    }

    // ---------- playing ----------

    void Play(AudioClip clip, string key, float minGap, float gain)
    {
        float now = Time.unscaledTime;
        if (_cooldown.TryGetValue(key, out float next) && now < next) return;
        _cooldown[key] = now + minGap;

        _source.pitch = UnityEngine.Random.Range(0.92f, 1.08f);
        _source.PlayOneShot(clip, volume * gain);
    }

    // ---------- synthesis ----------

    static AudioClip Make(float seconds, Func<float, float> sample)
    {
        int count = Mathf.CeilToInt(seconds * Rate);
        float[] data = new float[count];
        for (int i = 0; i < count; i++) data[i] = Mathf.Clamp(sample(i / (float)Rate), -1f, 1f);
        AudioClip clip = AudioClip.Create("sfx", count, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static float Env(float t, float length, float decay) => Mathf.Exp(-decay * t) * Mathf.Clamp01((length - t) * 50f);
    static float Noise() => UnityEngine.Random.value * 2f - 1f;
    static float Square(float freq, float t) => Mathf.Sin(2f * Mathf.PI * freq * t) >= 0f ? 1f : -1f;
    static float Saw(float freq, float t) => 2f * ((t * freq) % 1f) - 1f;
}