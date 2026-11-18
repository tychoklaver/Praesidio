using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runs one level on top of the existing WaveManager: starts the waves on a timer, counts the
/// living enemies, pays out kill rewards, makes later waves tougher, fills the HUD and decides
/// victory or defeat.
///   Victory: the last wave has spawned and every enemy is dead.
///   Defeat:  the keep (the object the enemies march towards) is destroyed.
/// Nothing in other people's scripts is changed; their private fields are read by reflection.
/// </summary>
public class LevelController : MonoBehaviour
{
    [Header("Waves")]
    [Tooltip("The WaveManager (with WaveBuilder and WaveFormations on the same object).")]
    [SerializeField] WaveManager waveManager;
    [SerializeField, Min(1)] int totalWaves = 5;
    [SerializeField, Min(0f)] float firstWaveDelay = 30f;
    [SerializeField, Min(0f)] float timeBetweenWaves = 45f;
    [Tooltip("Start the next wave early (after a short pause) when all enemies are dead.")]
    [SerializeField] bool skipWaitWhenCleared = true;
    [SerializeField, Min(0f)] float clearedPause = 5f;
    [Tooltip("Optional. Pressing it starts the next wave right away.")]
    [SerializeField] Button startWaveButton;

    [Header("Difficulty")]
    [Tooltip("Extra enemy health per wave. 0.15 means wave 2 has 15% more health than wave 1.")]
    [SerializeField, Min(0f)] float healthGrowthPerWave = 0.15f;

    [Header("Boss (last wave)")]
    [Tooltip("Optional. A prefab with UnitAIBase + UnitHealth, e.g. King or VikingJarl. It joins the final wave.")]
    [SerializeField] UnitAIBase bossPrefab;
    [Tooltip("Where the boss appears. Empty = (8, 0).")]
    [SerializeField] Transform bossSpawn;
    [SerializeField, Min(1f)] float bossHealth = 400f;
    [SerializeField, Min(0.5f)] float bossScale = 1.6f;
    [SerializeField] ResourceCost[] bossReward = { new ResourceCost { type = ResourceType.Stone, amount = 50 } };

    [Header("Start early bonus")]
    [Tooltip("Stone paid per 5 seconds skipped when you press Start Wave early. 0 = off.")]
    [SerializeField, Min(0)] int earlyBonusPerFiveSeconds = 1;

    [Header("Banner")]
    [SerializeField] float bannerSeconds = 4f;
    [Tooltip("Drag your Texturina TMP Font Asset here. Empty = default font.")]
    [SerializeField] TMP_FontAsset bannerFont;
    [SerializeField, Range(0f, 1f)] float bannerBackground = 0.55f;

    [Header("Rewards")]
    [Tooltip("Paid for every kill. Wood is left out on purpose: it would count as 'wood gathered' for achievements.")]
    [SerializeField] ResourceCost[] killReward = { new ResourceCost { type = ResourceType.Stone, amount = 5 } };

    [Header("Win and lose")]
    [Tooltip("Destroying this object loses the level. It needs a DBTest so the enemies target it.")]
    [SerializeField] DBTest keep;
    [SerializeField] WinLoseUI winLose;

    [Header("HUD")]
    [Tooltip("Optional. Leave empty to keep the HUD wave text unchanged.")]
    [SerializeField] InGameUI hud;
    [Tooltip("Optional. A text that shows the countdown to the next wave.")]
    [SerializeField] TMP_Text countdownText;

    // Private fields of other scripts that this script reads or writes (see FindFields).
    static readonly BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    FieldInfo _waveField, _maxWaveField, _enemiesField, _unitHealthField;

    // WaveBuilder forgets the enemies of earlier waves, so every wave is copied into this list.
    readonly List<UnitHealth> _enemies = new();
    WaveBuilder _builder;

    UnitHealth _boss;
    int _pendingBonus;
    int _kills, _buildingsLost;
    long _woodAtStart;
    readonly HashSet<BuildingHealth> _hookedBuildings = new();
    float _scanTimer;
    TMP_Text _bannerTitle, _bannerSub;
    CanvasGroup _bannerGroup;
    Coroutine _bannerRoutine;

    int _wave;
    float _secondsLeft;
    bool _waiting, _allWavesSpawned, _ended, _startNow;

    int AliveCount => _enemies.Count;

