using UnityEngine;

public class AttackRangeHandler : MonoBehaviour
{
    [SerializeField] public EnemyUnitSO unitData;
    [SerializeField] bool showGizmos;
    
    public void SetUnitData(EnemyUnitSO data)
    {
        unitData = data;
    }
    public bool IsInAttackRange(Transform target)
    {
        if (unitData == null || target == null) return false;
        
        return Vector2.Distance(transform.position, target.position) <= unitData.unitAttackRange;
    }
    private void OnDrawGizmos()
    {
        if (unitData == null) return;

        if(showGizmos == true)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, unitData.unitAttackRange);
        }
    }
}
