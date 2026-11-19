/// <summary>
/// Lets any scene give the player skill points. They are stored in the save data and
/// picked up by <see cref="SkillTreeSaver"/> the next time the skill tree opens.
/// </summary>
public static class SkillPoints
{
    /// <summary>Adds points to the saved total. Does nothing for zero or negative amounts.</summary>
    public static void Award(int pAmount)
    {
        if (pAmount <= 0 || StatsTracker.Instance == null) return;

        PlayerData data = StatsTracker.Instance.Data;
        data.skillTree ??= new SkillTreeData();
        data.skillTree.skillPoints += pAmount;

        StatsTracker.Instance.RequestSave();
    }
}