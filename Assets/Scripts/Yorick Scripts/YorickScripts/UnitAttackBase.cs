using Unity.VisualScripting;
using UnityEngine;
[RequireComponent(typeof(AttackRangeHandler))]
public abstract class UnitAttackBase : MonoBehaviour
{

    public string attackAnimationTrigger = "Attack";
    protected AttackRangeHandler detector;
    protected Animator animator;
    float cooldownTimer;
    Transform pendingTarget;
    void Awake()
    {
        detector = GetComponent<AttackRangeHandler>();
        animator = GetComponent<Animator>();
    }
    protected virtual void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
    }
    protected void TriggerAttackAnimation()
    {
        if (animator == null || string.IsNullOrEmpty(attackAnimationTrigger)) return;
        animator.SetTrigger(attackAnimationTrigger);
    }
    public bool TryAttack(Transform target)
    {
        if (cooldownTimer > 0f) return false;
        if (!detector.IsInAttackRange(target)) return false;

        cooldownTimer = detector.unitData.unitAttackSpeed;
        pendingTarget = target;

        if (animator != null)
            animator.SetTrigger(attackAnimationTrigger);
        else
            OnAttackHit();

        return true;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void OnAttackHit()
    {
        if (pendingTarget == null) return;
        PerformAttack(pendingTarget);
    }
    protected abstract void PerformAttack(Transform target);
}
