using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class ProductionBuilding : Building
{
    [Header("Workers")]
    [SerializeField] JobData job;
    [SerializeField] int maxWorkers = 3;
    
    readonly List<Worker> workers = new List<Worker>();
    
    public JobData Job => job;
    public int WorkerCount => workers.Count;
    public int MaxWorkers => maxWorkers;
    
    public bool CanAddWorker => workers.Count < maxWorkers;
    public bool CanRemoveWorker => workers.Count > 0;

    public bool AddWorker()
    {
        if (workers.Count >= maxWorkers) return false;
        Worker w = WorkerManager.Instance.GetNearestIdle(transform.position);
        if (w == null) return false;
        
        w.AssignTo(this);
        workers.Add(w);
        return true;
    }

    public bool RemoveWorker()
    {
        if (workers.Count == 0) return false;
        
        Worker w = workers[workers.Count - 1];
        workers.RemoveAt(workers.Count - 1);
        if (w != null) w.Unassign();
        return true;
    }

    protected override void OnDestroy()
    {
        foreach (Worker w in workers)
            if  (w != null) w.Unassign();
        base.OnDestroy();
    }
}