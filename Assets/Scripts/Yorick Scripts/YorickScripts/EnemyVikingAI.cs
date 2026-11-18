using UnityEngine;

public class EnemyVikingAI : UnitAIBase
{
    [SerializeField] LayerMask playerGuardUnit;

    [Header("Muren slopen")]
    [SerializeField] LayerMask wallLayer;     // zelfde layer als de breakable muren
    [SerializeField] int wallsToBreak = 3;    // maximaal zoveel muren slopen

    int wallsBroken;
    bool breachDone;
    Transform trackedWall;

    public override FlowfieldManager.TargetType TargetType =>
        breachDone
            ? FlowfieldManager.TargetType.DefenseBuilding
            : FlowfieldManager.TargetType.DefenseBuildingBreach;

    public override LayerMask DetectionLayer => playerGuardUnit;

    public override Transform GetFallbackTarget() =>
        EnemyVikingTargetManager.Instance != null ? EnemyVikingTargetManager.Instance.GetClosest(transform.position) : null;

    // Wordt alleen aangeroepen bij een retarget (elke ~0.2s), niet elke frame
    public override void OnRetargeted()
    {
        var fm = FlowfieldManager.Instance;
        if (fm == null) return;

        // Is de muur die we aanvielen weg? Dan telt die mee.
        if (trackedWall != null || wasTracking)
        {
            if (trackedWall == null)   // Unity-null: vernietigd
            {
                wasTracking = false;
                wallsBroken++;
                if (wallsBroken >= wallsToBreak) breachDone = true;
            }
        }

        bool normalPath = fm.GetGoal(transform.position, FlowfieldManager.TargetType.DefenseBuilding) != null;

        if (breachDone)
        {
            if (!normalPath)
            {
                breachDone = false;
                wallsBroken = 0;
            }
        }
        else if (wallsBroken > 0 && normalPath)
        {
            
            breachDone = true;
        }
        Transform t = currentTarget;
        if (!breachDone && t != null && (wallLayer.value & (1 << t.gameObject.layer)) != 0)
        {
            trackedWall = t;
            wasTracking = true;
        }
    }

    bool wasTracking;
}