    void Start()
    {
        if (waveManager == null || winLose == null || keep == null)
        {
            Debug.LogError("[LevelController] Assign the wave manager, win/lose UI and keep in the inspector.", this);
            enabled = false;
            return;
        }

        _builder = waveManager.GetComponent<WaveBuilder>();
        FindFields();
        BuildBanner();
        if (StatsTracker.Instance != null) _woodAtStart = StatsTracker.Instance.GetStat("wood_gathered");
        if (startWaveButton != null) startWaveButton.onClick.AddListener(() => _startNow = true);
        StartCoroutine(RunWaves());
    }

    void Update()
    {
        if (_ended) return;

        CollectKills();
        WatchBuildings();
        PushToHud();
        ShowCountdown();

        // 'keep == null' is also true for a destroyed Unity object.
        if (keep == null) End(false);
        else if (_allWavesSpawned && AliveCount == 0) End(true);
    }

    // --- Waves ---------------------------------------------------------------------------------

    IEnumerator RunWaves()
    {
        yield return WaitForNextWave(firstWaveDelay);

        for (_wave = 1; _wave <= totalWaves; _wave++)
        {
            SpawnWave();
            if (_wave == totalWaves) break;

            yield return WaitForNextWave(timeBetweenWaves);
            waveManager.EndWave(); // raises the budget for the next wave
        }
        _allWavesSpawned = true;
    }

    void SpawnWave()
    {
        waveManager.StartWave();

        // StartWave spawns instantly, so the builder now holds exactly this wave's enemies.
        List<UnitHealth> spawned = new();
        Dictionary<string, int> counts = new();
        foreach (UnitAIBase enemy in _builder.GetSpawnedEnemies())
        {
            if (!enemy.TryGetComponent(out UnitHealth health)) continue;
            spawned.Add(health);
            string unitName = enemy.name.Replace("(Clone)", "").Trim();
            counts[unitName] = counts.TryGetValue(unitName, out int n) ? n + 1 : 1;
        }

        _enemies.AddRange(spawned);
        StartCoroutine(ScaleHealth(spawned, 1f + (_wave - 1) * healthGrowthPerWave));

        bool bossWave = _wave == totalWaves && bossPrefab != null;
        if (bossWave) SpawnBoss();

        ShowBanner(bossWave, counts);
    }

    void SpawnBoss()
    {
        Vector3 position = bossSpawn != null ? bossSpawn.position : new Vector3(8f, 0f, 0f);
        UnitAIBase boss = Instantiate(bossPrefab, position, Quaternion.identity);
        boss.transform.localScale *= bossScale;

        if (!boss.TryGetComponent(out _boss)) return;
        _enemies.Add(_boss);
        StartCoroutine(SetBossHealth(_boss));
    }

    IEnumerator SetBossHealth(UnitHealth pBoss)
    {
        if (_unitHealthField == null) yield break;
        yield return null; // after UnitHealth set its own health
        if (pBoss != null) _unitHealthField.SetValue(pBoss, bossHealth);
    }

    // Counts in scaled time, so the speed buttons (0.5x / 1x / 2x) also affect the waves.
    IEnumerator WaitForNextWave(float pSeconds)
    {
        _startNow = false;
        _waiting = true;

        for (_secondsLeft = pSeconds; _secondsLeft > 0f && !_startNow; _secondsLeft -= Time.deltaTime)
        {
            // Only shorten the wait once a wave has run, not before the first one.
            if (_wave > 0 && skipWaitWhenCleared && AliveCount == 0 && _secondsLeft > clearedPause)
                _secondsLeft = clearedPause;
            yield return null;
        }
        _waiting = false;

        if (_startNow && earlyBonusPerFiveSeconds > 0 && ResourceManager.Instance != null)
        {
            int bonus = Mathf.FloorToInt(_secondsLeft / 5f) * earlyBonusPerFiveSeconds;
            if (bonus > 0)
            {
                ResourceManager.Instance.Add(ResourceType.Stone, bonus);
                _pendingBonus = bonus;
            }
        }
    }

    // UnitHealth sets its health from the unit data in its Start, so wait one frame and scale after that.
    IEnumerator ScaleHealth(List<UnitHealth> pUnits, float pFactor)
    {
        if (pFactor <= 1f || _unitHealthField == null) yield break;
        yield return null;

        foreach (UnitHealth unit in pUnits)
        {
            if (unit == null) continue;
            _unitHealthField.SetValue(unit, (float)_unitHealthField.GetValue(unit) * pFactor);
        }
    }

    // --- Kills and rewards ---------------------------------------------------------------------

