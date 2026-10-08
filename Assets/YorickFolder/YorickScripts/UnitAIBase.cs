using UnityEngine;

public abstract class UnitAIBase : MonoBehaviour
{
    [Header("Scriptable Object")]
    [SerializeField] protected EnemyUnitSO enemyUnitData;

    [Header("Targeting")]
    public bool hasUnitInRange;
    public Transform currentTarget;

    [SerializeField] bool showGizmos;

    [Header("Movement")]
    [SerializeField, Range(0f, 0.45f)] float spread = 0.3f;

    // Verschilt per soort unit
    public abstract FlowfieldManager.TargetType TargetType { get; }
    public abstract LayerMask DetectionLayer { get; }
    public abstract Transform GetFallbackTarget();

    // Gebruikt door de manager
    public EnemyUnitSO Data => enemyUnitData;
    public Vector2 CellOffset { get; private set; }
    public AttackRangeHandler AttackRange { get; private set; }
    public UnitAttackBase Attack { get; private set; }
    [HideInInspector] public float nextRetargetTime;

    void Awake()
    {
        CellOffset = new Vector2(Random.Range(-spread, spread), Random.Range(-spread, spread));
        AttackRange = GetComponent<AttackRangeHandler>();
        Attack = GetComponent<UnitAttackBase>();
        nextRetargetTime = Random.Range(0f, 0.2f); // niet alle units in hetzelfde frame
    }

    void Start()
    {
        GetComponent<AttackRangeHandler>()?.SetUnitData(enemyUnitData);
        GetComponent<UnitHealth>()?.SetUnitData(enemyUnitData);
    }

    void OnEnable() { UnitMovementManager.Units.Add(this); }
    void OnDisable() { UnitMovementManager.Units.Remove(this); }

    void OnDrawGizmosSelected()
    {
        if (showGizmos)
        {
            Gizmos.color = Color.burlywood;
            Gizmos.DrawWireSphere(transform.position, enemyUnitData.unitDetectionRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, enemyUnitData.unitAttackRange);
        }

        if (FlowfieldManager.Instance != null)
            FlowfieldManager.Instance.DrawPath(transform.position, TargetType);
    }
}