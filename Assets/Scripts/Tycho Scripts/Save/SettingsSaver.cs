using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Saves and restores the settings WITHOUT changing SettingsMenuUI. Put it on the persistent GameSystems
/// object, next to StatsTracker.
///
/// At game start it restores everything the game itself uses: keybinds and camera speed (static classes),
/// vsync, frame rate and UI scale. This works without the menu, which only exists while it is open.
/// While the menu is open it watches it (through <see cref="SettingsMenuBridge"/>): on opening it puts the saved
/// values into the menu, and every change is written to the save.
/// Resolution and window mode are not re-applied here, Unity remembers those itself in builds.
/// </summary>
[DefaultExecutionOrder(100)] // After StatsTracker.Awake (order 0), which loads the save file.
public class SettingsSaver : MonoBehaviour
{
    private const float CheckInterval = 0.1f;

    private static SettingsSaver _instance;

    private SettingsMenuUI _menu;       // The menu being watched; null while none exists.
    private bool _menuCompatible;
    private bool _menuRestored;         // Saved values were put into _menu (once per menu instance).
    private string _lastJson;           // Last saved state, to detect changes.
    private float _timer;

    private static SettingsData Data => StatsTracker.Instance.Data.settings ??= new SettingsData();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => _instance = null;

    private void Awake()
    {
        // A second GameSystems object (from reloading the first scene) must not run a second saver.
        if (_instance != null && _instance != this)
        {
            enabled = false;
            return;
        }

        if (StatsTracker.Instance == null)
        {
            Debug.LogError("[SettingsSaver] No StatsTracker found. Settings will not be saved.", this);
            enabled = false;
            return;
        }

        _instance = this;
        _menuCompatible = SettingsMenuBridge.IsCompatible();
        Debug.Log($"[SettingsSaver] Running. Saved settings in file: {Data.hasSaved}. Menu members found: {_menuCompatible}.", this);

        // In Awake, so keybinds and camera speed are right before the menu or any gameplay script starts.
        if (Data.hasSaved) RestoreGameSettings(Data);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        ApplyUiScale();
        FindMenu();
    }

    private void OnDestroy()
    {
        if (_instance != this) return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        _instance = null;
    }

    private void Update()
    {
        _timer += Time.unscaledDeltaTime;
        if (_timer < CheckInterval) return;

        _timer = 0f;
        if (!_menuCompatible || _menu == null || !SettingsMenuBridge.IsReady(_menu)) return;

        if (!_menuRestored)
        {
            if (Data.hasSaved) SettingsMenuBridge.Restore(_menu, Data);
            Debug.Log($"[SettingsSaver] Menu ready. Restored saved values: {Data.hasSaved}.");

            // Baseline: what is shown now counts as saved, so nothing is written until the player changes something.
            _lastJson = JsonUtility.ToJson(Snapshot());
            _menuRestored = true;
            return;
        }

        SaveIfChanged();
    }

    private void OnApplicationPause(bool pPaused)
    {
        if (pPaused) SaveIfChanged(true);
    }

    private void OnApplicationQuit() => SaveIfChanged(true);

    private void OnSceneLoaded(Scene pScene, LoadSceneMode pMode)
    {
        ApplyUiScale(); // Every scene has its own canvases, so the scale is applied again.
        FindMenu();     // The menu may be an additive scene or a panel in the new scene.
    }

    private void FindMenu()
    {
        SettingsMenuUI found = FindFirstObjectByType<SettingsMenuUI>(FindObjectsInactive.Include);
        if (found == _menu) return;

        _menu = found;
        _menuRestored = false;
        Debug.Log($"[SettingsSaver] Settings menu {(found != null ? "found" : "gone")}.");
    }

    private static void RestoreGameSettings(SettingsData pData)
    {
        // Start from the defaults, so keybinds added since the save still get their default key.
        Key[] keys = (Key[])Keybinds.defaultKeys.Clone();
        for (int i = 0; i < Math.Min(pData.keys.Length, keys.Length); i++)
            if (Enum.TryParse(pData.keys[i], out Key key)) keys[i] = key;
        Keybinds.keys = keys;

        CameraSettings.moveSpeed = pData.cameraMoveSpeed;
        CameraSettings.zoomSpeed = pData.cameraZoomSpeed;

        QualitySettings.vSyncCount = pData.vSync ? 1 : 0;
        Application.targetFrameRate = pData.targetFrameRate;
    }

    // Mirrors what the menu's Apply does to the canvases. Skipped at 100%, so players who never touched
    // the slider keep the scaling every canvas was designed with.
    private static void ApplyUiScale()
    {
        SettingsData data = Data;
        if (!data.hasSaved || data.uiScale <= 0f || Mathf.Approximately(data.uiScale, 1f)) return;

        foreach (CanvasScaler scaler in FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            scaler.referenceResolution = data.uiReferenceResolution / data.uiScale;
    }

    // Everything that is saved, read from where it lives right now. vsync and frame rate come from the engine,
    // so they are the APPLIED values, like the menu itself only changes them when Apply is pressed.
    private SettingsData Snapshot()
    {
        var data = new SettingsData
        {
            hasSaved = true,
            keys = Array.ConvertAll(Keybinds.keys, key => key.ToString()),
            cameraMoveSpeed = CameraSettings.moveSpeed,
            cameraZoomSpeed = CameraSettings.zoomSpeed,
            vSync = QualitySettings.vSyncCount > 0,
            targetFrameRate = Application.targetFrameRate
        };

        SettingsMenuBridge.Capture(_menu, data);
        return data;
    }

    /// <param name="pImmediate">Write the file right now instead of at the end of the frame (needed on quit/pause).</param>
    private void SaveIfChanged(bool pImmediate = false)
    {
        if (_instance != this || !_menuCompatible || _menu == null || !_menuRestored) return;

        SettingsData snapshot = Snapshot();
        string json = JsonUtility.ToJson(snapshot);
        if (json == _lastJson) return;

        _lastJson = json;
        StatsTracker.Instance.Data.settings = snapshot;
        Debug.Log("[SettingsSaver] Settings changed, saving.");

        if (pImmediate) StatsTracker.Instance.SaveNow();
        else StatsTracker.Instance.RequestSave();
    }
}