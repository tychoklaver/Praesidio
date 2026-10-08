using UnityEngine;
using UnityEngine.EventSystems;

public class BuildingGhost : MonoBehaviour
{
    [SerializeField] Camera cam;
    [SerializeField] Color validColor = new Color(0.3f, 1f, 0.3f, 0.6f);
    [SerializeField] Color invalidColor = new Color(1f, 0.3f, 0.3f, 0.6f);

    public bool isValidPlacement;
    public bool IsPlacing => prefab != null;

    Building prefab;
    GameObject ghost;
    SpriteRenderer[] renderers;
    Vector2Int currentCell;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    public void Begin(Building buildingPrefab)
    {
        Cancel();
        prefab = buildingPrefab;

        ghost = Instantiate(buildingPrefab.gameObject, transform);
        ghost.GetComponent<Building>().enabled = false;
        foreach (var col in ghost.GetComponentsInChildren<Collider2D>()) col.enabled = false;
        renderers = ghost.GetComponentsInChildren<SpriteRenderer>();

        if (GridVisualizer.Instance != null) GridVisualizer.Instance.SetVisible(true);
    }

    public void Cancel()
    {
        if (ghost != null) Destroy(ghost);
        ghost = null;
        prefab = null;

        if (GridVisualizer.Instance != null) GridVisualizer.Instance.SetVisible(false);
    }

    void Update()
    {
        if (prefab == null) return;

        Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
        world.z = 0f;
        FollowMouse(world);

        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (Input.GetMouseButtonDown(0) && isValidPlacement && !overUI)
            PlaceBuilding();

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            Cancel();
    }

    public void FollowMouse(Vector3 pos)
    {
        var grid = GridManager.Instance;
        currentCell = grid.WorldToCell(pos);
        ghost.transform.position = grid.CellToWorld(currentCell, prefab.size);

        isValidPlacement = grid.IsAreaFree(currentCell, prefab.size)
                           && ResourceManager.Instance.HasEnough(prefab.cost);

        Color c = isValidPlacement ? validColor : invalidColor;
        foreach (var r in renderers) r.color = c;
    }

    void PlaceBuilding()
    {
        Building b = Instantiate(prefab);
        b.Place(currentCell);
        
        if (!ResourceManager.Instance.HasEnough(prefab.cost)) Cancel();
    }
}