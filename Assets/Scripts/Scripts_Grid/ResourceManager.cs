using System;
using UnityEngine;

public enum ResourceType { Wood, Stone, Iron, Gold, Food }

[Serializable]
public struct ResourceCost
{
    public ResourceType type;
    public int amount;
}

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    public int wood = 100;
    public int stone = 50;
    public int iron = 0;
    public int gold = 0;
    public int food = 0;

    public event Action OnResourcesChanged;

    void Awake() => Instance = this;
    void Start() => OnResourcesChanged?.Invoke();

    public int Get(ResourceType type) => type switch
    {
        ResourceType.Wood => wood,
        ResourceType.Stone => stone,
        ResourceType.Iron => iron,
        ResourceType.Gold => gold,
        ResourceType.Food => food,
        _ => 0
    };

    public bool HasEnough(ResourceCost[] cost)
    {
        foreach (var c in cost)
            if (Get(c.type) < c.amount) return false;
        return true;
    }

    public void Subtract(ResourceCost[] cost)
    {
        foreach (var c in cost) Add(c.type, -c.amount);
    }

    public void Add(ResourceType type, int amount)
    {
        switch (type)
        {
            case ResourceType.Wood: wood += amount; break;
            case ResourceType.Stone: stone += amount; break;
            case ResourceType.Iron: iron += amount; break;
            case ResourceType.Gold: gold += amount; break;
            case ResourceType.Food: food += amount; break;
        }
        OnResourcesChanged?.Invoke();
    }
}