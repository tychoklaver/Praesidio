using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


public class Worker : MonoBehaviour
{
    public enum State { Idle, Construction, Working }
    public enum WorkPhase { ToNode, Gathering, ToDepot }
    
    [Header("Movement")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float wanderRadius = 5f;
    [SerializeField] Vector2 wanderPause = new Vector2(1f, 3f);
    
    [Header("Work")]
    [SerializeField] float buildingRate = 1f;
    [Tooltip("Hoe ver van de rand van een gebouw de worker blijft staan")]
    [SerializeField] float arriveDistance = 0.3f;

    
    public ProductionBuilding Home {get; private set;}
    public JobData Job => Home != null ? Home.Job : null;
    public State CurrentState {get; private set;} = State.Idle;
    public bool IsAvailable => Home  == null;
    public int Carried => carried;
    
    Vector3 wanderCenter, wanderTarget;
    bool hasWanderTarget;
    float waitTimer, gatherTimer;
    [SerializeField] WorkPhase phase = WorkPhase.ToNode;
    int carried;

    public void AssignTo(ProductionBuilding building)
    {
        Home = building;
        ResetWork();
    }

    public void Unassign()
    {
        Home = null;
        ResetWork();
    }

    void ResetWork()
    {
        gatherTimer = 0f;
        carried = 0;
        phase = WorkPhase.ToNode;
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

        ResourceNode node = Home.Node;
        if (node == null)
        {
            WorkAtBuilding();
            return;
        }

        switch (phase)
        {
            case WorkPhase.ToNode:
                if (MoveTo(node.transform.position, node.Reach))
                {
                    gatherTimer = 0f;
                    phase = WorkPhase.Gathering;
                }
                break;

            case WorkPhase.Gathering:
                gatherTimer += Time.deltaTime;
                if (gatherTimer >= Job.interval)
                {
                    carried = Job.amountPerTick;
                    phase = WorkPhase.ToDepot;
                }
                break;

            case WorkPhase.ToDepot:
                if (MoveTo(Home.transform.position, ReachOf(Home)))
                {
                    Deliver();
                    phase = WorkPhase.ToNode;
                }
                break;
        }
    }

    void WorkAtBuilding()
    {
        if (!MoveTo(Home.transform.position, ReachOf(Home))) return;

        gatherTimer += Time.deltaTime;
        if (gatherTimer >= Job.interval)
        {
            gatherTimer = 0f;
            carried = Job.amountPerTick;
            Deliver();
        }
    }

    void Deliver()
    {
        if (Job != null && carried > 0)
            ResourceManager.Instance.Add(Job.resource, carried);
        carried = 0;
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

        if (MoveTo(wanderTarget, 0f))
        {
            hasWanderTarget = false;
            waitTimer = Random.Range(wanderPause.x, wanderPause.y);
        }
    }

    const float ArriveTolerance = 0.05f;

    bool MoveTo(Vector3 target, float stopDistance)
    {
        Vector3 to = target  - transform.position;
        to.z = 0f;
        float distance = to.magnitude;
        if (distance <= stopDistance + ArriveTolerance) return true;
        
        float step = Mathf.Min(moveSpeed * Time.deltaTime, distance - stopDistance);
        transform.position += to / distance * step;
        return false;
    }
    
    float ReachOf(Building building)
    {
        float half = Mathf.Max(building.size.x, building.size.y) * 0.5f * GridManager.Instance.CellSize;
        return half + arriveDistance;
    }
}