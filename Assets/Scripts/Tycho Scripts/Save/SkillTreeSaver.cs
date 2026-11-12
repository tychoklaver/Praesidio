using System.Reflection;
using UnityEngine;

/// <summary>
/// Saves and restores the skill tree (learned skills + unspent skill points) WITHOUT changing SkillTreeUI.
/// Add it to any object in the skill tree scene and assign the SkillTreeUI.
///
/// SkillTreeUI keeps its data in private fields, so this script reaches them through reflection
/// (the C# way to read and write private members). That is why the field names "skills" and
/// "skillPoints" are hard-coded below: if SkillTreeUI ever renames them, this script logs an error
/// and does nothing, instead of failing silently.
///
/// Load: in Awake. All Awakes run before any Start, so SkillTreeUI builds its hexes from the restored data.
/// Points earned in other scenes (SkillPoints.Award) are stored in the save data and picked up here.
/// Save: SkillTreeUI has no events, so the state is checked twice a second; it is only written when it changed.
/// </summary>

[DefaultExecutionOrder(100)] // Runs after StatsTracker.Awake (order 0), which loads the save file.
public class SkillTreeSaver : MonoBehaviour
{
    private const float CHECK_INTERVAL = 0.5f;
    private const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;

    [SerializeField] private SkillTreeUI tree;

    private FieldInfo _pointsField;
    private SkillTreeUI.Skill[] _skills;

    // Cheap change detection: learning or resetting always changes the points or the learned count.
    private (int points, int learned) _lastState;
    private float _timer;

    private void Awake()
    {
        if (tree == null) tree = FindFirstObjectByType<SkillTreeUI>();

        _pointsField = typeof(SkillTreeUI).GetField("skillPoints", PRIVATE_INSTANCE);
        FieldInfo skillsField = typeof(SkillTreeUI).GetField("skills", PRIVATE_INSTANCE);

        if (tree == null || _pointsField == null || skillsField == null || StatsTracker.Instance == null)
        {
            Debug.LogError("[SkillTreeSaver] Needs a SkillTreeUI (with fields 'skills' and 'skillPoints') and a StatsTracker. Skill tree will not be saved.", this);
            enabled = false;
            return;
        } 

        _skills = (SkillTreeUI.Skill[])skillsField.GetValue(tree);
        Load();
    }

    private void Update()
    {
        _timer += Time.unscaledDeltaTime; // Unscaled: the tree must also save while the game is paused.
        if (_timer < CHECK_INTERVAL) return;

        _timer = 0f;
        SaveIfChanged();
    }

    // Catches changes from the last half second when leaving the scene or the game.
    private void OnDestroy() => SaveIfChanged();

    private void OnApplicationPause(bool pPaused)
    {
        if (pPaused) SaveIfChanged(true);
    }

    private void OnApplicationQuit() => SaveIfChanged(true);

    private void Load()
    {
        PlayerData playerData = StatsTracker.Instance.Data;
        playerData.skillTree ??= new SkillTreeData();
        SkillTreeData saved = playerData.skillTree;

        _pointsField.SetValue(tree, saved.skillPoints);

        if (saved.hasSaved)
        {
            foreach (SkillTreeUI.Skill skill in _skills)
                skill.learned = saved.learned.Contains(skill.name);
        }

        _lastState = ReadState();
    }

    private (int points, int learned) ReadState()
    {
        int learned = 0;
        foreach (SkillTreeUI.Skill skill in _skills)
            if (skill.learned) learned++;

        return ((int)_pointsField.GetValue(tree), learned);
    }
    
    private void SaveIfChanged(bool pImmediate = false)
    {
        if (_skills == null || StatsTracker.Instance == null) return;

        (int points, int learned) state = ReadState();
        if (state == _lastState) return;

        Write(state, pImmediate);
    }

    private void Write((int points, int learned) pState, bool pImmediate)
    {
        _lastState = pState;

        SkillTreeData data = StatsTracker.Instance.Data.skillTree;
        data.hasSaved = true;
        data.skillPoints = pState.points;
        data.learned.Clear();
        foreach (SkillTreeUI.Skill skill in _skills)
            if (skill.learned) data.learned.Add(skill.name);

        if (pImmediate) StatsTracker.Instance.SaveNow();
        else StatsTracker.Instance.RequestSave();
    }
}
