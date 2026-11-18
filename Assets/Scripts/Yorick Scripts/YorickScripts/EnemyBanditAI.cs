using UnityEngine;

public class EnemyBanditAI : UnitAIBase
{
    [SerializeField] LayerMask playerGuardUnit;

    public override FlowfieldManager.TargetType TargetType => FlowfieldManager.TargetType.PlayerVillagerSiege;
    public override LayerMask DetectionLayer => playerGuardUnit;

    public override Transform GetFallbackTarget() => EnemyBanditTargetManager.Instance != null ? EnemyBanditTargetManager.Instance.GetClosest(transform.position) : null;
}
