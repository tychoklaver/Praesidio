using UnityEngine;

public class MinimapCamera : MonoBehaviour
{
    [SerializeField] BoxCollider2D mapBounds;
    [SerializeField] Camera minimapCamera;

    void Start()
    {
        UpdateBounds();
    }

    void Update()
    {
        UpdateBounds();
    }

    void UpdateBounds()
    {
        float height = minimapCamera.orthographicSize * 2f;
        float width = height * minimapCamera.aspect;

        mapBounds.size = new Vector2(width, height);

        mapBounds.transform.position = minimapCamera.transform.position;
    }
}