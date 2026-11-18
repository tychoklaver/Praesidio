using UnityEngine;

public class WaveFormations : MonoBehaviour
{
    [System.Serializable]
    public struct FormationSettings
    {
        public string formationName;
        public int budgetNeeded;
        public Transform[] spawnPositions;
    }

    public FormationSettings[] formationSettings;

    public FormationSettings RandomWaveFormation()
    {
        int index = Random.Range(0, formationSettings.Length);
        return formationSettings[index];
    }

    // Optioneel: alleen formaties kiezen die binnen het budget passen
    public bool TryGetRandomFormation(int budget, out FormationSettings formation)
    {
        var valid = new System.Collections.Generic.List<FormationSettings>();
        foreach (var f in formationSettings)
            if (f.budgetNeeded <= budget) valid.Add(f);

        if (valid.Count == 0)
        {
            formation = default;
            return false;
        }

        formation = valid[Random.Range(0, valid.Count)];
        return true;
    }
}