using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(BuildingHealth))]
public class HouseBuilding : Building
{
    [SerializeField] private Worker worker;

    [SerializeField] private float interval;
    [SerializeField] private float spawnChance;

    private int maxSpawns;
    private float time;
    private int spawns;

    private void Awake()
    {
        maxSpawns = Random.Range(1, 5);
    }

    void FixedUpdate()
    {
        if (!IsComplete)
            return;
        
        if  (spawns > maxSpawns)
            return;
        
        time -= Time.deltaTime;

        if (time <= 0)
        {
            if (Random.Range(0, 100) <= spawnChance)
                SpawnWorker();

            time = interval;
        }
    }

    private void SpawnWorker()
    {
        if (worker == null)
            return;
        
        if (spawns < maxSpawns)
        {
            Worker w = Instantiate(worker, this.transform.position, Quaternion.identity);
            WorkerManager.Instance.workers.Add(w);
            spawns++;
        }
    }
}