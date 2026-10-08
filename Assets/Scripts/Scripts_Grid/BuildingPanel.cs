using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildingPanel : MonoBehaviour
{
    [SerializeField] GameObject root;
    [SerializeField] TMP_Text buildingName;
    [SerializeField] TMP_Text workerText;
    [SerializeField] Button addButton;
    [SerializeField] Button removeButton;
    
    ProductionBuilding currentBuilding;

    private void Start()
    {
        root.SetActive(false);
    }

    public void Show(ProductionBuilding building)
    {
        currentBuilding = building;
        root.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        if (currentBuilding == null) return;
        root.SetActive(false);
        currentBuilding = null;
        
    }

    public void AddWorker()
    {
        if  (currentBuilding != null) currentBuilding.AddWorker();
    }

    public void RemoveWorker()
    {
        if  (currentBuilding != null) currentBuilding.RemoveWorker();
    }

    private void Update()
    {
        if (currentBuilding == null) return;
        Refresh();
    }

    private void Refresh()
    {
        string title = currentBuilding.buildingType.ToString();
        if (currentBuilding.Job != null) title += currentBuilding.Job.name;
        if (!currentBuilding.IsComplete) title += $" - In aanbouw {Mathf.RoundToInt(currentBuilding.ConstructionProgress * 100)}%";
        
        buildingName.text = title;
        
        workerText.text = $"Werkers: {currentBuilding.WorkerCount}/{currentBuilding.MaxWorkers} (Vrij {WorkerManager.Instance.IdleCount}";
        addButton.interactable = currentBuilding.CanAddWorker;
        removeButton.interactable = currentBuilding.CanRemoveWorker;
    }
}