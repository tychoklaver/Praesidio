using System;
using UnityEngine;

/// <summary>
/// The single entry point for player statistics: owns the loaded <see cref="PlayerData"/>,
/// announces stat changes (the achievements listen to those) and decides when to save.
/// Lives on the persistent GameSystems prefab and survives scene loads.
/// </summary>
[RequireComponent(typeof(SaveSystem))]
public class StatsTracker : MonoBehaviour
{
    public static StatsTracker Instance { get; private set; }

    [SerializeField] private SaveSystem saveSystem;

    public PlayerData Data { get; private set; }

    /// <summary>Raised with the stat key after a stat actually changed.</summary>
    public event Action<string> OnStatChanged;

    private bool _saveRequested;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStats() => Instance = null;

    private void Awake()
    {
        // A second GameSystems object (e.g. from reloading the first scene) must not replace the first.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (saveSystem == null) saveSystem = GetComponent<SaveSystem>();
        Data = saveSystem.Load();
    }

    public long GetStat(string pKey) => Data.GetStat(pKey);

    /// <summary>Adds to a stat. Stats only count up: a negative amount (e.g. spending wood) is ignored.</summary>
    public void AddStat(string pKey, long pAmount)
    {
        if (pAmount <= 0) return;
        SetStat(pKey, GetStat(pKey) + pAmount);
    }

    /// <summary>Sets a stat, e.g. for streaks. Does nothing (and raises no event) if the value is unchanged.</summary>
    public void SetStat(string pKey, long pValue)
    {
        if (GetStat(pKey) == pValue) return;

        Data.SetStat(pKey, pValue);
        OnStatChanged?.Invoke(pKey);
    }

    /// <summary>
    /// Stats change often, so they are not saved on every change.
    /// Call this at natural moments (wave end, level end); saving also happens on quit and on unlock.
    /// Requests are collected and written once per frame.
    /// </summary>
    public void RequestSave() => _saveRequested = true;

    public void SaveNow()
    {
        if (Data == null) return; // A destroyed duplicate never loaded any data.

        _saveRequested = false;
        saveSystem.Save(Data);
    }

    private void LateUpdate()
    {
        if (_saveRequested) SaveNow();
    }

    private void OnApplicationPause(bool pPaused)
    {
        if (pPaused) SaveNow();
    }

    private void OnApplicationQuit() => SaveNow();
}
