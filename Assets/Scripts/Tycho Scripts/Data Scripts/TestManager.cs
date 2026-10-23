using UnityEngine;
using UnityEngine.InputSystem;

public class TestManager : MonoBehaviour
{
    private void Start()
    {
        Debug.Log($"[TestManager] Space = +50 wood_gathered | V = +1 vikings_killed | S = save now");
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null || StatsTracker.Instance == null) return;

        if (kb.spaceKey.wasPressedThisFrame) StatsTracker.Instance.AddStat("wood_gathered", 50);
        if (kb.vKey.wasPressedThisFrame) StatsTracker.Instance.AddStat("vikings_killed", 1);
        if (kb.sKey.wasPressedThisFrame) StatsTracker.Instance.SaveNow();
    }

    [ContextMenu("Test: Print profile JSON in console.")]
    public void TestPrintProfile()
    {
        if (StatsTracker.Instance == null) return;
        Debug.Log($"\n{JsonUtility.ToJson(StatsTracker.Instance.Data, true)}");
    }
}
