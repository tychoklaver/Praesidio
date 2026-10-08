using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class RangedAttack : UnitAttackBase
{
    private Transform currentTarget;
    [SerializeField] Transform firePoint;
    [SerializeField] Projectile projectilePrefab;
    protected override void PerformAttack(Transform target)
    {
        if (target == null || target == transform || target.IsChildOf(transform)) return;
        print("Valt aan");
        currentTarget = target;

        TriggerAttackAnimation();

        OnAttackAnimationEnd();
    }
    public void OnAttackAnimationEnd()
    {
        if (currentTarget != null)
        {
            SpawnProjectile();
            //currentTarget.GetComponent<IDamageable>()?.TakeDamage(detector.unitData.unitDamage);
        }
    }
    public void SpawnProjectile()
    {
        if (currentTarget == null) return;

        Vector3 pos = firePoint != null ? firePoint.position : transform.position;
        Projectile p = Instantiate(projectilePrefab, pos, Quaternion.identity);
        p.Initialize(currentTarget, detector.unitData.unitDamage);
    }
}
