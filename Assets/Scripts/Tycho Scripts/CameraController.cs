using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles 2D orthographic camera movement, zooming, bounds clamping and target tracking.
/// Designed to work with the modern Unity Input system.
/// </summary>


[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("World units per second moved via WASD input.")]
    [SerializeField] private float movementSpeed = 20f;
    [Tooltip("Smoothing factor for camera position interpolation (higher = faster snap).")]
    [SerializeField] private float movementSmoothness = 10f;

    [Header("Zoom Settings")]
    [Tooltip("Orthographic size change per scroll wheel notch.")]
    [SerializeField] private float zoomSpeed = 2f;
    [Tooltip("Smoothing factor for orthographic size interpolation.")]
    [SerializeField] private float zoomSmoothness = 10f;
    [SerializeField] private float minOrthographicSize = 2f;
    [SerializeField] private float maxOrthographicSize = 12f;
    
    [Header("Camera Bounds")]
    [Tooltip("BoxCollider2D defining the outer boundaries the camera cannot leave.")]
    [SerializeField] private BoxCollider2D boundsBox;

    [SerializeField] private Transform headquartersTransform;

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
        // Gather the user input for movement and zooming.
        HandleInput();
        HandleZoom();

        // Ensure calculated target positions remain inside the allowed world bounds.
        if (boundsBox != null)
            ApplyBounds();
    }

    void LateUpdate()
    {
        // Apply smooth interpolation in LateUpdate to prevent jittering from objects moving in Update.
        ApplyMovement();
    }

    /// <summary>
    /// Processes keyboard inputs for WASD panning and snapping to objects.
    /// </summary>
    private void HandleInput()
    {
        if (Keyboard.current == null) return;

        // Instantly target the HQ position when 'H' is pressed.
        if (Keyboard.current.hKey.wasPressedThisFrame && headquartersTransform != null)
        {
            targetPos = new Vector3(headquartersTransform.position.x, headquartersTransform.position.y, transform.position.z);
            return;
        }

        Vector3 inputDirection = Vector3.zero;

        if (Keyboard.current.wKey.isPressed) inputDirection.y += 1f;
        if (Keyboard.current.sKey.isPressed) inputDirection.y -= 1f;
        if (Keyboard.current.aKey.isPressed) inputDirection.x -= 1f;
        if (Keyboard.current.dKey.isPressed) inputDirection.x += 1f;

        // Prevent diagonal movement from exceeding a magnitude of 1 (faster speed vector)/
        inputDirection = Vector3.ClampMagnitude(inputDirection, 1f);

        // Accumulate movement velocity onto target destination.
        targetPos += inputDirection * movementSpeed * Time.deltaTime;
    }

    /// <summary>
    /// Reads mouse scroll wheel delta to adjust target orthographic size.
    /// </summary>
    private void HandleZoom()
    {
        if (Mouse.current == null) return;

        // Read scroll delta y-axis value
        float scrollDelta = Mouse.current.scroll.ReadValue().y;

        if (Mathf.Abs(scrollDelta) > 0.01f)
        {
            // Normalize scroll values across platforms.
            float normalizedScroll = Mathf.Sign(scrollDelta);

            targetZoom -= normalizedScroll * zoomSpeed;
            targetZoom = Mathf.Clamp(targetZoom, minOrthographicSize, maxOrthographicSize);
        }
    }

    /// <summary>
    /// Calculates camera view extents dynamically based on zoom and aspect ratio,
    /// clamping target position to stay within the boundaries.
    /// </summary>
    private void ApplyBounds()
    {
        Bounds boxBounds = boundsBox.bounds;
        
        // Calculate half extents of the camera view in world units.
        float vertExtent = targetZoom;
        float horizontalExtent = vertExtent * cam.aspect;

        // Derive inner clamping boundaries accounting for viewport scale.
        float minX = boxBounds.min.x + horizontalExtent;
        float maxX = boxBounds.max.x - horizontalExtent;
        float minY = boxBounds.min.y + vertExtent;
        float maxY = boxBounds.max.y - vertExtent;

        // Handle cases where the camera view is larger than the bounds box itself.
        if (minX > maxX) minX = maxX = boxBounds.center.x;
        if (minY > maxY) minY = maxY = boxBounds.center.y;

        // Enforce boundaries on desired position.
        targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
        targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);
    }

    /// <summary>
    /// Smoothly interpolates the camera position and zoom towards target values,
    /// using frame-rate independent dampening.
    /// </summary>
    private void ApplyMovement()
    {
        // Frame-rate independent exponential smoothing factor.
        float moveAlpha = 1f - Mathf.Exp(-movementSmoothness * Time.deltaTime);
        float zoomAlpha = 1f - Mathf.Exp(-zoomSmoothness * Time.deltaTime);

        transform.position = Vector3.Lerp(transform.position, targetPos, moveAlpha);
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, zoomAlpha);
    }
}
