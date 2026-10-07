using System;
using System.Collections.Generic;
using UnityEngine;

public class Building : MonoBehaviour
{
    public static List<Building> constructionSites = new List<Building>();
    
    public BuildingType buildingType;
    public ResourceCost[] cost;
    public Vector2Int size = Vector2Int.one;

    [SerializeField] float buildWorkNeeded = 1f;
    
    public Vector2Int Cell { get; private set; }
    public bool IsComplete { get; private set; }
    public float ConstructionProgress =>  IsComplete ? 1f : Mathf.Clamp01(_progress / buildWorkNeeded);
    
    private float _progress;
    private bool _placed;
    SpriteRenderer[] _renderers;
    
    public void Place(Vector2Int cell)
    {
        Cell = cell;
        _placed = true;
        transform.position = GridManager.Instance.CellToWorld(cell, size);
        GridManager.Instance.Occupy(cell, size);
        ResourceManager.Instance.Subtract(cost);
        OnPlaced();
        
        _renderers = GetComponentsInChildren<SpriteRenderer>();
        if (buildWorkNeeded <= 0f)
        {
            IsComplete = true;
            OnCompleted();
            return;
        }
        
        IsComplete = false;
        constructionSites.Add(this);
        SetAlpha(0.5f);
    }

    public void AddConstructionWork(float amount)
    {
        if (IsComplete) return;
        _progress  += amount;
        if (_progress >= buildWorkNeeded) Complete();
    }
    
    void Complete()
    {
        IsComplete = true;
        constructionSites.Remove(this);
        SetAlpha(1);
        OnCompleted();
    }

    protected virtual void OnDestroy()
    {
        constructionSites.Remove(this);
        
        if (_placed && GridManager.Instance != null)
            GridManager.Instance.Free(Cell, size);
    }

    protected virtual void OnPlaced() { }

    protected virtual void OnCompleted() { }
    

    protected virtual void SetAlpha(float alpha)
    {
        if (_renderers == null) return;
        foreach (var renderer in _renderers)
        {
            Color c = renderer.color;
            c.a = alpha;
            renderer.color = c;
        }
    }
}