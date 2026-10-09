using System.Collections.Generic;
using UnityEngine;

public class EnemySoldierTargetManager : MonoBehaviour
{
    public static EnemySoldierTargetManager Instance { get; private set; }

    [SerializeField] float refreshInterval = 1f;

    [SerializeField] private DBTest[] buildings = new DBTest[0];

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        InvokeRepeating(nameof(RefreshBuildings), 0f, refreshInterval);
    }

    private void RefreshBuildings()
    {
        buildings = FindObjectsByType<DBTest>(FindObjectsSortMode.None);
    }

    public Transform GetClosest(Vector2 position)
    {
        Transform closest = null;
        float closestDistance = float.MaxValue;

        foreach (DBTest building in buildings)
        {
            if (building == null) continue;

            float distance = Vector2.Distance(position, building.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = building.transform;
            }
        }

        return closest;
    }
}
