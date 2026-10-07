using UnityEngine;

public class MinimapCamera : MonoBehaviour
{
    [SerializeField] BoxCollider2D mapBounds;
    [SerializeField] Camera minimapCamera;

    void Start()
    {
        Bounds bounds = mapBounds.bounds;

        float aspect = (float)Screen.width / Screen.height;

        float width = bounds.size.x;
        float height = bounds.size.y;

        minimapCamera.orthographicSize =
            Mathf.Max(height, width / aspect) / 2f;

        minimapCamera.transform.position = new Vector3(
            bounds.center.x,
            bounds.center.y,
            minimapCamera.transform.position.z
        );
    }
}