using System;
using UnityEngine;
using Random = System.Random;

public class ResourceSpawner : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        public ResourceNode prefab;
        [Min(0)] public int count = 10;
    }

    [SerializeField] Entry[] entries;

    [Header("Plaatsing")]
    [SerializeField, Min(0)] int spacing = 1;
    [SerializeField] Transform clearZoneCenter;
    [SerializeField, Min(0f)] float clearZoneRadius = 6f;
    [SerializeField, Min(1)] int maxAttemptsPerNode = 50;

    [Header("Random")]
    [SerializeField] bool randomSeed = true;
    [SerializeField] int seed = 12345;

    [Header("Overig")]
    [SerializeField] bool spawnOnStart = true;
    [SerializeField] Transform parent;

    void Start()
    {
        if (spawnOnStart) SpawnAll();
    }

    public void SpawnAll()
    {
        GridManager grid = GridManager.Instance;
        if (grid == null)
        {
            Debug.LogError("ResourceNodeSpawner: geen GridManager in de scene.", this);
            return;
        }

        Random rng = randomSeed ? new Random() : new Random(seed);

        foreach (Entry entry in entries)
        {
            if (entry.prefab == null) continue;

            int spawned = 0;
            for (int i = 0; i < entry.count; i++)
            {
                if (TrySpawn(grid, entry.prefab, rng)) spawned++;
            }

            if (spawned < entry.count)
                Debug.LogWarning($"ResourceNodeSpawner: {entry.prefab.name}: maar {spawned} van {entry.count} geplaatst (geen vrije plek gevonden). Maak de map groter, verlaag het aantal of de spacing.", this);
        }
    }

    bool TrySpawn(GridManager grid, ResourceNode prefab, Random rng)
    {
        Vector2Int size = prefab.Size;
        Vector2Int gridSize = grid.GridSize;

        int maxX = gridSize.x - size.x;
        int maxY = gridSize.y - size.y;
        if (maxX < 0 || maxY < 0) return false;

        for (int attempt = 0; attempt < maxAttemptsPerNode; attempt++)
        {
            Vector2Int cell = new Vector2Int(rng.Next(0, maxX + 1), rng.Next(0, maxY + 1));

            if (!IsValidSpot(grid, cell, size)) continue;

            ResourceNode node = Instantiate(prefab, grid.CellToWorld(cell, size), Quaternion.identity, parent);
            node.PlaceAt(cell);
            return true;
        }
        return false;
    }

    bool IsValidSpot(GridManager grid, Vector2Int cell, Vector2Int size)
    {
        if (clearZoneCenter != null)
        {
            Vector3 center = grid.CellToWorld(cell, size);
            if (Vector2.Distance(center, clearZoneCenter.position) < clearZoneRadius) return false;
        }
        
        for (int x = -spacing; x < size.x + spacing; x++)
        {
            for (int y = -spacing; y < size.y + spacing; y++)
            {
                Vector2Int c = cell + new Vector2Int(x, y);
                bool insideFootprint = x >= 0 && x < size.x && y >= 0 && y < size.y;

                if (insideFootprint)
                {
                    if (grid.IsOccupied(c)) return false;
                }
                else if (grid.InBounds(c) && grid.IsOccupied(c))
                {
                    return false;
                }
            }
        }
        return true;
    }
}