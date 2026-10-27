using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
 
/// <summary>
/// The achievements screen: fills a list with one row per achievement, keeps it up to date
/// while the scene is open, and handles going back to the main menu.
/// </summary>
public class AchievementMenuUI : MonoBehaviour
{
    [Header("List")]
    [Tooltip("The Content object of the Scroll View.")]
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private AchievementRowUI rowPrefab;
    [Tooltip("Optional: shows e.g. '3 / 10 unlocked'.")]
    [SerializeField] private TMP_Text summaryText;
 
    [Header("Navigation")]
    [Tooltip("Exact scene name, and the scene must be in the Build Settings.")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
 
    private readonly List<AchievementRowUI> _rows = new();
    private bool _subscribed;
 
    // Start instead of Awake/OnEnable: by now the persistent systems exist.
    private void Start()
    {
        AchievementManager manager = AchievementManager.Instance;
 
        if (manager == null || StatsTracker.Instance == null)
        {
            if (summaryText != null)
                summaryText.text = "Achievement system not found. Add the GameSystems prefab to this scene.";
            return;
        }
 
        foreach (AchievementDefinition def in manager.Definitions)
        {
            if (def == null) continue;
 
            AchievementRowUI row = Instantiate(rowPrefab, contentRoot);
            row.Bind(def, manager);
            _rows.Add(row);
        }
 
        UpdateSummary(manager);
 
        // Keep the list live, e.g. when an achievement unlocks while this scene is open.
        manager.OnAchievementUnlocked += HandleUnlocked;
        StatsTracker.Instance.OnStatChanged += HandleStatChanged;
        _subscribed = true;
    }
 
    private void OnDestroy()
    {
        if (!_subscribed) return;
 
        if (AchievementManager.Instance != null)
            AchievementManager.Instance.OnAchievementUnlocked -= HandleUnlocked;
        if (StatsTracker.Instance != null)
            StatsTracker.Instance.OnStatChanged -= HandleStatChanged;
    }
 
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            BackToMainMenu();
    }
 
    /// <summary>Hook this up to the Back button's OnClick.</summary>
    public void BackToMainMenu() => SceneManager.LoadScene(mainMenuSceneName);
 
    private void HandleUnlocked(AchievementDefinition pDef) => Refresh();
 
    private void HandleStatChanged(string pKey) => Refresh();
 
    private void Refresh()
    {
        AchievementManager manager = AchievementManager.Instance;
        if (manager == null) return;
 
        foreach (AchievementRowUI row in _rows)
            row.Bind(row.Definition, manager);
 
        UpdateSummary(manager);
    }
 
    private void UpdateSummary(AchievementManager pManager)
    {
        if (summaryText == null) return;
 
        int unlocked = 0;
        foreach (AchievementRowUI row in _rows)
            if (pManager.IsUnlocked(row.Definition)) unlocked++;
 
        summaryText.text = $"{unlocked} / {_rows.Count} unlocked";
    }
}