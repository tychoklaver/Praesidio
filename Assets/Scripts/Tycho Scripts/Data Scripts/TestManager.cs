using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Temporary test harness. Keys 1-9 and 0 each push one achievement's stat to its target value,
/// so every achievement can be unlocked on demand. Disables itself in release builds; remove before shipping.
/// </summary>
public class TestManager : MonoBehaviour
{
    private readonly struct TestAction
    {
        public readonly Key key;
        public readonly string achievement;
        public readonly string statKey;
        public readonly long amount;
        public readonly bool setInsteadOfAdd;

        public TestAction(Key pKey, string pAchievement, string pStatKey, long pAmount, bool pSet = false)
        {
            key = pKey;
            achievement = pAchievement;
            statKey = pStatKey;
            amount = pAmount;
            setInsteadOfAdd = pSet;
        }
    }

    // Amounts equal the achievement targets, so one press unlocks it.
    private static readonly TestAction[] Actions =
    {
        new(Key.Digit1, "first_spade",    "workers_assigned",         1),
        new(Key.Digit2, "forester",       "wood_gathered",            500),
        new(Key.Digit3, "hard_as_stone",  "stone_walls_built",        1),
        new(Key.Digit4, "wall_of_sparta", "pass_hold_streak",         3, true), // SetStat: it is a streak.
        new(Key.Digit5, "open_for_trade", "trade_building_activated", 1),
        new(Key.Digit6, "speed_runner",   "manual_wave_starts",       3),
        new(Key.Digit7, "first_victory",  "levels_completed",         1),
        new(Key.Digit8, "viking_slayer",  "vikings_killed",           100),
        new(Key.Digit9, "tower_builder",  "towers_built",             25),
        new(Key.Digit0, "gold_rush",      "gold_gathered",            1000),
    };

    // isDebugBuild is true in the editor and in Development Builds only. A disabled component skips Start/Update.
    private void Awake()
    {
        if (!Debug.isDebugBuild) enabled = false;
    }

    private void Start()
    {
        var sb = new StringBuilder("[TestManager] Controls:\n");
        foreach (TestAction action in Actions)
            sb.AppendLine($"  {action.key.ToString().Replace("Digit", "")} = {action.achievement} ({action.statKey} {(action.setInsteadOfAdd ? "=" : "+")}{action.amount})");

        sb.AppendLine("  P = show test popup | S = save now | R = RESET all stats and achievements");
        Debug.Log(sb.ToString());
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null || StatsTracker.Instance == null) return;

        foreach (TestAction action in Actions)
        {
            if (!kb[action.key].wasPressedThisFrame) continue;

            if (action.setInsteadOfAdd) StatsTracker.Instance.SetStat(action.statKey, action.amount);
            else StatsTracker.Instance.AddStat(action.statKey, action.amount);
        }

        if (kb.pKey.wasPressedThisFrame && AchievementPopup.Instance != null)
            AchievementPopup.Instance.Show("Test Achievement", "This is what an unlocked achievement looks like.", null);

        if (kb.sKey.wasPressedThisFrame) StatsTracker.Instance.SaveNow();

        if (kb.rKey.wasPressedThisFrame) ResetProfile();
    }

    // Wipes stats and unlocks so every achievement can be tested again without deleting the save file.
    // The data is cleared in place without raising events, so an open achievements menu refreshes on the next change.
    private void ResetProfile()
    {
        PlayerData data = StatsTracker.Instance.Data;
        data.stats.Clear();
        data.achievements.Clear();
        StatsTracker.Instance.SaveNow();

        Debug.Log("[TestManager] Profile reset. All achievements are locked again.");
    }

    [ContextMenu("Test: Print profile JSON in console.")]
    public void TestPrintProfile()
    {
        if (StatsTracker.Instance == null) return;
        Debug.Log($"\n{JsonUtility.ToJson(StatsTracker.Instance.Data, true)}");
    }
}
