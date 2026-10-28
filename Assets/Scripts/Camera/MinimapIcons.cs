using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapIcons : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Camera minimapCamera;
    [SerializeField] RectTransform minimap;
    [SerializeField] Sprite unitSprite;
    [SerializeField] Sprite buildingSprite;
    [SerializeField] LayerMask enemyLayer;

    [Header("Colors")]
    [SerializeField] Color playerColor = new Color32(96, 168, 226, 255);
    [SerializeField] Color enemyColor = new Color32(214, 72, 58, 255);
    [SerializeField] Color workerColor = new Color32(240, 226, 200, 255);
    [SerializeField] Color buildingColor = new Color32(226, 178, 92, 255);

    [Header("Size")]
    [SerializeField] float unitSize = 16;
    [SerializeField] float buildingSize = 20;
    [SerializeField] float groupDistance = 22;
    [SerializeField] float groupGrow = 2;
    [SerializeField] int maxGrow = 4;

    List<Image> icons = new List<Image>();
    List<Vector2> groupPositions = new List<Vector2>();
    List<int> groupCounts = new List<int>();
    int used;

    void LateUpdate()
    {
        used = 0;

        List<Vector3> players = new List<Vector3>();
        List<Vector3> enemies = new List<Vector3>();
        foreach (UnitHealth unit in UnitHealth.all)
        {
            if (unit.isDead)
            {
                continue;
            }

            if ((enemyLayer.value & (1 << unit.gameObject.layer)) != 0)
            {
                enemies.Add(unit.transform.position);
            }
            else
            {
                players.Add(unit.transform.position);
            }
        }

        List<Vector3> workers = new List<Vector3>();
        foreach (Worker worker in FindObjectsByType<Worker>(FindObjectsSortMode.None))
        {
            workers.Add(worker.transform.position);
        }

        List<Vector3> buildings = new List<Vector3>();
        foreach (Building building in FindObjectsByType<Building>(FindObjectsSortMode.None))
        {
            if (building.enabled)
            {
                buildings.Add(building.transform.position);
            }
        }

        DrawGroups(buildings, buildingSprite, buildingColor, buildingSize);
        DrawGroups(workers, unitSprite, workerColor, unitSize);
        DrawGroups(players, unitSprite, playerColor, unitSize);
        DrawGroups(enemies, unitSprite, enemyColor, unitSize);

        for (int i = used; i < icons.Count; i++)
        {
            icons[i].gameObject.SetActive(false);
        }
    }

    void DrawGroups(List<Vector3> positions, Sprite sprite, Color color, float size)
    {
        groupPositions.Clear();
        groupCounts.Clear();
        Vector2 mapSize = minimap.rect.size;

        foreach (Vector3 position in positions)
        {
            Vector3 viewport = minimapCamera.WorldToViewportPoint(position);
            if (viewport.x < 0 || viewport.x > 1 || viewport.y < 0 || viewport.y > 1)
            {
                continue;
            }

            Vector2 point = new Vector2(viewport.x * mapSize.x, viewport.y * mapSize.y);
            bool added = false;
            for (int i = 0; i < groupPositions.Count; i++)
            {
                if (Vector2.Distance(groupPositions[i], point) < groupDistance)
                {
                    int count = groupCounts[i];
                    groupPositions[i] = (groupPositions[i] * count + point) / (count + 1);
                    groupCounts[i] = count + 1;
                    added = true;
                    break;
                }
            }

            if (!added)
            {
                groupPositions.Add(point);
                groupCounts.Add(1);
            }
        }

        for (int i = 0; i < groupPositions.Count; i++)
        {
            Image icon = GetIcon();
            icon.sprite = sprite;
            icon.color = color;
            float grow = Mathf.Min(groupCounts[i] - 1, maxGrow) * groupGrow;
            icon.rectTransform.sizeDelta = new Vector2(size + grow, size + grow);
            icon.rectTransform.anchoredPosition = groupPositions[i];
        }
    }

    Image GetIcon()
    {
        if (used == icons.Count)
        {
            GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(minimap, false);
            Image image = iconObject.GetComponent<Image>();
            image.raycastTarget = false;
            image.rectTransform.anchorMin = Vector2.zero;
            image.rectTransform.anchorMax = Vector2.zero;
            icons.Add(image);
        }

        Image icon = icons[used];
        icon.gameObject.SetActive(true);
        used++;
        return icon;
    }
}
