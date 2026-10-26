using UnityEngine;
using UnityEngine.EventSystems;

public class SkillTreeDrag : MonoBehaviour, IDragHandler, IScrollHandler
{
    [SerializeField] RectTransform content;
    [SerializeField] Canvas canvas;
    [SerializeField] float maxDistance = 700;
    [SerializeField] float zoomStep = 0.1f;
    [SerializeField] float minZoom = 0.6f;
    [SerializeField] float maxZoom = 1.6f;
    [SerializeField] float zoomSpeed = 12f;

    float targetZoom = 1;

    void Update()
    {
        float zoom = Mathf.Lerp(content.localScale.x, targetZoom, Time.unscaledDeltaTime * zoomSpeed);
        content.localScale = Vector3.one * zoom;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        Vector2 position = content.anchoredPosition + eventData.delta / canvas.scaleFactor;
        position.x = Mathf.Clamp(position.x, -maxDistance, maxDistance);
        position.y = Mathf.Clamp(position.y, -maxDistance, maxDistance);
        content.anchoredPosition = position;
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (eventData.scrollDelta.y > 0)
        {
            targetZoom += zoomStep;
        }
        if (eventData.scrollDelta.y < 0)
        {
            targetZoom -= zoomStep;
        }
        targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
    }
}
