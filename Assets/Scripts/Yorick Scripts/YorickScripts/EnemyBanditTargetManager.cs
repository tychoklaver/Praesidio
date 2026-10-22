using UnityEngine;

public class EnemyBanditTargetManager : MonoBehaviour
{
    public static EnemyBanditTargetManager Instance { get; private set; }

    [SerializeField] float refreshInterval = 1f;

    [SerializeField] private PVTest[] playerVillagers = new PVTest[0];

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
        playerVillagers = FindObjectsByType<PVTest>(FindObjectsSortMode.None);
    }

    public Transform GetClosest(Vector2 position)
    {
        Transform closest = null;
        float closestDistance = float.MaxValue;

        foreach (PVTest playerVillager in playerVillagers)
        {
            if (playerVillager == null) continue;

            float distance = Vector2.Distance(position, playerVillager.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = playerVillager.transform;
            }
        }

        return closest;
    }
}