    // A dead enemy stays in the scene for a few seconds, so it is paid for once and then forgotten.
    void CollectKills()
    {
        for (int i = _enemies.Count - 1; i >= 0; i--)
        {
            UnitHealth enemy = _enemies[i];
            if (enemy != null && !enemy.isDead) continue;

            if (enemy != null)
            {
                _kills++;
                PayKillReward();
                if (enemy == _boss) PayBossReward();
            }
            _enemies.RemoveAt(i);
        }
    }

    void PayKillReward()
    {
        if (ResourceManager.Instance == null) return;
        foreach (ResourceCost reward in killReward) ResourceManager.Instance.Add(reward.type, reward.amount);
    }

    void PayBossReward()
    {
        if (ResourceManager.Instance == null) return;
        foreach (ResourceCost reward in bossReward) ResourceManager.Instance.Add(reward.type, reward.amount);
    }

    // --- Result screen ---------------------------------------------------------------------------
    // The WinLose prefab only contains placeholder texts, so they are filled in here.

    void WatchBuildings()
    {
        _scanTimer -= Time.deltaTime;
        if (_scanTimer > 0f) return;
        _scanTimer = 0.5f;

        foreach (BuildingHealth b in FindObjectsByType<BuildingHealth>(FindObjectsSortMode.None))
        {
            if (b.gameObject == keep.gameObject || !_hookedBuildings.Add(b)) continue;
            b.OnDied += () => _buildingsLost++;
        }
    }

    void FillResults(bool pVictory)
    {
        int survived = pVictory ? totalWaves : Mathf.Max(0, _wave - 1);
        long wood = StatsTracker.Instance != null ? StatsTracker.Instance.GetStat("wood_gathered") - _woodAtStart : 0;
        int seconds = Mathf.FloorToInt(Time.timeSinceLevelLoad);
        string time = (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");

        // Use the screen objects WinLoseUI itself points at; fall back to searching by name.
        FieldInfo screenField = typeof(WinLoseUI).GetField(pVictory ? "victoryScreen" : "defeatScreen", PrivateInstance);
        GameObject screenObject = screenField != null ? screenField.GetValue(winLose) as GameObject : null;
        Transform screen = screenObject != null ? screenObject.transform
            : FindDeep(winLose.transform.root, pVictory ? "VictoryScreen" : "DefeatScreen");
        if (screen == null)
        {
            Debug.LogWarning("[LevelController] Could not find the result screen, texts not filled in.", this);
            return;
        }

        SetRow(screen, "Wavessurvived", survived + " / " + totalWaves);
        SetRow(screen, "Enemiesdefeated", _kills.ToString());
        SetRow(screen, "Woodcollected", wood.ToString());
        SetRow(screen, "Buildingslost", _buildingsLost.ToString());
        SetRow(screen, "Time", time);

        SetText(FindDeep(screen, "Subtitle"), pVictory
            ? "All " + totalWaves + " waves defeated"
            : "The Headquarters has fallen on wave " + _wave);
        SetText(FindDeep(screen, "XP"), "+" + (_kills * 25) + " XP");
    }

    // A row is an object with a 'Value' and a 'Label' child.
    static void SetRow(Transform screen, string rowName, string value)
    {
        Transform row = FindDeep(screen, rowName);
        if (row == null) { Debug.LogWarning("[LevelController] Result row '" + rowName + "' not found.", screen); return; }
        SetText(FindDeep(row, "Value"), value);
    }

    static void SetText(Transform target, string text)
    {
        if (target != null && target.TryGetComponent(out TMP_Text tmp)) tmp.text = text;
    }

    static Transform FindDeep(Transform root, string name)
    {
        foreach (Transform child in root)
        {
            if (child.name == name) return child;
            Transform found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }

    // --- Banner ----------------------------------------------------------------------------------

    void BuildBanner()
    {
        GameObject canvasObject = new GameObject("WaveBannerCanvas", typeof(Canvas), typeof(CanvasGroup));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        _bannerGroup = canvasObject.GetComponent<CanvasGroup>();
        _bannerGroup.alpha = 0f;
        _bannerGroup.blocksRaycasts = false;
        _bannerGroup.interactable = false;

        // dark strip behind the text so it stays readable on any background
        GameObject strip = new GameObject("Strip", typeof(RectTransform), typeof(Image));
        strip.transform.SetParent(canvasObject.transform, false);
        Image stripImage = strip.GetComponent<Image>();
        stripImage.color = new Color(0.08f, 0.05f, 0.03f, bannerBackground);
        stripImage.raycastTarget = false;
        RectTransform stripRect = stripImage.rectTransform;
        stripRect.anchorMin = new Vector2(0f, 0.58f);
        stripRect.anchorMax = new Vector2(1f, 0.80f);
        stripRect.offsetMin = Vector2.zero;
        stripRect.offsetMax = Vector2.zero;

        _bannerTitle = MakeText(canvasObject.transform, "Title", 60, 0.73f, bannerFont);
        _bannerSub = MakeText(canvasObject.transform, "Sub", 30, 0.64f, bannerFont);
    }

    static TMP_Text MakeText(Transform parent, string name, float size, float anchorY, TMP_FontAsset font)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.enableWordWrapping = true;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.outlineWidth = 0.35f;
        text.outlineColor = new Color32(0, 0, 0, 255);
        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0f, anchorY);
        rect.anchorMax = new Vector2(1f, anchorY);
        rect.sizeDelta = new Vector2(0f, 80f);
        rect.anchoredPosition = Vector2.zero;
        return text;
    }

