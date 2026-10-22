using UnityEngine;

public class AttackRangeHandler : MonoBehaviour
{
    [SerializeField] public EnemyUnitSO unitData;
    [SerializeField] bool showGizmos;

    Transform cachedFor;
    Collider2D cachedCollider;

    public void SetUnitData(EnemyUnitSO data)
    {
        unitData = data;
    }

    public bool IsInAttackRange(Transform target)
    {
        if (unitData == null || target == null) return false;

        Vector2 pos = transform.position;
        return Vector2.Distance(pos, ClosestPoint(target, pos)) <= unitData.unitAttackRange;
    }

    Vector2 ClosestPoint(Transform target, Vector2 from)
    {
        if (cachedFor != target)
        {
            cachedFor = target;
            cachedCollider = target.GetComponent<Collider2D>();
            if (!cachedCollider) cachedCollider = target.GetComponentInChildren<Collider2D>();
        }

        return cachedCollider ? cachedCollider.ClosestPoint(from) : (Vector2)target.position;
    }

    private void OnDrawGizmos()
    {
        if (unitData == null || !showGizmos) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, unitData.unitAttackRange);
    }
}
