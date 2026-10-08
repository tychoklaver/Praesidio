using System.Collections.Generic;
using UnityEngine;

public class FlowfieldManager : MonoBehaviour
{
    public static FlowfieldManager Instance { get; private set; }

    // Voeg hier later gewoon meer doelen aan toe
    public enum TargetType { DefenseBuilding, EnemyTarget }

    public bool showAllPaths;

    [Header("Grid Settings")]
    public Vector3 origin = Vector3.zero;
    public int width = 50;
    public int height = 50;
    public float cellSize = 1f;
    public float recalculateInterval = 0.5f;

    [System.Serializable]
    public struct LayerCost
    {
        public LayerMask layer;
        public byte cost;//255 is een tile waar je niet doorheen kunt.
    }

    [Header("Cost Settings")]
    public LayerCost[] layerCosts;

    const byte Impassable = 255;

    [SerializeField] byte[] costField;

    // Per TargetType een eigen veld (index = (int)TargetType)
    int[][] distance;
    Vector2[][] flow;
    Transform[][] goal;
    int typeCount;

    static readonly Vector2Int[] Dirs =
    {
        new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
        new(1, 1), new(-1, 1), new(1, -1), new(-1, -1)
    };

    void Awake()
    {
        Instance = this;

        typeCount = System.Enum.GetValues(typeof(TargetType)).Length;
        costField = new byte[width * height];
        distance = new int[typeCount][];
        flow = new Vector2[typeCount][];
        goal = new Transform[typeCount][];

        for (int t = 0; t < typeCount; t++)
        {
            distance[t] = new int[width * height];
            flow[t] = new Vector2[width * height];
            goal[t] = new Transform[width * height];
        }
    }

    void Start()
    {
        InvokeRepeating(nameof(Rebuild), 0f, recalculateInterval);
    }

    int Idx(int x, int y) => y * width + x;
    bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < width && y < height;

    Vector3 CellToWorld(int x, int y) => origin + new Vector3(x * cellSize, y * cellSize, 0f);

    Vector2Int WorldToCell(Vector3 pos) => new(
        Mathf.RoundToInt((pos.x - origin.x) / cellSize),
        Mathf.RoundToInt((pos.y - origin.y) / cellSize));

    void Rebuild()
    {
        BuildCosts(); // één keer, geldt voor alle doelen

        for (int t = 0; t < typeCount; t++)
            BuildFlow((TargetType)t);
    }

    bool CanStep(int x, int y, Vector2Int d)
    {
        int nx = x + d.x, ny = y + d.y;
        if (!InBounds(nx, ny) || costField[Idx(nx, ny)] == Impassable) return false;

        if (d.x != 0 && d.y != 0)
            if (costField[Idx(x + d.x, y)] == Impassable || costField[Idx(x, y + d.y)] == Impassable)
                return false;

        return true;
    }

    // ---------- Publiek (gebruikt door de units) ----------
    // type is standaard DefenseBuilding, dus bestaande enemy-code werkt ongewijzigd.

    public bool TryGetNextPoint(Vector3 worldPos, out Vector3 point, TargetType type = TargetType.DefenseBuilding)
    {
        point = worldPos;
        Vector2Int c = WorldToCell(worldPos);
        if (!InBounds(c.x, c.y)) return false;

        Vector2 dir = flow[(int)type][Idx(c.x, c.y)];
        if (dir == Vector2.zero) return false;

        point = CellToWorld(c.x + Mathf.RoundToInt(dir.x), c.y + Mathf.RoundToInt(dir.y));
        return true;
    }

    // Naar welk target leidt het pad vanaf deze positie (null als er geen pad is)
    public Transform GetGoal(Vector3 worldPos, TargetType type = TargetType.DefenseBuilding)
    {
        Vector2Int c = WorldToCell(worldPos);
        return InBounds(c.x, c.y) ? goal[(int)type][Idx(c.x, c.y)] : null;
    }

    public bool IsBlocked(Vector3 worldPos)
    {
        Vector2Int c = WorldToCell(worldPos);
        return InBounds(c.x, c.y) && costField[Idx(c.x, c.y)] == Impassable;
    }

    // Zijn er geen impassable cellen tussen deze twee punten?
    public bool HasLineOfSight(Vector3 from, Vector3 to)
    {
        int steps = Mathf.CeilToInt(Vector2.Distance(from, to) / (cellSize * 0.25f));

        for (int i = 1; i <= steps; i++)
            if (IsBlocked(Vector3.Lerp(from, to, (float)i / steps)))
                return false;

        return true;
    }

    // ---------- Opbouwen ----------

