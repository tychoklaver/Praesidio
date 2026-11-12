public static class SkillPoints
{
    public static void Award(int pAmount)
    {
        if (pAmount <= 0 || StatsTracker.Instance == null) return;

        PlayerData data = StatsTracker.Instance.Data;
        data.skillTree ??= new SkillTreeData();
        data.skillTree.skillPoints += pAmount;

        StatsTracker.Instance.RequestSave();
    }
}