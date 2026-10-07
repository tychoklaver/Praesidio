using UnityEngine;

[CreateAssetMenu(menuName = "Jobs/JobData")]
public class JobData : ScriptableObject
{
    public string jobName;
    public float interval;
    public ResourceType resource;
    public int amountPerTick;
}