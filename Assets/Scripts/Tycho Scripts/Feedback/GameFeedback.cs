using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Adds game feel without touching teammates' scripts:
/// hit flash + damage numbers on enemies and buildings, death puffs with reward popup,
/// screen shake and red vignette when the keep is hit. Everything is created in code.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class GameFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Camera mainCamera;
    [SerializeField] DBTest keep;

    [Header("Hit flash / numbers")]
    [SerializeField] Color flashColor = new Color(1f, 0.55f, 0.55f, 1f);
    [SerializeField] float flashTime = 0.08f;
    [SerializeField] Color damageColor = new Color(1f, 0.9f, 0.4f, 1f);
    [SerializeField] float numberSize = 3f;

    [Header("Kill popup")]
    [SerializeField] string killText = "+5 Stone";
    [SerializeField] Color killColor = new Color(0.7f, 1f, 0.6f, 1f);

    [Header("Keep hit")]
    [SerializeField] float shakeAmount = 0.15f;
    [SerializeField] float shakeTime = 0.25f;
    [SerializeField, Range(0f, 1f)] float lowHealthFraction = 0.35f;

    static readonly FieldInfo UnitHealthField = typeof(UnitHealth).GetField("health", BindingFlags.Instance | BindingFlags.NonPublic);
    static readonly FieldInfo KeepHealthField = typeof(DBTest).GetField("health", BindingFlags.Instance | BindingFlags.NonPublic);

    readonly Dictionary<UnitHealth, float> _lastHealth = new Dictionary<UnitHealth, float>();
    readonly HashSet<UnitHealth> _dead = new HashSet<UnitHealth>();
    readonly HashSet<BuildingHealth> _hooked = new HashSet<BuildingHealth>();
    readonly List<UnitHealth> _remove = new List<UnitHealth>();

    Sprite _square;
    Image _vignette;
    Vector3 _shakeOffset;
    float _shakeLeft;
    float _keepMax, _keepLast;
    float _hitFlash;

    void OnEnable() => LevelController.WaveStarted += OnWaveStarted;
    void OnDisable() => LevelController.WaveStarted -= OnWaveStarted;

    // Boss wave: a long, heavy shake and a red flash, so it feels like something big arrived.
    void OnWaveStarted(bool pBossWave)
    {
        if (!pBossWave) return;
        _shakeLeft = shakeTime * 4f;
        _hitFlash = 0.7f;
    }

    void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        _square = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
        BuildVignette();
        if (keep != null)
        {
            _keepLast = _keepMax = ReadKeep();
        }
    }

    // ---------- per frame ----------

    void Update()
    {
        // remove last frame's shake before anything else moves the camera
        if (mainCamera != null) mainCamera.transform.position -= _shakeOffset;
        _shakeOffset = Vector3.zero;

        PollEnemies();
        HookBuildings();
        PollKeep();
    }

    void LateUpdate()
    {
        if (mainCamera != null && _shakeLeft > 0f)
        {
            _shakeLeft -= Time.unscaledDeltaTime;
            float power = shakeAmount * Mathf.Clamp01(_shakeLeft / shakeTime);
            _shakeOffset = (Vector3)Random.insideUnitCircle * power;
            mainCamera.transform.position += _shakeOffset;
        }

        UpdateVignette();
    }

    // ---------- enemies ----------

    void PollEnemies()
    {
        foreach (UnitHealth unit in UnitHealth.all)
        {
            if (unit == null) continue;

            float hp = (float)UnitHealthField.GetValue(unit);

            if (unit.isDead)
            {
                if (_dead.Add(unit)) OnEnemyDied(unit);
                continue;
            }

            if (_lastHealth.TryGetValue(unit, out float last) && hp < last - 0.01f)
            {
                SpawnNumber(unit.transform.position + Vector3.up * 0.4f, Mathf.RoundToInt(last - hp).ToString(), damageColor, numberSize);
                StartCoroutine(Flash(unit.gameObject));
            }
            _lastHealth[unit] = hp;
        }

        // forget destroyed units
        _remove.Clear();
        foreach (var pair in _lastHealth)
            if (pair.Key == null) _remove.Add(pair.Key);
        foreach (UnitHealth gone in _remove) { _lastHealth.Remove(gone); _dead.Remove(gone); }
    }

    void OnEnemyDied(UnitHealth unit)
    {
        Vector3 pos = unit.transform.position;
        SpawnPuff(pos, new Color(0.85f, 0.85f, 0.8f, 1f), 8);
        if (!string.IsNullOrEmpty(killText))
            SpawnNumber(pos + Vector3.up * 0.7f, killText, killColor, numberSize * 0.9f);
    }

    // ---------- buildings ----------

    void HookBuildings()
    {
        foreach (BuildingHealth b in FindObjectsByType<BuildingHealth>(FindObjectsSortMode.None))
        {
            if (!_hooked.Add(b)) continue;
            BuildingHealth captured = b;
            float last = b.Health;
            b.OnHealthChanged += (hp, max) =>
            {
                if (captured == null) return;
                if (hp < last - 0.01f)
                {
                    SpawnNumber(captured.transform.position + Vector3.up * 0.6f, Mathf.RoundToInt(last - hp).ToString(), new Color(1f, 0.5f, 0.4f), numberSize);
                    StartCoroutine(Flash(captured.gameObject));
                }
                last = hp;
            };
            b.OnDied += () =>
            {
                if (captured != null) SpawnPuff(captured.transform.position, new Color(0.6f, 0.5f, 0.4f, 1f), 14);
            };
        }
    }

    // ---------- keep ----------

    float ReadKeep() => keep != null ? (float)KeepHealthField.GetValue(keep) : 0f;

    void PollKeep()
    {
        if (keep == null) return; // destroyed or not assigned

        float hp = ReadKeep();
        if (hp > _keepMax) _keepMax = hp;
        if (hp < _keepLast - 0.01f)
        {
            _shakeLeft = shakeTime;
            _hitFlash = 1f;
            SpawnNumber(keep.transform.position + Vector3.up * 1.2f, "-" + Mathf.RoundToInt(_keepLast - hp), new Color(1f, 0.3f, 0.3f), numberSize * 1.3f);
            StartCoroutine(Flash(keep.gameObject));
        }
        _keepLast = hp;
    }

    // ---------- vignette ----------

    void BuildVignette()
    {
        GameObject canvasObject = new GameObject("FeedbackCanvas", typeof(Canvas));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        GameObject imageObject = new GameObject("Vignette", typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);
        _vignette = imageObject.GetComponent<Image>();
        _vignette.raycastTarget = false;
        RectTransform rect = _vignette.rectTransform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;

        const int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x / (size - 1f) - 0.5f) * 2f;
                float dy = (y / (size - 1f) - 0.5f) * 2f;
                float d = Mathf.Clamp01((Mathf.Sqrt(dx * dx + dy * dy) - 0.55f) / 0.9f);
                tex.SetPixel(x, y, new Color(1f, 0f, 0f, d * d));
            }
        tex.Apply();
        _vignette.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        _vignette.color = new Color(1, 1, 1, 0);
    }

    void UpdateVignette()
    {
        _hitFlash = Mathf.MoveTowards(_hitFlash, 0f, Time.unscaledDeltaTime * 2.5f);
        float alpha = _hitFlash * 0.8f;

        if (keep != null && _keepMax > 0f)
        {
            float fraction = _keepLast / _keepMax;
            if (fraction < lowHealthFraction)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f);
                alpha = Mathf.Max(alpha, Mathf.Lerp(0.25f, 0.6f, pulse) * (1f - fraction / lowHealthFraction * 0.5f));
            }
        }
        _vignette.color = new Color(1, 1, 1, alpha);
    }

    // ---------- effects ----------

    IEnumerator Flash(GameObject target)
    {
        SpriteRenderer[] renderers = target.GetComponentsInChildren<SpriteRenderer>();
        Color[] originals = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) { originals[i] = renderers[i].color; renderers[i].color = flashColor; }

        yield return new WaitForSeconds(flashTime);

        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].color = originals[i];
    }

    void SpawnNumber(Vector3 position, string text, Color color, float size)
    {
        StartCoroutine(FloatText(position, text, color, size));
    }

    IEnumerator FloatText(Vector3 start, string text, Color color, float size)
    {
        GameObject go = new GameObject("Popup");
        go.transform.position = start + (Vector3)(Random.insideUnitCircle * 0.15f);
        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.sortingOrder = 100;
        tmp.outlineWidth = 0.2f;
        tmp.outlineColor = new Color32(0, 0, 0, 255);

        const float life = 0.8f;
        for (float t = 0; t < life; t += Time.deltaTime)
        {
            if (go == null) yield break;
            go.transform.position += Vector3.up * Time.deltaTime * 0.9f;
            Color c = color; c.a = 1f - Mathf.Clamp01((t / life - 0.5f) * 2f);
            tmp.color = c;
            yield return null;
        }
        Destroy(go);
    }

    void SpawnPuff(Vector3 position, Color color, int count)
    {
        for (int i = 0; i < count; i++) StartCoroutine(Particle(position, color));
    }

    IEnumerator Particle(Vector3 start, Color color)
    {
        GameObject go = new GameObject("Particle");
        go.transform.position = start;
        float scale = Random.Range(0.05f, 0.14f);
        go.transform.localScale = new Vector3(scale, scale, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = _square;
        sr.color = color;
        sr.sortingOrder = 90;

        Vector3 velocity = (Vector3)(Random.insideUnitCircle.normalized * Random.Range(0.8f, 2.2f));
        float life = Random.Range(0.35f, 0.6f);
        for (float t = 0; t < life; t += Time.deltaTime)
        {
            if (go == null) yield break;
            go.transform.position += velocity * Time.deltaTime;
            velocity *= 1f - Time.deltaTime * 3f;
            Color c = color; c.a = 1f - t / life;
            sr.color = c;
            yield return null;
        }
        Destroy(go);
    }
}