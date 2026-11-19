using System;
using System.Collections.Generic;

// Unity's JsonUtility cannot serialize a Dictionary, so stats and achievements are lists of entries.
// They are searched linearly: fine for a few dozen items.

[Serializable]
public class StatEntry
{
    public string key;
    public long value;
}

[Serializable]
public class AchievementProgress
{
    public string id;
    public bool unlocked;
    public string unlockedAtUtc; // ISO 8601 round-trip string: see AchievementManager.
}

/// <summary>Everything that is saved about the player. Plain data only; logic lives in the managers.</summary>
[Serializable]
public class PlayerData
{
    /// <summary>Bump when the layout is changed, so old saves can be migrated on load.</summary>
    public int saveVersion = 1;

    public List<StatEntry> stats = new();
    public List<AchievementProgress> achievements = new();
    public SettingsData settings = new();
    public SkillTreeData skillTree = new();

    public long GetStat(string pKey) => FindStat(pKey)?.value ?? 0;

    public void SetStat(string pKey, long pValue)
    {
        StatEntry entry = FindStat(pKey);
        if (entry == null) stats.Add(entry = new StatEntry { key = pKey });

        entry.value = pValue;
    }

    public AchievementProgress FindAchievement(string pId)
    {
        foreach (AchievementProgress progress in achievements)
            if (progress.id == pId) return progress;
        
        return null;
    }

    public AchievementProgress GetOrCreateAchievement(string pId)
    {
        AchievementProgress progress = FindAchievement(pId);
        if (progress == null) achievements.Add(progress = new AchievementProgress { id = pId });

        return progress;
    }

    private StatEntry FindStat(string pKey)
    {
        foreach (StatEntry entry in stats)
            if (entry.key == pKey) return entry;

        return null;
    }
}