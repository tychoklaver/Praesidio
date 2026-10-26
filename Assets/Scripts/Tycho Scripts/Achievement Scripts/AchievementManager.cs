using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }
    [SerializeField] private List<AchievementDefinition> definitions = new();

    public event Action<AchievementDefinition> OnAchievementUnlocked;
    public IReadOnlyList<AchievementDefinition> Definitions => definitions;

    private readonly Dictionary<string, List<AchievementDefinition>> _byStat = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStats() => Instance = null;

    private void Awake()
    {
        if (Instance != null && Instance != this) return;

        Instance = this;

        foreach (AchievementDefinition def in definitions)
        {
            if (def == null) continue;

            if (!_byStat.TryGetValue(def.StatKey, out var list))
                _byStat[def.StatKey] = list = new List<AchievementDefinition>();

            list.Add(def);
        }
    }

    private void Start()
    {
        if (Instance != this || StatsTracker.Instance == null) return;

        StatsTracker.Instance.OnStatChanged += HandleStatChanged;

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

        for (int i = 0; i < list.Count; i++)
            TryUnlock(list[i]);
    }

    private void TryUnlock(AchievementDefinition pDef)
    {
        PlayerData data = StatsTracker.Instance.Data;
        
        AchievementProgress existing = data.FindAchievement(pDef.Id);
        if (existing != null && existing.unlocked) return;

        if (data.GetStat(pDef.StatKey) < pDef.Target) return;

        AchievementProgress progress = data.GetOrCreateAchievement(pDef.Id);
        progress.unlocked = true;
        progress.unlockedAtUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

        StatsTracker.Instance.RequestSave();

        Debug.Log($"[AchievementManager] Unlocked: {pDef.Title}");
        OnAchievementUnlocked?.Invoke(pDef);
    }

    public bool IsUnlocked(AchievementDefinition pDef)
    {
        AchievementProgress progress = StatsTracker.Instance.Data.FindAchievement(pDef.Id);
        return progress != null && progress.unlocked;
    }
}
