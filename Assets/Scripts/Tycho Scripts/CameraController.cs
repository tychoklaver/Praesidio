using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// 2D orthographic camera: WASD panning, scroll-wheel zoom, 'H' to jump to the headquarters,
/// and clamping to a bounds box. Uses the new Input System.
/// Input only changes a TARGET position/zoom; the camera then eases towards it in LateUpdate.
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

    [Tooltip("The camera jumps here when 'H' is pressed.")]
    [SerializeField] private Transform headquartersTransform;

    private Camera _cam;
    private Vector3 _targetPos;
    private float _targetZoom;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        _targetPos = transform.position;
        _targetZoom = _cam.orthographicSize;
    }

    private void Update()
    {
        HandleInput();
        HandleZoom();

        // Clamp the TARGET (not the camera itself), so the smoothing can never overshoot the bounds.
        if (boundsBox != null) ApplyBounds();
    }

    // Smoothing runs in LateUpdate so objects that move in Update don't make the camera jitter.
    private void LateUpdate() => ApplyMovement();

    /// <summary>WASD pans the target; 'H' snaps it to the headquarters.</summary>
    private void HandleInput()
    {
        if (Keybinds.WasPressed(Keybinds.JumpToHeadquarters) && headquartersTransform != null)
        {
            Vector3 hq = headquartersTransform.position;
            _targetPos = new Vector3(hq.x, hq.y, transform.position.z);
            return;
        }
 
        var direction = new Vector3(Axis(Keybinds.MoveRight, Keybinds.MoveLeft), Axis(Keybinds.MoveUp, Keybinds.MoveDown));
 
        // Clamping the length keeps diagonal movement from being faster than straight movement.
        _targetPos += Vector3.ClampMagnitude(direction, 1f) * (movementSpeed * Time.deltaTime);
    }

    /// <summary>Returns +1, -1 or 0 from a pair of opposing keys.</summary>
    private static float Axis(int pPositive, int pNegative) =>
        (Keybinds.IsHeld(pPositive) ? 1f : 0f) - (Keybinds.IsHeld(pNegative) ? 1f : 0f);
    
    /// <summary>Scroll wheel changes the target orthographic size.</summary>
    private void HandleZoom()
    {
        if (Mouse.current == null) return;
 
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) < 0.01f) return;
 
        // Only the direction is used: scroll values differ per platform and device.
        _targetZoom = Mathf.Clamp(_targetZoom - Mathf.Sign(scroll) * zoomSpeed, minOrthographicSize, maxOrthographicSize);
    }

    /// <summary>
    /// Keeps the whole camera view inside the bounds box: the camera CENTER may only move within
    /// the box shrunk by half the view size (which depends on zoom and aspect ratio).
    /// </summary>
    private void ApplyBounds()
    {
        Bounds box = boundsBox.bounds;
 
        float halfHeight = _targetZoom;
        float halfWidth = halfHeight * _cam.aspect;
 
        float minX = box.min.x + halfWidth;
        float maxX = box.max.x - halfWidth;
        float minY = box.min.y + halfHeight;
        float maxY = box.max.y - halfHeight;
 
        // View larger than the box on an axis: no valid range, so center on the box.
        if (minX > maxX) minX = maxX = box.center.x;
        if (minY > maxY) minY = maxY = box.center.y;
 
        _targetPos.x = Mathf.Clamp(_targetPos.x, minX, maxX);
        _targetPos.y = Mathf.Clamp(_targetPos.y, minY, maxY);
    }

    /// <summary>
    /// Eases position and zoom towards their targets. 1 - e^(-k*dt) is a frame-rate independent
    /// lerp factor: the camera feels the same at 30 and 144 FPS.
    /// </summary>
    private void ApplyMovement()
    {
        float moveAlpha = 1f - Mathf.Exp(-movementSmoothness * Time.deltaTime);
        float zoomAlpha = 1f - Mathf.Exp(-zoomSmoothness * Time.deltaTime);
 
        transform.position = Vector3.Lerp(transform.position, _targetPos, moveAlpha);
        _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, _targetZoom, zoomAlpha);
    }
}