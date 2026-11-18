using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public int waveBudget;
    public int currentWave;
    public int budgetIncreaseModifier;
    bool isBossWave;
    WaveBuilder waveBuilder;

    private void Start()
    {
        waveBuilder = GetComponent<WaveBuilder>();
        //StartWave();
    }

    public void StartWave()
    {
        waveBuilder.CreateWave(waveBudget);
    }

    public void EndWave()
    {
        currentWave++;
        waveBudget += budgetIncreaseModifier;
    }
}