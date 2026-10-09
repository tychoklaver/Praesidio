using UnityEngine;

public class UnitHealth : MonoBehaviour, IDamageable
{
    [SerializeField] EnemyUnitSO unitData;
    [SerializeField] float health;

    private void GetHealth()
    {
        health = unitData.unitHealth;
    }
    public void TakeDamage(float amount)
    {
        health -= amount;
        if (health <= 0f) Destroy(gameObject);
    }
    public void SetUnitData(EnemyUnitSO data)
    {
        unitData = data;
        GetHealth();
    }
}
