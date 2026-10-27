using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BuildingPanel : MonoBehaviour
{
    [Header("HUD")] 
    [SerializeField] GameObject root;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text counterText;
    [SerializeField] Button minusButton;
    [SerializeField] Button plusButton;
    [SerializeField] RectTransform slotParent;

    [Header("Villagers")]
    [SerializeField] Sprite[] villagerPortraits;
    [SerializeField] Sprite filledSlot;
    [SerializeField] Sprite emptySlot;

    [Header("Slot layout")] 
    [SerializeField] Vector2 slotSize = new Vector2(96, 102);

    [SerializeField] Vector2 arcRadius = new Vector2(250, 220);
    [SerializeField] Vector2 portraitSize = new Vector2(64, 64);
    [SerializeField] Vector2 portraitOffset = new Vector2(0, 4);

    [Header("Positie")]
    [SerializeField] Camera cam;
    [SerializeField] Vector2 screenOffset;
    [SerializeField] bool scaleWithZoom = true;

    class Slot
    {
        public Image background;
        public Image portrait;
    }

    readonly List<Slot> slots = new List<Slot>();
    ProductionBuilding current;
    SpriteRenderer[] currentSprites;
    float referenceOrthoSize;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        referenceOrthoSize = cam.orthographicSize;
        
        plusButton.onClick.AddListener(AddWorker);
        minusButton.onClick.AddListener(RemoveWorker);
    }

    void Start() => root.SetActive(false);

    public void Show(ProductionBuilding building)
    {
        current = building;

        currentSprites = building.GetComponentsInChildren<SpriteRenderer>();

        Refresh();
        FollowBuilding();
        root.SetActive(true);
    }

    public void Hide()
    {
        if (current == null) return;
        current = null;
        root.SetActive(false);
    }

    public void AddWorker()
    {
        if (current != null) current.AddWorker();
    }

    public void RemoveWorker()
    {
        if (current != null) current.RemoveWorker();
    }
    
    public bool IsPointerOverHUD(Vector2 screenPos)
    {
        if (current == null || !root.activeInHierarchy) return false;
 
        foreach (var g in root.GetComponentsInChildren<Graphic>())
        {
            if (!g.enabled || !g.raycastTarget || !g.gameObject.activeInHierarchy) continue;
 
            Camera uiCam = g.canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : g.canvas.worldCamera;
            if (RectTransformUtility.RectangleContainsScreenPoint(g.rectTransform, screenPos, uiCam))
                return true;
        }
        return false;
    }

    
    void LateUpdate()
    {
        if (current == null)
        {
            if (root.activeSelf) root.SetActive(false);
            return;
        }

        Refresh();
        FollowBuilding();
    }

    void FollowBuilding()
    {
        Vector3 screen = cam.WorldToScreenPoint(GetSpriteCenter());
        root.transform.position = screen + (Vector3)screenOffset;

        if (scaleWithZoom)
            root.transform.localScale = Vector3.one * (referenceOrthoSize / cam.orthographicSize);
    }
    
    Vector3 GetSpriteCenter()
    {
        if (currentSprites == null || currentSprites.Length == 0)
            return current.transform.position;

        Bounds bounds = currentSprites[0].bounds;
        for (int i = 1; i < currentSprites.Length; i++)
            bounds.Encapsulate(currentSprites[i].bounds);
        return bounds.center;
    }

    void Refresh()
    {
        if (slots.Count != current.MaxWorkers) BuildSlots(current.MaxWorkers);
        
        string title = Regex.Replace(current.buildingType.ToString(), "(?<=[a-z])([A-Z])", " $1");
        if (!current.IsComplete) title += $" ({Mathf.RoundToInt(current.ConstructionProgress * 100)}%)";
        nameText.text = title;

        counterText.text = $"{current.WorkerCount}/{current.MaxWorkers}";
        minusButton.interactable = current.CanRemoveWorker;
        plusButton.interactable = current.CanAddWorker;

        UpdateSlots();
    }
    
    void BuildSlots(int count)
    {
        foreach (var s in slots) Destroy(s.background.gameObject);
        slots.Clear();

        for (int i = 0; i < count; i++)
        {
            GameObject slotGO = new GameObject("Slot", typeof(RectTransform), typeof(Image));
            slotGO.transform.SetParent(slotParent, false);

            RectTransform slotRect = (RectTransform)slotGO.transform;
            slotRect.sizeDelta = slotSize;
            
            float t = count > 1 ? i / (float)(count - 1) : 0.5f;
            float angle = Mathf.Lerp(180f, 0f, t) * Mathf.Deg2Rad;
            slotRect.anchoredPosition = new Vector2(
                Mathf.Cos(angle) * arcRadius.x,
                Mathf.Sin(angle) * arcRadius.y);

            GameObject portraitGO = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            portraitGO.transform.SetParent(slotGO.transform, false);

            Image portraitImage = portraitGO.GetComponent<Image>();
            portraitImage.preserveAspect = true;
            portraitImage.raycastTarget = false;

            RectTransform portraitRect = (RectTransform)portraitGO.transform;
            portraitRect.sizeDelta = portraitSize;
            portraitRect.anchoredPosition = portraitOffset;

            slots.Add(new Slot { background = slotGO.GetComponent<Image>(), portrait = portraitImage });
        }
    }

    void UpdateSlots()
    {
        bool hasPortraits = villagerPortraits != null && villagerPortraits.Length > 0;

        for (int i = 0; i < slots.Count; i++)
        {
            bool filled = i < current.WorkerCount;
            slots[i].background.sprite = filled ? filledSlot : emptySlot;

            bool showPortrait = filled && hasPortraits;
            slots[i].portrait.gameObject.SetActive(showPortrait);
            if (showPortrait)
                slots[i].portrait.sprite = villagerPortraits[i % villagerPortraits.Length];
        }
    }
}