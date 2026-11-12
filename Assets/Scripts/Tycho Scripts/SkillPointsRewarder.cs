using System;
using UnityEngine;

public class SkillPointsRewarder : MonoBehaviour
{
    [SerializeField, Min(1)] private int pointsPerLevel = 1;

    private void Start()
    {
        if (XPManager.Instance == null)
        {
            Debug.LogError("[SkillPointRewarder] No XPManager found in this scene. No skill points will be awarded.", this);
            enabled = false;
            return;
        }

        XPManager.Instance.OnLevelUp += HandleLevelUp;
    }

    private void OnDestroy()
    {
        if (XPManager.Instance != null) XPManager.Instance.OnLevelUp -= HandleLevelUp;
    }

    private void HandleLevelUp(int pNewLevel) => SkillPoints.Award(pointsPerLevel);

}
