using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


public class Worker : MonoBehaviour
{
    public enum State { Idle, Construction, Working }
    
    [Header("Movement")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float wanderRadius = 5f;
    [SerializeField] Vector2 wanderPause = new Vector2(1f, 3f);
    
    [Header("Work")]
    [SerializeField] float buildingRate = 1f;
    [SerializeField] float workRange= 2f;

    
    public ProductionBuilding Home {get; private set;}
    public JobData Job => Home != null ? Home.Job : null;
    public State CurrentState {get; private set;} = State.Idle;
    public bool IsAvailable => Home  == null;
    
    Vector3 wanderCenter, wanderTarget;
    bool hasWanderTarget;
    float waitTimer, gatherTimer;

    public void AssignTo(ProductionBuilding building)
    {
        Home = building;
        gatherTimer = 0f;
    }

    public void Unassign()
    {
        Home = null;
        gatherTimer = 0f;
    }
    private void Update()
    {
        Building site = GetConstructionTarget();
        
        State next = site != null ? State.Construction : Home != null ? State.Working : State.Idle;

        if (next == State.Idle && CurrentState != State.Idle)
        {
            wanderCenter = transform.position;
            hasWanderTarget = false;
            waitTimer = 0f;
        }
        CurrentState = next;
        switch (CurrentState)
        {
            case State.Construction: Construct(site); break;
            case State.Working: Work(); break;
            default: Wander(); break;
        }
    }

    Building GetConstructionTarget()
    {
        if (Home != null) return Home.IsComplete ? null : Home;
        
        Building best = null;
        float bestDist = float.MaxValue;
        List<Building> sites = Building.constructionSites;
        for (int i = sites.Count - 1; i >= 0; i--)
        {
            Building s = sites[i];
            if (s == null)
            {
                sites.RemoveAt(i);
                continue;
            }
            float d = (s.transform.position - transform.position).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d; best = s;
            }
        }
        return best;
    }

    void Construct(Building site)
    {
        if (MoveTo(site.transform.position, ReachOf(site)))
            site.AddConstructionWork(buildingRate * Time.deltaTime);
    }

    private void Work()
    {
        if (Job == null) return;
        if (!MoveTo(Home.transform.position, ReachOf(Home))) return;
        
        gatherTimer += Time.deltaTime;
        if (gatherTimer >= Job.interval)
        {
            gatherTimer = 0f;
            Gather();
        }
    }
    void Gather()
    {
        if (Job != null)
            ResourceManager.Instance.Add(Job.resource, Job.amountPerTick);
    }

    void Wander()
    {
        if (waitTimer > 0f) {waitTimer -= Time.deltaTime; return;}

        if (!hasWanderTarget)
        {
            Vector2 offset = Random.insideUnitCircle * wanderRadius;
            wanderTarget = wanderCenter + new Vector3(offset.x, offset.y, 0f);
            hasWanderTarget = true;
        }

        if (MoveTo(wanderTarget, 0.05f))
        {
            hasWanderTarget = false;
            waitTimer = Random.Range(wanderPause.x, wanderPause.y);
        }
    }
    bool MoveTo(Vector3 target, float stopDistance)
    {
        Vector3 to = target  - transform.position;
        to.z = 0f;
        float distance = to.magnitude;
        if (distance <= stopDistance) return true;
        
        float step = Mathf.Min(moveSpeed * Time.deltaTime, distance - stopDistance);
        transform.position += to / distance * step;
        return false;
    }

    float ReachOf(Building building)
    {
        float half = Mathf.Max(building.size.x, building.size.y) * 0.5f * GridManager.Instance.CellSize;
        return half + workRange;
    }
}