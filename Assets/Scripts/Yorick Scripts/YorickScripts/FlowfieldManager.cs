using System.Collections.Generic;
using UnityEngine;

public class FlowfieldManager : MonoBehaviour
{
    public static FlowfieldManager Instance { get; private set; }

    // Voeg hier later gewoon meer doelen aan toe
    public enum TargetType
    {
        DefenseBuilding,
        EnemyTarget,
        PlayerVillager,
        DefenseBuildingBreach,  // Vikings: muren slopen is de kortste route
        DefenseBuildingSiege,   // soldiers: muur slopen alleen als er geen open pad is
        PlayerVillagerSiege     // bandits: idem
    }

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
        public byte cost;        // 255 is een tile waar je niet doorheen kunt.
        public bool breakable;   // vink aan bij de muur-layer: dicht voor iedereen, behalve in de pad-planning van breakers
    }

    [Header("Cost Settings")]
    public LayerCost[] layerCosts;

    [Header("Breach Settings")]
    public byte wallBreachCost = 1;    // Vikings: muur = even goedkoop als open grond
    public byte wallSiegeCost = 200;   // soldiers/bandits: muur = enorme omweg (max 254)

    const byte Impassable = 255;

    [SerializeField] byte[] costField;
    byte[] breachCostField;
    byte[] siegeCostField;
    int breakableMask;

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
        breachCostField = new byte[width * height];
        siegeCostField = new byte[width * height];
        distance = new int[typeCount][];
        flow = new Vector2[typeCount][];
        goal = new Transform[typeCount][];

        for (int t = 0; t < typeCount; t++)
        {
            distance[t] = new int[width * height];
            flow[t] = new Vector2[width * height];
            goal[t] = new Transform[width * height];
        }

        breakableMask = 0;
        foreach (var lc in layerCosts)
        {
            if (lc.layer.value == 0)
                Debug.LogWarning("Een layerCost heeft geen layer ingesteld.");

            if (lc.breakable) breakableMask |= lc.layer.value;
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

    bool CanStep(int x, int y, Vector2Int d, byte[] costs)
    {
        int nx = x + d.x, ny = y + d.y;
        if (!InBounds(nx, ny) || costs[Idx(nx, ny)] == Impassable) return false;

        if (d.x != 0 && d.y != 0)
            if (costs[Idx(x + d.x, y)] == Impassable || costs[Idx(x, y + d.y)] == Impassable)
                return false;

        return true;
    }

    // Welk kostenveld hoort bij dit type?
    byte[] CostsFor(TargetType type) => type switch
    {
        TargetType.DefenseBuildingBreach => breachCostField,
        TargetType.DefenseBuildingSiege or TargetType.PlayerVillagerSiege => siegeCostField,
        _ => costField
    };

    // Mag dit type muren als target kiezen?
    public bool CanBreach(TargetType type) =>
        type == TargetType.DefenseBuildingBreach ||
        type == TargetType.DefenseBuildingSiege ||
        type == TargetType.PlayerVillagerSiege;

    // ---------- Publiek (gebruikt door de units) ----------

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

    // Fysieke blokkade: geldt voor alle units
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

    // Staat er een breakable muur op de volgende stap van het pad?
    public bool TryGetBlockingWall(Vector3 worldPos, TargetType type, out Transform wall)
    {
        wall = null;
        Vector2Int c = WorldToCell(worldPos);
        if (!InBounds(c.x, c.y)) return false;

        Vector2 dir = flow[(int)type][Idx(c.x, c.y)];
        if (dir == Vector2.zero) return false;

        int dx = Mathf.RoundToInt(dir.x), dy = Mathf.RoundToInt(dir.y);

        // Doelcel, en bij een diagonaal ook de twee zijcellen
        if (TryWallAt(c.x + dx, c.y + dy, out wall)) return true;

        if (dx != 0 && dy != 0)
        {
            if (TryWallAt(c.x + dx, c.y, out wall)) return true;
            if (TryWallAt(c.x, c.y + dy, out wall)) return true;
        }

        return false;
    }

    bool TryWallAt(int x, int y, out Transform wall)
    {
        wall = null;
        if (!InBounds(x, y) || costField[Idx(x, y)] != Impassable) return false;

        Collider2D col = Physics2D.OverlapBox(CellToWorld(x, y), Vector2.one * cellSize * 0.9f, 0f, breakableMask);
        if (col == null) return false;

        wall = col.transform;
        return true;
    }

    // ---------- Opbouwen ----------

    void BuildCosts()
    {
        Vector2 size = Vector2.one * cellSize * 0.9f;

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                byte cost = 1, breachCost = 1, siegeCost = 1;

                foreach (var lc in layerCosts)
                {
                    byte c = lc.breakable ? Impassable : lc.cost;      // normale beweging: muur is altijd dicht
                    byte bc = lc.breakable ? wallBreachCost : lc.cost;  // Vikings: muur is goedkoop
                    byte sc = lc.breakable ? wallSiegeCost : lc.cost;   // soldiers/bandits: muur is heel duur

                    if ((c > cost || bc > breachCost || sc > siegeCost) &&
                        Physics2D.OverlapBox(CellToWorld(x, y), size, 0f, lc.layer))
                    {
                        if (c > cost) cost = c;
                        if (bc > breachCost) breachCost = bc;
                        if (sc > siegeCost) siegeCost = sc;
                    }
                }

                costField[Idx(x, y)] = cost;
                breachCostField[Idx(x, y)] = breachCost;
                siegeCostField[Idx(x, y)] = siegeCost;
            }
    }

    List<Transform> GetTargets(TargetType type)
    {
        var list = new List<Transform>();

        switch (type)
        {
            case TargetType.DefenseBuilding:
            case TargetType.DefenseBuildingBreach:
            case TargetType.DefenseBuildingSiege:
                foreach (var t in FindObjectsByType<DBTest>(FindObjectsSortMode.None))
                    list.Add(t.transform);
                break;

            case TargetType.EnemyTarget:
                foreach (var t in FindObjectsByType<EnemyTargetSCript>(FindObjectsSortMode.None))
                    list.Add(t.transform);
                break;

            case TargetType.PlayerVillager:
            case TargetType.PlayerVillagerSiege:
                foreach (var t in FindObjectsByType<PVTest>(FindObjectsSortMode.None))
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
        byte[] costs = CostsFor(type);

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
                if (!CanStep(cur.x, cur.y, d, costs)) continue;

                int step = costs[Idx(nx, ny)] * (d.x != 0 && d.y != 0 ? 141 : 100);
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
                    if (dist[Idx(nx, ny)] != 0 && !CanStep(x, y, d, costs)) continue;
                    best = dist[Idx(nx, ny)];
                    fl[Idx(x, y)] = ((Vector2)d).normalized;
                }
            }
    }

    // ---------- Gizmos ----------

    void OnDrawGizmos()// puur voor het debuggen
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
                DrawPath(u.transform.position, u.TargetType);

            foreach (var u in FindObjectsByType<PlayerUnitTestAI>(FindObjectsSortMode.None))
                DrawPath(u.transform.position, TargetType.EnemyTarget);

            foreach (var u in FindObjectsByType<EnemyBanditAI>(FindObjectsSortMode.None))
                DrawPath(u.transform.position, u.TargetType);

            foreach (var u in FindObjectsByType<EnemyVikingAI>(FindObjectsSortMode.None))
                DrawPath(u.transform.position, u.TargetType);
        }
    }

    public void DrawPath(Vector3 start, TargetType type = TargetType.DefenseBuilding)
    {
        if (flow == null) return;

        Vector2[] fl = flow[(int)type];
        Gizmos.color = type switch
        {
            TargetType.DefenseBuilding or TargetType.DefenseBuildingSiege => Color.green,
            TargetType.EnemyTarget => Color.cyan,
            TargetType.PlayerVillager or TargetType.PlayerVillagerSiege => Color.magenta,
            _ => Color.blue
        };

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