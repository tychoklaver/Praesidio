using UnityEngine;

public class BuildingSelector : MonoBehaviour
{
    [SerializeField] Camera mainCamera;
    [SerializeField] BuildingGhost ghost;
    [SerializeField] BuildingPanel buildingPanel;
    
    void Update()
    {
        if (ghost.IsPlacing)
        {
            buildingPanel.Hide();
            return;
        }
        if (!Input.GetMouseButtonDown(0)) return;
        
        Vector2 world = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Collider2D hit = Physics2D.OverlapPoint(world);
        ProductionBuilding b = hit != null ? hit.GetComponent<ProductionBuilding>() : null;

        if (b != null)
        {
            Debug.Log("rah");
            buildingPanel.Show(b);
        }
    }
    
}
