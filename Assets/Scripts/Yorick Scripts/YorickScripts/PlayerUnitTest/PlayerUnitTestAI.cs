using System.Collections.Generic;
using UnityEngine;

public class PlayerUnitTestAI : UnitAIBase
{
    [SerializeField] LayerMask enemyLayer;

    public override FlowfieldManager.TargetType TargetType => FlowfieldManager.TargetType.EnemyTarget;
    public override LayerMask DetectionLayer => enemyLayer;

    public override Transform GetFallbackTarget() => PlayerUnitTargetingManagerTest.Instance != null ? PlayerUnitTargetingManagerTest.Instance.GetClosest(transform.position): null;
}