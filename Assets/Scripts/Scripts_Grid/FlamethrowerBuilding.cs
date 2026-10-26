using UnityEngine;

[RequireComponent(typeof(BuildingHealth))]
public class FlamethrowerBuilding : Building
{
    [Header("Combat")]
    [SerializeField] float range = 3f;
    [SerializeField, Range(10f, 180f)] float coneAngle = 60f;
    [SerializeField] float damagePerSecond = 15f;

    [Header("Burn   ")]
    [SerializeField] float burnDps = 5f;
    [SerializeField] float burnDuration = 3f;

    [Header("Visuals")]
    [SerializeField] Transform flameRoot;
    [SerializeField] ParticleSystem flames;

    UnitHealth target;
    Vector2 aimDirection = Vector2.right;
    bool flameOn;

    void Start() => SetFlame(false);

    void Update()
    {
        if (!IsComplete)
        {
            SetFlame(false);
            return;
        }

        if (target == null || !InRange(target))
            target = FindTarget();

        if (target == null)
        {
            SetFlame(false);
            return;
        }
        
        aimDirection = target.transform.position - transform.position;
        aimDirection.Normalize();
        if (flameRoot != null)
        {
            float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
            flameRoot.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        SetFlame(true);
        BurnCone();
    }
    
    void BurnCone()
    {
        float rangeSqr = range * range;
        float halfCone = coneAngle * 0.5f;
        var all = UnitHealth.all;
        
        for (int i = all.Count - 1; i >= 0; i--)
        {
            if (i >= all.Count) continue;
            UnitHealth unit = all[i];
            if (unit == null || !unit.IsAlive) continue;

            Vector2 to = unit.transform.position - transform.position;
            if (to.sqrMagnitude > rangeSqr) continue;
            if (Vector2.Angle(aimDirection, to) > halfCone) continue;

            unit.TakeDamage(damagePerSecond * Time.deltaTime);
            if (unit.IsAlive) BurnStatus.Apply(unit.transform, burnDps, burnDuration);
        }
    }

    bool InRange(UnitHealth u) =>
        u.IsAlive && (u.transform.position - transform.position).sqrMagnitude <= range * range;
    
    UnitHealth FindTarget()
    {
        UnitHealth best = null;
        float bestDist = range * range;
        foreach (var u in UnitHealth.all)
        {
            if (u == null || !u.IsAlive) continue;
            float d = (u.transform.position - transform.position).sqrMagnitude;
            if (d <= bestDist) { bestDist = d; best = u; }
        }
        return best;
    }

    void SetFlame(bool on)
    {
        if (flames == null || flameOn == on) return;
        flameOn = on;

        var emission = flames.emission;
        emission.enabled = on;
        if (on && !flames.isPlaying) flames.Play();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.7f);
        Vector3 pos = transform.position;
        Vector2 dir = Application.isPlaying ? aimDirection : Vector2.right;
        float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float half = coneAngle * 0.5f;

        Vector3 left = Quaternion.Euler(0, 0, baseAngle + half) * Vector3.right * range;
        Vector3 right = Quaternion.Euler(0, 0, baseAngle - half) * Vector3.right * range;
        Gizmos.DrawLine(pos, pos + left);
        Gizmos.DrawLine(pos, pos + right);
        Gizmos.DrawLine(pos + left, pos + right);
    }
}