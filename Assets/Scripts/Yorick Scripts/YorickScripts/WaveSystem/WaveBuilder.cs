using UnityEngine;
using System.Collections.Generic;

public class WaveBuilder : MonoBehaviour
{
    [System.Serializable]
    public struct UnitEntry
    {
        public UnitAIBase prefab;
        public int budgetCost;
    }

    [SerializeField] UnitEntry[] availableUnits;

    WaveFormations waveFormations;
    readonly List<UnitAIBase> spawnedEnemies = new List<UnitAIBase>();

    private void Start()
    {
        waveFormations = GetComponent<WaveFormations>();
    }

    public void CreateWave(int budget)
    {
        spawnedEnemies.Clear();

        if (!waveFormations.TryGetRandomFormation(budget, out var formation))
        {
            Debug.LogWarning("Geen formatie past binnen het budget: " + budget);
            return;
        }

        SpawnEnemiesInFormation(formation, budget);
    }

    public void SpawnEnemiesInFormation(WaveFormations.FormationSettings formation, int budget)
    {
        int remaining = budget;

        // Stap 1: eerst 1 enemy op elk spawn point
        foreach (Transform spawnPoint in formation.spawnPositions)
        {
            if (!TryPickUnit(remaining, out UnitEntry unit))
                break; // budget te laag voor welke unit dan ook

            Spawn(unit, spawnPoint);
            remaining -= unit.budgetCost;
        }

        // Stap 2: resterend budget besteden aan extra enemies op willekeurige spawn points
        while (TryPickUnit(remaining, out UnitEntry unit))
        {
            Transform spawnPoint = formation.spawnPositions[Random.Range(0, formation.spawnPositions.Length)];
            Spawn(unit, spawnPoint);
            remaining -= unit.budgetCost;
        }
    }

    // Kiest een willekeurige unit die nog binnen het resterende budget past
    bool TryPickUnit(int remainingBudget, out UnitEntry unit)
    {
        var affordable = new List<UnitEntry>();
        foreach (var u in availableUnits)
            if (u.budgetCost > 0 && u.budgetCost <= remainingBudget)
                affordable.Add(u);

        if (affordable.Count == 0)
        {
            unit = default;
            return false;
        }

        unit = affordable[Random.Range(0, affordable.Count)];
        return true;
    }

    void Spawn(UnitEntry unit, Transform spawnPoint)
    {
        UnitAIBase enemy = Instantiate(unit.prefab, spawnPoint.position, spawnPoint.rotation);
        spawnedEnemies.Add(enemy);
    }

    public List<UnitAIBase> GetSpawnedEnemies()
    {
        return spawnedEnemies;
    }
}