    void ShowBanner(bool pBoss, Dictionary<string, int> pCounts)
    {
        string title = pBoss ? "FINAL WAVE - BOSS INCOMING!" : "Wave " + _wave + " / " + totalWaves;
        Color color = pBoss ? new Color(1f, 0.3f, 0.25f) : new Color(1f, 0.85f, 0.4f);

        List<string> parts = new();
        foreach (var pair in pCounts) parts.Add(pair.Value + "x " + pair.Key);
        if (pBoss) parts.Add("1x " + bossPrefab.name + " (boss)");
        string sub = "Incoming: " + string.Join(", ", parts);
        if (_pendingBonus > 0) { sub += "\nEarly start bonus: +" + _pendingBonus + " Stone"; _pendingBonus = 0; }

        if (_bannerRoutine != null) StopCoroutine(_bannerRoutine);
        _bannerRoutine = StartCoroutine(BannerRoutine(title, sub, color));
    }

    IEnumerator BannerRoutine(string pTitle, string pSub, Color pColor)
    {
        _bannerTitle.text = pTitle;
        _bannerTitle.color = pColor;
        _bannerSub.text = pSub;
        _bannerSub.color = Color.white;

        for (float t = 0f; t < bannerSeconds; t += Time.unscaledDeltaTime)
        {
            float fadeIn = Mathf.Clamp01(t / 0.3f);
            float fadeOut = Mathf.Clamp01((bannerSeconds - t) / 0.8f);
            _bannerGroup.alpha = Mathf.Min(fadeIn, fadeOut);
            yield return null;
        }
        _bannerGroup.alpha = 0f;
    }

    // --- End of the level ----------------------------------------------------------------------

    void End(bool pVictory)
    {
        _ended = true;
        FillResults(pVictory);

        if (pVictory) winLose.ShowVictory();
        else winLose.ShowDefeat();

        // Make sure the result and any new achievements are on disk.
        if (StatsTracker.Instance != null) StatsTracker.Instance.SaveNow();
    }

    // --- HUD -----------------------------------------------------------------------------------
    // InGameUI keeps its wave numbers in private serialized fields and refreshes its texts every
    // frame, so writing the fields is all that is needed.

    void FindFields()
    {
        _unitHealthField = typeof(UnitHealth).GetField("health", PrivateInstance);
        if (_unitHealthField == null)
            Debug.LogWarning("[LevelController] UnitHealth no longer has a 'health' field. Waves will not get tougher.", this);

        if (hud == null) return;

        System.Type type = typeof(InGameUI);
        _waveField = type.GetField("wave", PrivateInstance);
        _maxWaveField = type.GetField("maxWave", PrivateInstance);
        _enemiesField = type.GetField("enemies", PrivateInstance);

        if (_waveField == null || _maxWaveField == null || _enemiesField == null)
        {
            Debug.LogWarning("[LevelController] InGameUI no longer has 'wave', 'maxWave' and 'enemies'. HUD not updated.", this);
            hud = null;
        }
    }

    void PushToHud()
    {
        if (hud == null) return;

        _waveField.SetValue(hud, _wave);
        _maxWaveField.SetValue(hud, totalWaves);
        _enemiesField.SetValue(hud, AliveCount);
    }

    void ShowCountdown()
    {
        if (countdownText == null) return;

        if (_waiting)
            countdownText.text = (_wave == 0 ? "First wave in " : "Next wave in ") + Mathf.CeilToInt(_secondsLeft) + "s";
        else
            countdownText.text = _allWavesSpawned ? "Final wave: defeat every enemy!" : "";
    }
}