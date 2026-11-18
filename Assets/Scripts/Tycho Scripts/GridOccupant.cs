using UnityEngine;

/// <summary>
/// Puts an object on the build grid: snaps it to its cells and marks them as occupied, so the
/// player can't build on top of it (used for the keep). Works like ResourceNode, but without
/// registering as a resource that workers can gather.
/// </summary>
[DefaultExecutionOrder(-50)] // Runs before the ResourceSpawner, so it can't place a tree on this object.
public class GridOccupant : MonoBehaviour
{
    [Tooltip("Size in grid cells. The object is snapped so that it covers exactly these cells.")]
    [SerializeField] Vector2Int size = new(2, 1);

    Vector2Int _cell;
    bool _occupied;

    void Start()
    {
        GridManager grid = GridManager.Instance;
        if (grid == null)
        {
            Debug.LogError("[GridOccupant] No GridManager in the scene.", this);
            return;
        }

        // Same rule as ResourceNode: the cell under the object's centre, shifted by half its size.
        _cell = grid.WorldToCell(transform.position) - new Vector2Int(size.x / 2, size.y / 2);

        if (!grid.IsAreaFree(_cell, size))
            Debug.LogWarning($"[GridOccupant] {name} is partly outside the grid or on an occupied cell.", this);

        transform.position = grid.CellToWorld(_cell, size);
        grid.Occupy(_cell, size);
        _occupied = true;
    }

    void OnDestroy()
    {
        if (_occupied && GridManager.Instance != null) GridManager.Instance.Free(_cell, size);
    }
}