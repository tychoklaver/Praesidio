using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Temporary test harness for achievements, XP and skill points. Disables itself in release builds.
/// Put it on the persistent GameSystems object so it works in every scene. Remove before shipping.
/// Keys avoid the game's own hotkeys (WASD, H, Space, F1, F2, 1-3).
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
        new(Key.F3,  "first_spade",    "workers_assigned",         1),
        new(Key.F4,  "forester",       "wood_gathered",            500),
        new(Key.F5,  "hard_as_stone",  "stone_walls_built",        1),
        new(Key.F6,  "wall_of_sparta", "pass_hold_streak",         3, true), // SetStat: it is a streak.
        new(Key.F7,  "open_for_trade", "trade_building_activated", 1),
        new(Key.F8,  "speed_runner",   "manual_wave_starts",       3),
        new(Key.F9,  "first_victory",  "levels_completed",         1),
        new(Key.F10, "viking_slayer",  "vikings_killed",           100),
        new(Key.F11, "tower_builder",  "towers_built",             25),
        new(Key.F12, "gold_rush",      "gold_gathered",            1000),
    };

    private const int XpPerPress = 50; // Equals xpForFirstLevelUp's default: one press = one level-up.

    // isDebugBuild is true in the editor and in Development Builds only. A disabled component skips Start/Update.
    private void Awake()
    {
        if (!Debug.isDebugBuild) enabled = false;
    }

    private void Start()
    {
        var sb = new StringBuilder("[TestManager] Controls:\n");
        foreach (TestAction action in Actions)
            sb.AppendLine($"  {action.key} = {action.achievement} ({action.statKey} {(action.setInsteadOfAdd ? "=" : "+")}{action.amount})");

        sb.AppendLine($"  X = add {XpPerPress} XP (level scene only) | K = award 1 skill point directly");
        sb.AppendLine("  P = test popup | U = save now | J = print profile JSON | R = RESET stats, achievements and skill tree");
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

        if (kb.xKey.wasPressedThisFrame) AddXp();
        if (kb.kKey.wasPressedThisFrame) AwardSkillPoint();

        if (kb.pKey.wasPressedThisFrame && AchievementPopup.Instance != null)
            AchievementPopup.Instance.Show("Test Achievement", "This is what an unlocked achievement looks like.", null);

        if (kb.uKey.wasPressedThisFrame) StatsTracker.Instance.SaveNow();
        if (kb.jKey.wasPressedThisFrame) PrintProfile();
        if (kb.rKey.wasPressedThisFrame) ResetProfile();
    }

    // Goes through the real route: XPManager -> OnLevelUp -> SkillPointRewarder -> SkillPoints.Award.
    private void AddXp()
    {
        if (XPManager.Instance == null)
        {
            Debug.LogWarning("[TestManager] No XPManager in this scene. Press X in a level scene.");
            return;
        }

        XPManager.Instance.AddXP(XpPerPress);
        Debug.Log($"[TestManager] Level {XPManager.Instance.Level}, XP {XPManager.Instance.XP}. Saved skill points: {SavedSkillPoints()}");
    }

    private void AwardSkillPoint()
    {
        SkillPoints.Award(1);
        Debug.Log($"[TestManager] Awarded 1 skill point. Saved skill points: {SavedSkillPoints()}");
    }

    private static int SavedSkillPoints() => StatsTracker.Instance.Data.skillTree?.skillPoints ?? 0;

    // Wipes progress so everything can be tested again without deleting the save file.
    // An already open skill tree scene keeps showing old values until it is reloaded.
    private void ResetProfile()
    {
        PlayerData data = StatsTracker.Instance.Data;
        data.stats.Clear();
        data.achievements.Clear();
        data.skillTree = new SkillTreeData();
        StatsTracker.Instance.SaveNow();

        Debug.Log("[TestManager] Profile reset. Reload the skill tree scene to see it.");
    }

    private static void PrintProfile() =>
        Debug.Log($"[TestManager] Profile:\n{JsonUtility.ToJson(StatsTracker.Instance.Data, true)}");
}