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

    [HideInInspector] public Transform cachedFor;
    [HideInInspector] public Collider2D cachedCollider;

    [Header("Animation")]
    [SerializeField] Animator animator;
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] bool spriteFacesRight = true;
    static readonly int IsWalkingHash = Animator.StringToHash("isWalking");
    bool isWalking;
    void Awake()
    {
        CellOffset = new Vector2(Random.Range(-spread, spread), Random.Range(-spread, spread));
        AttackRange = GetComponent<AttackRangeHandler>();
        Attack = GetComponent<UnitAttackBase>();
        nextRetargetTime = Random.Range(0f, 0.2f); // niet alle units in hetzelfde frame
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Start()
    {
        GetComponent<AttackRangeHandler>()?.SetUnitData(enemyUnitData);
        GetComponent<UnitHealth>()?.SetUnitData(enemyUnitData);
    }
    public void SetWalking(bool walking)
    {
        Debug.Log($"{name} walking = {walking}, animator = {animator}", this);
        if (animator == null || isWalking == walking) return;
        isWalking = walking;
        animator.SetBool(IsWalkingHash, walking);
    }

    // Wordt aangeroepen door UnitMovementManager
    public void FaceDirection(float dirX)
    {
        if (spriteRenderer == null || Mathf.Abs(dirX) < 0.01f) return;
        bool movingRight = dirX > 0f;
        spriteRenderer.flipX = spriteFacesRight ? !movingRight : movingRight;
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