    void BuildCosts()
    {
        Vector2 size = Vector2.one * cellSize * 0.9f;

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                byte cost = 1;
                foreach (var lc in layerCosts)
                    if (lc.cost > cost && Physics2D.OverlapBox(CellToWorld(x, y), size, 0f, lc.layer))
                        cost = lc.cost;

                costField[Idx(x, y)] = cost;
            }
    }
    List<Transform> GetTargets(TargetType type)
    {
        var list = new List<Transform>();
        
        switch (type)
        {
            case TargetType.DefenseBuilding:
                foreach (var t in FindObjectsByType<DBTest>(FindObjectsSortMode.None))
                    list.Add(t.transform);
                break;

            case TargetType.EnemyTarget:
                foreach (var t in FindObjectsByType<EnemyTargetSCript>(FindObjectsSortMode.None))
                    list.Add(t.transform);
                break;
        }

        return list;
    }

    void BuildFlow(TargetType type)
    {
        int[] dist = distance[(int)type];
        Vector2[] fl = flow[(int)type];
        Transform[] gl = goal[(int)type];

        for (int i = 0; i < dist.Length; i++)
        {
            dist[i] = int.MaxValue;
            fl[i] = Vector2.zero;
            gl[i] = null;
        }

        var queue = new Queue<Vector2Int>();

        
        foreach (Transform t in GetTargets(type))
        {
            Collider2D col = t.GetComponent<Collider2D>();
            Vector2 min = col ? col.bounds.min : (Vector2)t.position;
            Vector2 max = col ? col.bounds.max : (Vector2)t.position;

            Vector2Int cMin = WorldToCell(min), cMax = WorldToCell(max);

            for (int x = cMin.x; x <= cMax.x; x++)
                for (int y = cMin.y; y <= cMax.y; y++)
                {
                    if (!InBounds(x, y)) continue;
                    dist[Idx(x, y)] = 0;
                    gl[Idx(x, y)] = t;
                    queue.Enqueue(new Vector2Int(x, y));
                }
        }

        while (queue.Count > 0)
        {
            Vector2Int cur = queue.Dequeue();

            foreach (var d in Dirs)
            {
                int nx = cur.x + d.x, ny = cur.y + d.y;
                if (!CanStep(cur.x, cur.y, d)) continue;

                int step = costField[Idx(nx, ny)] * (d.x != 0 && d.y != 0 ? 141 : 100);
                int newDist = dist[Idx(cur.x, cur.y)] + step;

                if (newDist < dist[Idx(nx, ny)])
                {
                    dist[Idx(nx, ny)] = newDist;
                    gl[Idx(nx, ny)] = gl[Idx(cur.x, cur.y)];
                    queue.Enqueue(new Vector2Int(nx, ny));
                }
            }
        }

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int best = dist[Idx(x, y)];
                if (best == int.MaxValue || best == 0) continue;

                foreach (var d in Dirs)
                {
                    int nx = x + d.x, ny = y + d.y;
                    if (!InBounds(nx, ny) || dist[Idx(nx, ny)] >= best) continue;
                    if (dist[Idx(nx, ny)] != 0 && !CanStep(x, y, d)) continue;
                    best = dist[Idx(nx, ny)];
                    fl[Idx(x, y)] = ((Vector2)d).normalized;
                }
            }
    }

    // ---------- Gizmos ----------

    void OnDrawGizmos()//Gizmos puur voor het debuggen. laat de flowfield grid, de costfields en de paden van de units zien.
    {
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                byte c = costField != null ? costField[Idx(x, y)] : (byte)1;

                if (c == 1) Gizmos.color = new Color(1f, 1f, 1f, 0.2f);
                else if (c == Impassable) Gizmos.color = Color.red;
                else Gizmos.color = Color.yellow;

                Gizmos.DrawWireCube(CellToWorld(x, y), new Vector3(cellSize, cellSize, 0.1f));
            }

        if (showAllPaths && Application.isPlaying)
        {
            foreach (var u in FindObjectsByType<EnemySoldierAI>(FindObjectsSortMode.None))
                DrawPath(u.transform.position, TargetType.DefenseBuilding);

            foreach (var u in FindObjectsByType<PlayerUnitTestAI>(FindObjectsSortMode.None))
                DrawPath(u.transform.position, TargetType.EnemyTarget);
        }
    }

    public void DrawPath(Vector3 start, TargetType type = TargetType.DefenseBuilding)//tekent het pad van de units (alleen voor testen).
    {
        if (flow == null) return;

        Vector2[] fl = flow[(int)type];
        Gizmos.color = type == TargetType.DefenseBuilding ? Color.green : Color.cyan;

        Vector2Int cell = WorldToCell(start);
        Vector3 prev = start;

        for (int i = 0; i < 200 && InBounds(cell.x, cell.y); i++)
        {
            Vector2 dir = fl[Idx(cell.x, cell.y)];
            if (dir == Vector2.zero) break;

            cell += new Vector2Int(Mathf.RoundToInt(dir.x), Mathf.RoundToInt(dir.y));
            Vector3 next = CellToWorld(cell.x, cell.y);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}