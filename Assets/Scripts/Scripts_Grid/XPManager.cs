using System;
using UnityEngine;

public class XPManager : MonoBehaviour
{
    public static XPManager Instance { get; private set; }
    
    [Header("Levels")]
    [SerializeField, Min(1)] int maxLevel = 5;
    [SerializeField, Min(1)] int xpForFirstLevelUp = 50;
    [SerializeField] float xpGrowth = 1.5f;
    
    public int Level { get; private set; } = 1;
    public int XP { get; private set; }
    public bool IsMaxLevel => Level >= maxLevel;
    
    public int XPToNextLevel =>
        IsMaxLevel ? 0 : Mathf.RoundToInt(xpForFirstLevelUp * Mathf.Pow(xpGrowth, Level - 1));
    
    public float Progress => IsMaxLevel ? 1f : (float)XP / XPToNextLevel;
    

    public event Action<int, int> OnXPChanged;
    public event Action<int> OnLevelUp;

    private void Awake() => Instance = this;

    public void AddXP(int amount)
    {
        if (amount <= 0 || IsMaxLevel) return;

        XP += amount;
        while (!IsMaxLevel && XP >= XPToNextLevel)
        {
            XP -= XPToNextLevel;
            Level++;
            OnLevelUp?.Invoke(Level);
        }
        if (IsMaxLevel) XP = 0;

        OnXPChanged?.Invoke(XP, XPToNextLevel);
    }
}