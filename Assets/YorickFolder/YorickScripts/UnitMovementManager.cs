using System.Collections.Generic;
using UnityEngine;

public class UnitMovementManager : MonoBehaviour
{
    public static readonly List<UnitAIBase> Units = new List<UnitAIBase>();

    [Header("Retarget")]
    [SerializeField] float retargetInterval = 0.2f;

    [Header("Separation")]
    [SerializeField] float separationRadius = 0.6f;
    [SerializeField] float separationStrength = 1.5f;

    static readonly Collider2D[] buffer = new Collider2D[32];
    readonly Dictionary<long, List<UnitAIBase>> grid = new Dictionary<long, List<UnitAIBase>>();

    static long Key(int x, int y) => ((long)x << 32) ^ (uint)y;

    void Update()
    {
        FlowfieldManager fm = FlowfieldManager.Instance;
        if (fm == null) return;

        BuildGrid();

        float dt = Time.deltaTime;
        float now = Time.time;

        for (int i = 0; i < Units.Count; i++)
        {
            UnitAIBase u = Units[i];

            if (now >= u.nextRetargetTime)
            {
                u.nextRetargetTime = now + retargetInterval;
                Retarget(u, fm);
            }

            Move(u, fm, dt);
        }
    }

    void Retarget(UnitAIBase u, FlowfieldManager fm)//als een vijand in de radius komt gaat de unit daarnaartoe, anders naar het doel van het pad.
    {
        Vector2 pos = u.transform.position;

        var filter = new ContactFilter2D();
        filter.SetLayerMask(u.DetectionLayer);

        int count = Physics2D.OverlapCircle(pos, u.Data.unitDetectionRadius, filter, buffer);

        if (count > 0)
        {
            Transform closest = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                float d = (pos - (Vector2)buffer[i].transform.position).sqrMagnitude;
                if (d < closestDistance)
                {
                    closestDistance = d;
                    closest = buffer[i].transform;
                }
            }

            u.hasUnitInRange = true;
            u.currentTarget = closest;
        }
        else
        {
            u.hasUnitInRange = false;
            Transform goal = fm.GetGoal(pos, u.TargetType);
            u.currentTarget = goal != null ? goal : u.GetFallbackTarget();
        }
    }

    void Move(UnitAIBase unitBase, FlowfieldManager fm, float dt)
    {
        Transform target = unitBase.currentTarget;

        if (target == null)
        {
            unitBase.nextRetargetTime = 0f;
            return;
        }

        if (unitBase.AttackRange.IsInAttackRange(target))
        {
            unitBase.Attack?.TryAttack(target);
            return;
        }

        Vector3 pos = unitBase.transform.position;
        bool chase = unitBase.hasUnitInRange && fm.HasLineOfSight(pos, target.position);

        Vector3 destination;

        if (chase)
        {
            destination = target.position;
        }
        else if (fm.TryGetNextPoint(pos, out destination, unitBase.TargetType))
        {
            destination += (Vector3)(unitBase.CellOffset * fm.cellSize);
        }
        else
        {
            return;
        }

        if (!chase && fm.IsBlocked(destination)) return;

        Vector2 toDestination = (Vector2)(destination - pos);
        Vector2 velocity = toDestination.normalized + GetSeparation(unitBase) * separationStrength;

        if (velocity.magnitude > 1f) velocity.Normalize();

        Vector3 step = (Vector3)(velocity * unitBase.Data.unitSpeed * dt / 2);

        if (!fm.IsBlocked(pos + step))
            unitBase.transform.position = pos + step;
        else if (!fm.IsBlocked(pos + new Vector3(step.x, 0, 0)))
            unitBase.transform.position = pos + new Vector3(step.x, 0, 0);
        else if (!fm.IsBlocked(pos + new Vector3(0, step.y, 0)))
            unitBase.transform.position = pos + new Vector3(0, step.y, 0);
    }

    void BuildGrid()
    {
        foreach (var list in grid.Values) list.Clear();

        for (int i = 0; i < Units.Count; i++)
        {
            Vector2 p = Units[i].transform.position;
            long key = Key(Mathf.FloorToInt(p.x / separationRadius), Mathf.FloorToInt(p.y / separationRadius));

            if (!grid.TryGetValue(key, out var cell))
            {
                cell = new List<UnitAIBase>();
                grid[key] = cell;
            }
            cell.Add(Units[i]);
        }
    }

    Vector2 GetSeparation(UnitAIBase u)//om te zorgen dat units niet in 1 rechte lijn aanvallen en wat meer opgesplitst zijn.
    {
        Vector2 pos = u.transform.position;
        int cx = Mathf.FloorToInt(pos.x / separationRadius);
        int cy = Mathf.FloorToInt(pos.y / separationRadius);

        Vector2 push = Vector2.zero;

        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (!grid.TryGetValue(Key(cx + dx, cy + dy), out var cell)) continue;

                for (int i = 0; i < cell.Count; i++)
                {
                    UnitAIBase other = cell[i];
                    if (other == u || other.TargetType != u.TargetType) continue;

                    Vector2 away = pos - (Vector2)other.transform.position;
                    float dist = away.magnitude;

                    if (dist >= separationRadius) continue;

                    if (dist < 0.001f) away = Random.insideUnitCircle;
                    else away /= dist;

                    push += away * (1f - dist / separationRadius);
                }
            }

        return push;
    }
}