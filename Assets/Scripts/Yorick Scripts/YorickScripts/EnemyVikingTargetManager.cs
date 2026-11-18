using UnityEngine;

public class EnemyVikingTargetManager : MonoBehaviour
{
    public static EnemyVikingTargetManager Instance { get; private set; }

    [SerializeField] float refreshInterval = 1f;

    [SerializeField] private DBTest[] defenseBuildings = new DBTest[0];

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
        defenseBuildings = FindObjectsByType<DBTest>(FindObjectsSortMode.None);
    }

    public Transform GetClosest(Vector2 position)
    {
        Transform closest = null;
        float closestDistance = float.MaxValue;

        foreach (DBTest building in defenseBuildings)
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
