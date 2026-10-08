using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerData
{
    public int totalScore = 0;
    public int wavesCompleted = 0;
    public List<string> unlockedAchievements = new();
}