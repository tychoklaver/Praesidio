using System;
using System.Collections.Generic;
using UnityEngine;

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
    public string unlockedAtUtc;
}

[Serializable]
public class PlayerData
{
    public int saveVersion = 1;

    public List<StatEntry> stats = new();
    public List<AchievementProgress> achievements = new();

    public long GetStat(string pKey)
    {
        for (int i = 0; i < stats.Count; i++)
            if(stats[i].key == pKey) return stats[i].value;
        
        return 0;
    }

    public void SetStat(string pKey, long pValue)
    {
        for (int i = 0; i < stats.Count; i++)
        {
            if (stats[i].key != pKey) continue;
            stats[i].value = pValue;
            return;
        }

        stats.Add(new StatEntry { key = pKey, value = pValue });
    }

    public void AddStat(string pKey, long pAmount) => SetStat(pKey, GetStat(pKey) + pAmount);

    public AchievementProgress FindAchievement(string pId)
    {
        for (int i = 0; i < achievements.Count; i++)
            if (achievements[i].id == pId) return achievements[i];

        return null;
    }

    public AchievementProgress GetOrCreateAchievement(string pId)
    {
        AchievementProgress progress = FindAchievement(pId);
        if (progress != null) return progress;

        progress = new AchievementProgress { id = pId };
        achievements.Add(progress);
        return progress;
    }
}