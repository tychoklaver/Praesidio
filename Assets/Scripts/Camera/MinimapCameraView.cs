using UnityEngine;

public class MinimapCameraView : MonoBehaviour
{
    [SerializeField] Camera mainCamera;
    [SerializeField] Camera minimapCamera;
    [SerializeField] RectTransform cameraViewImage;
    [SerializeField] BoxCollider2D mapBounds;

    void LateUpdate()
    {
        Bounds bounds = mapBounds.bounds;

        Vector3 cameraWorldPosition = mainCamera.transform.position;

        Vector3 minimapPosition = minimapCamera.WorldToViewportPoint(cameraWorldPosition);

        cameraViewImage.anchorMin = minimapPosition;
        cameraViewImage.anchorMax = minimapPosition;
        cameraViewImage.anchoredPosition = Vector2.zero;

        float cameraWorldHeight = mainCamera.orthographicSize * 2f;
        float cameraWorldWidth = cameraWorldHeight * mainCamera.aspect;

        float width = cameraWorldWidth / bounds.size.x;
        float height = cameraWorldHeight / bounds.size.y;

        cameraViewImage.sizeDelta = new Vector2(
            cameraViewImage.parent.GetComponent<RectTransform>().rect.width * width,
            cameraViewImage.parent.GetComponent<RectTransform>().rect.height * height
        );
    }
}