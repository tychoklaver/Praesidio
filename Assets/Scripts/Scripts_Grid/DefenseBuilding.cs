using UnityEngine;

[RequireComponent(typeof(BuildingHealth))]
public class DefenseBuilding : Building
{
    [Header("Combat")] 
    [SerializeField] float range = 5f;
    [SerializeField] float damage = 10f;

    [Tooltip("Schoten per seconde")] 
    [SerializeField] float fireRate = 1f;

    [Header("Projectiel")]
    [SerializeField] Projectile projectilePrefab;
    [SerializeField] Transform firePoint;

    float cooldown;
    UnitHealth target;

    void Update()
    {
        if (!IsComplete) return;

        cooldown -= Time.deltaTime;

        if (target == null || !InRange(target))
            target = FindTarget();

        if (target == null || cooldown > 0f) return;

        Fire(target);
        cooldown = 1f / fireRate;
    }

    bool InRange(UnitHealth e) =>
        e.IsAlive && (e.transform.position - transform.position).sqrMagnitude <= range * range;
    
    UnitHealth FindTarget()
    {
        UnitHealth best = null;
        float bestDist = range * range;
        foreach (var e in UnitHealth.all)
        {
            if (e == null || !e.IsAlive) continue;
            float d = (e.transform.position - transform.position).sqrMagnitude;
            if (d <= bestDist)
            {
                bestDist = d;
                best = e;
            }
        }

        return best;
    }

    void Fire(UnitHealth enemy)
    {
        if (projectilePrefab == null)
        {
            enemy.TakeDamage(damage);
            return;
        }

        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        Projectile p = Instantiate(projectilePrefab, origin, Quaternion.identity);
        p.Launch(enemy.transform, damage);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, range);
    }
}