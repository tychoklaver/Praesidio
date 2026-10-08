using UnityEngine;

[CreateAssetMenu(menuName = "Units/EnemyUnit Data")]
public class EnemyUnitSO : ScriptableObject
{
    [Header("Unit Stats")]
    public float unitHealth;
    public float unitSpeed;
    public float unitArmor;
    public float unitAttackRange;
    public float unitAttackSpeed;
    public float unitDamage;

    public float unitPrice;

    public float unitDetectionRadius;
}
