using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Feeds the "towers_built" stat (Tower Builder achievement): every defense tower the player places adds 1.
/// It watches the scene instead of the build code, so no one else's script has to change.
/// Towers that already stand when the scene starts are not counted, and neither is the placement ghost
/// (BuildingGhost disables the Building component on its preview copy).
/// </summary>
public class TowerBuiltTracker : MonoBehaviour
{
    private const string StatKey = "towers_built";
    private const float ScanInterval = 0.25f;

    private readonly HashSet<DefenseBuilding> _seen = new();
    private float _timer;
    private bool _firstScan = true;

    private void Update()
    {
        _timer -= Time.unscaledDeltaTime;
        if (_timer > 0f) return;

        _timer = ScanInterval;
        Scan();
    }

    private void Scan()
    {
        foreach (DefenseBuilding tower in FindObjectsByType<DefenseBuilding>(FindObjectsSortMode.None))
        {
            if (!tower.enabled || !_seen.Add(tower)) continue; // Ghost preview, or already counted.

            if (!_firstScan && StatsTracker.Instance != null)
                StatsTracker.Instance.AddStat(StatKey, 1);
        }

        _firstScan = false;
    }
}