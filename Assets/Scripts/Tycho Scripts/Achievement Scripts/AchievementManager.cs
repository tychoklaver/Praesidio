using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Unlocks achievements when the stat they watch reaches its target, and exposes their state to the UI.
/// Lives on the persistent GameSystems prefab, next to <see cref="StatsTracker"/>.
/// </summary>
public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    [SerializeField] private List<AchievementDefinition> definitions = new();

    /// <summary>Raised once per achievement, at the moment it unlocks.</summary>
    public event Action<AchievementDefinition> OnAchievementUnlocked;

    public IReadOnlyList<AchievementDefinition> Definitions => definitions;

    // Stat key -> achievements watching it. A stat change then only checks the relevant achievements.
    private readonly Dictionary<string, List<AchievementDefinition>> _byStat = new();
    
    // With domain reload disabled (Enter Play Mode Options) statistics survive between runs, so reset them.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStats() => Instance = null;

    private void Awake()
    {
        if (Instance != null && Instance != this) return;

        Instance = this;

        foreach (AchievementDefinition def in definitions)
        {
            // An empty key can never match a stat, and null keys would throw in the dictionary.
            if (def == null || string.IsNullOrEmpty(def.StatKey)) continue;

            if (!_byStat.TryGetValue(def.StatKey, out var list))
                _byStat[def.StatKey] = list = new List<AchievementDefinition>();

            list.Add(def);
        }
    }

    // Start instead of Awake: StatsTracker.Awake must have loaded the save first.
    private void Start()
    {
        if (Instance != this) return;

        if (StatsTracker.Instance == null)
        {
            Debug.LogError("[AchievementManager] No StatsTracker found. Add the GameSystems prefab to the scene.", this);
            return;
        }

        StatsTracker.Instance.OnStatChanged += HandleStatChanged;

        // Initial sweep: catches targets that were lowered, or reached before this achievement existed.
        foreach (AchievementDefinition def in definitions)
            if (def != null) TryUnlock(def);        
    }

    private void OnDestroy()
    {
        if (Instance == this && StatsTracker.Instance != null)
            StatsTracker.Instance.OnStatChanged -= HandleStatChanged;
    }

    private void HandleStatChanged(string pKey)
    {
        if (!_byStat.TryGetValue(pKey, out var list)) return;

        foreach (AchievementDefinition def in list)
            TryUnlock(def);
    }

    private void TryUnlock(AchievementDefinition pDef)
    {
        PlayerData data = StatsTracker.Instance.Data;
        if (IsUnlocked(pDef) || data.GetStat(pDef.StatKey) < pDef.Target) return;

        AchievementProgress progress = data.GetOrCreateAchievement(pDef.Id);
        progress.unlocked = true;
        // Stored as UTC in the round-trip ("o") format, so it reads back identically on any machine/culture.
        progress.unlockedAtUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

        StatsTracker.Instance.RequestSave();

        Debug.Log($"[AchievementManager] Unlocked: {pDef.Title}");
        OnAchievementUnlocked?.Invoke(pDef);
    }

    private static AchievementProgress GetProgress(AchievementDefinition pDef) =>
        StatsTracker.Instance.Data.FindAchievement(pDef.Id);

    public bool IsUnlocked(AchievementDefinition pDef) => GetProgress(pDef)?.unlocked == true;

    public long GetCurrentValue(AchievementDefinition pDef) => StatsTracker.Instance.GetStat(pDef.StatKey);

    /// <summary>Gives the unlock moment in the player's local time. False if still locked or unreadable.</summary>
    public bool TryGetUnlockedAt(AchievementDefinition pDef, out DateTime pLocalTime)
    {
        pLocalTime = default;
 
        AchievementProgress progress = GetProgress(pDef);
        if (progress is not { unlocked: true}
            || !DateTime.TryParse(progress.unlockedAtUtc, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out DateTime utc))
            return false;
        
        pLocalTime = utc.ToLocalTime();
        return true;
    }
}