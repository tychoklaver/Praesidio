using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float movementSpeed;
    [SerializeField] private float movementSmoothness;

    [Header("Edge Scrolling")]
    [SerializeField] private bool enableEdgeScrolling = true;
    [SerializeField] private float edgeThreshold;    
    [SerializeField] private float edgeScrollMaxZoom = 5f;

    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed;
    [SerializeField] private float zoomSmoothness;
    [SerializeField] private float minOrthographicSize;
    [SerializeField] private float maxOrthographicSize;


    private Camera cam;
    private Vector3 targetPos;
    private float targetZoom;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        targetPos = transform.position;
        targetZoom = cam.orthographicSize;
    }


    void Update()
    {
        HandleInput();
        HandleZoom();
        ApplyMovement();
    }

    private void HandleInput()
{
    Vector3 inputDirection = Vector3.zero;

    if (Keyboard.current != null)
    {
        if (Keyboard.current.wKey.isPressed) inputDirection.y += 1f;
        if (Keyboard.current.sKey.isPressed) inputDirection.y -= 1f;
        if (Keyboard.current.aKey.isPressed) inputDirection.x -= 1f;
        if (Keyboard.current.dKey.isPressed) inputDirection.x += 1f;
    }

    bool isZoomedEnoughIn = cam.orthographicSize <= edgeScrollMaxZoom;

    if (enableEdgeScrolling && isZoomedEnoughIn && Mouse.current != null)
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();

        if (mousePos.x >= Screen.width - edgeThreshold)
            inputDirection.x += 1f;
        else if (mousePos.x <= edgeThreshold)
            inputDirection.x -= 1f;

        if (mousePos.y >= Screen.height - edgeThreshold)
            inputDirection.y += 1f;
        else if (mousePos.y <= edgeThreshold)
            inputDirection.y -= 1f;
    }

    inputDirection = Vector3.ClampMagnitude(inputDirection, 1f);

    // Calculate world delta
    targetPos += inputDirection * movementSpeed * Time.deltaTime;
}

    private void HandleZoom()
    {
        if (Mouse.current == null) return;

     // Read scroll delta y-axis value
    float scrollDelta = Mouse.current.scroll.ReadValue().y;

    if (Mathf.Abs(scrollDelta) > 0.01f)
    {
        // Normalize scroll values across platforms (mouse scroll values are typically ~120 or ~1 per notch)
        float normalizedScroll = Mathf.Sign(scrollDelta);

        targetZoom -= normalizedScroll * zoomSpeed;
        targetZoom = Mathf.Clamp(targetZoom, minOrthographicSize, maxOrthographicSize);
    }
}

    private void ApplyMovement()
    {
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * movementSmoothness);
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime * zoomSmoothness);
    }
}
