using UnityEngine;
using UnityEngine.InputSystem;

public class ParallaxSystem : MonoBehaviour
{
    [System.Serializable]
    public class Layer
    {
        public Transform target;
        public float strength = 0.1f;
    }

    [SerializeField] Layer[] layers;
    [SerializeField] float maxOffset = 2f;
    [SerializeField] float smoothSpeed = 5f;

    Vector3[] startPositions;

    void Start()
    {
        startPositions = new Vector3[layers.Length];

        for (int i = 0; i < layers.Length; i++)
            startPositions[i] = layers[i].target.localPosition;
    }

    void Update()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        float mouseX = mousePosition.x / Screen.width * 2f - 1f;
        float mouseY = mousePosition.y / Screen.height * 2f - 1f;

        for (int i = 0; i < layers.Length; i++)
        {
            Vector3 targetPosition = startPositions[i];

            targetPosition.x -= mouseX * maxOffset * layers[i].strength;
            targetPosition.y -= mouseY * maxOffset * layers[i].strength;

            layers[i].target.localPosition = Vector3.Lerp(
                layers[i].target.localPosition,
                targetPosition,
                Time.deltaTime * smoothSpeed
            );
        }
    }
}