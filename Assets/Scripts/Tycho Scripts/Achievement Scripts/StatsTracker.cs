using System;
using UnityEngine;

[RequireComponent(typeof(SaveSystem))]
public class StatsTracker : MonoBehaviour
{
    public static StatsTracker Instance { get; private set; }

    [SerializeField] private SaveSystem saveSystem;

    public PlayerData Data { get; private set; }

    public event Action<string> OnStatChanged;

    private bool _saveRequested;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStats() => Instance = null;

    private void Awake()
    {
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

    public void AddStat(string pKey, long pAmount)
    {
        if (pAmount == 0) return;

        Data.AddStat(pKey, pAmount);
        OnStatChanged?.Invoke(pKey);
    }

    public void SetStat(string pKey, long pValue)
    {
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
        if (Data == null) return;

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
