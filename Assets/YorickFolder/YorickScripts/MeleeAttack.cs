using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class MeleeAttack : UnitAttackBase
{
    private Transform currentTarget;
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
            currentTarget.GetComponent<IDamageable>()?.TakeDamage(detector.unitData.unitDamage);
        }
    }
}
