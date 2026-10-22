using UnityEngine;
using System.Collections.Generic;

public class EnemySoldierAI : UnitAIBase
{
    [SerializeField] LayerMask playerGuardUnit;

    public override FlowfieldManager.TargetType TargetType => FlowfieldManager.TargetType.DefenseBuilding;
    public override LayerMask DetectionLayer => playerGuardUnit;

    public override Transform GetFallbackTarget() => EnemySoldierTargetManager.Instance != null ? EnemySoldierTargetManager.Instance.GetClosest(transform.position): null;
}