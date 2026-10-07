using System.Collections.Generic;
using UnityEngine;

public class WorkerManager : MonoBehaviour
{
    public static WorkerManager Instance { get; private set; }
    
    [SerializeField] Worker workerPrefab;
    [SerializeField] private int startingWorkers = 5;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float spawnRadius = 1.5f;
    
    public readonly List<Worker> workers = new List<Worker>();

    public int IdleCount
    {
        get
        {
            int n = 0;
            foreach (Worker w in workers)
            {
                if (w != null && w.IsAvailable) n++;
                
            }
            return n;
        }
    }
    
    void Awake() => Instance = this;

    void Start()
    {
        for (int i = 0; i < startingWorkers; i++) SpawnWorkers();
    }

    public Worker SpawnWorkers()
    {
        Vector3 center = spawnPoint != null ? spawnPoint.position : transform.position;
        Vector2 offset = Random.insideUnitCircle * spawnRadius;
        Vector3 pos = new  Vector2(center.x + offset.x, center.y + offset.y);
        
        
        Worker w = Instantiate(workerPrefab, pos , spawnPoint.rotation);
        workers.Add(w);
        return w;
    }

    public Worker GetNearestIdle(Vector3 position)
    {
        Worker best = null;
        float bestDist = float.MaxValue;
        foreach (Worker w in workers)
        {
            if  (w == null || ! w.IsAvailable) continue;
            float d = (w.transform.position - position).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = w;
            }
        }
        return best;
    }
}