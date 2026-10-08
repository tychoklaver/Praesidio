using System.Collections.Generic;
using UnityEngine;

public class ResourceNode : MonoBehaviour
{
    public static readonly List<ResourceNode> All = new List<ResourceNode>();

    public ResourceType type = ResourceType.Wood;

    [Header("Grid")]
    [SerializeField] Vector2Int size = Vector2Int.one;
    [SerializeField] bool snapToGrid = true;

    [Header("Workers")]
    [SerializeField] float extraReach = 0.3f;

    Vector2Int cell;
    bool occupied;
    
    public float Reach
    {
        get
        {
            float cs = GridManager.Instance != null ? GridManager.Instance.CellSize : 1f;
            return Mathf.Max(size.x, size.y) * 0.5f * cs + extraReach;
        }
    }

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    void Start()
    {
        var grid = GridManager.Instance;
        if (grid == null) return;

        cell = grid.WorldToCell(transform.position) - new Vector2Int(size.x / 2, size.y / 2);

        if (!grid.IsAreaFree(cell, size))
            Debug.LogWarning($"{name}: staat (deels) buiten het grid of op een bezette cel.", this);

        if (snapToGrid)
            transform.position = grid.CellToWorld(cell, size);

        grid.Occupy(cell, size);
        occupied = true;
    }

    void OnDestroy()
    {
        if (occupied && GridManager.Instance != null)
            GridManager.Instance.Free(cell, size);
    }

    public static ResourceNode FindNearest(ResourceType type, Vector3 position)
    {
        ResourceNode best = null;
        float bestDist = float.MaxValue;
        foreach (var n in All)
        {
            if (n == null || n.type != type) continue;
            float d = (n.transform.position - position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = n; }
        }
        return best;
    }
}
