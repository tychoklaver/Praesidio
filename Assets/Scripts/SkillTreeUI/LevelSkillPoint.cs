using UnityEngine;

public class LevelSkillPoint : MonoBehaviour
{
    XPManager xpManager;

    int oldLevel = -1;

    void Start()
    {
        xpManager = XPManager.Instance;
    }

    void Update()
    {
        if (xpManager.Level > oldLevel)
        {
            SkillPoints.Award(1);
        }
        oldLevel = xpManager.Level;
    }
}
