using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [SerializeField] float cellSize = 1f;
    [SerializeField] Vector2Int gridSize = new Vector2Int(40, 24);
    [SerializeField] bool drawGizmos = true;

    readonly HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();

    public float CellSize => cellSize;
    public Vector2Int GridSize => gridSize;

    void Awake() => Instance = this;

    public Vector2Int WorldToCell(Vector3 pos)
    {
        Vector3 local = pos - transform.position;
        return new Vector2Int(Mathf.FloorToInt(local.x / cellSize), Mathf.FloorToInt(local.y / cellSize));
    }

    public Vector3 CellToWorld(Vector2Int cell, Vector2Int size)
    {
        return transform.position + new Vector3(
            (cell.x + size.x * 0.5f) * cellSize,
            (cell.y + size.y * 0.5f) * cellSize, 0f);
    }

    public Vector3 SnapToGrid(Vector3 pos) => CellToWorld(WorldToCell(pos), Vector2Int.one);

    public bool InBounds(Vector2Int cell) =>
        cell.x >= 0 && cell.y >= 0 && cell.x < gridSize.x && cell.y < gridSize.y;

    public bool IsOccupied(Vector2Int cell) => !InBounds(cell) || occupied.Contains(cell);

    public bool IsAreaFree(Vector2Int origin, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
                if (IsOccupied(origin + new Vector2Int(x, y))) return false;
        return true;
    }

    public void Occupy(Vector2Int origin, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
                occupied.Add(origin + new Vector2Int(x, y));
    }

    public void Free(Vector2Int origin, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
                occupied.Remove(origin + new Vector2Int(x, y));
    }

    [SerializeField] Color lineColor = new Color(1f, 1f, 1f, 0.5f);
    public Color LineColor => lineColor;
    public Vector3 Origin => transform.position;
    
    public IEnumerable<(Vector3 from, Vector3 to)> GetGridLines()
    {
        Vector3 o = transform.position;
        for (int x = 0; x <= gridSize.x; x++)
            yield return (o + new Vector3(x * cellSize, 0, 0), o + new Vector3(x * cellSize, gridSize.y * cellSize, 0));
        for (int y = 0; y <= gridSize.y; y++)
            yield return (o + new Vector3(0, y * cellSize, 0), o + new Vector3(gridSize.x * cellSize, y * cellSize, 0));
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        Gizmos.color = LineColor;
        foreach (var (from, to) in GetGridLines())
            Gizmos.DrawLine(from, to);
    }
}