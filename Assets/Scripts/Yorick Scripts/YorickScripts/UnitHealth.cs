using UnityEngine;

public class UnitHealth : MonoBehaviour, IDamageable
{
    [SerializeField] EnemyUnitSO unitData;
    [SerializeField] float health;
    [SerializeField] Animator animator;
    public bool isDead = false;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void GetHealth()
    {
        health = unitData.unitHealth;
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        health -= amount;
        if (health <= 0f) Die();
    }

    void Die()
    {
        isDead = true;
        UnitAttackBase attack = GetComponent<UnitAttackBase>();
        if (animator != null && attack != null)
            animator.ResetTrigger(attack.attackAnimationTrigger);

        Destroy(attack);
        Destroy(GetComponent<UnitAIBase>());
        Destroy(GetComponent<EnemyTargetSCript>());
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>())
            c.enabled = false;

        if (animator != null) animator.SetBool("isDead", true);

        Destroy(gameObject, 5f);
    }

    public void SetUnitData(EnemyUnitSO data)
    {
        unitData = data;
        GetHealth();
    }
}
