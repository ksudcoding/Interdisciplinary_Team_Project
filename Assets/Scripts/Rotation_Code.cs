using UnityEngine;
using UnityEngine.InputSystem;

public class Rotation_Code : MonoBehaviour
{
    public GameObject target;
    
    [Header("Orbit & View Settings")]
    public float mouseSensitivity = 2f;
    public float distanceFromTarget = 10f;
    
    [Range(20f, 80f)]
    public float elevationAngle = 45f; // Locked high angle looking down into the corner (NO SIDE-ON)

    [Header("Zoom Settings")]
    public float maxLimit = 25f;
    public float minLimit = 1f;
    public float scrollSensitivity = 5f;

    private float currentX = 0f;

    void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("Target is not assigned in Rotation_Code!");
            return;
        }

        UpdateCameraTransform();
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Spin horizontally around the target when holding the Right Mouse Button
        if (Mouse.current.rightButton.isPressed)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            currentX += mouseDelta.x * mouseSensitivity * 0.05f;
            // Vertical mouse movement is ignored, preventing any drop to side-on view
        }

        // 2. Smooth zoom with the mouse wheel (preserves the top-down angle)
        float scrollInput = Mouse.current.scroll.ReadValue().y;
        if (scrollInput != 0f)
        {
            distanceFromTarget -= scrollInput * scrollSensitivity * 0.1f;
            distanceFromTarget = Mathf.Clamp(distanceFromTarget, minLimit, maxLimit);
        }

        // 3. Apply position and rotation
        UpdateCameraTransform();
    }

    void UpdateCameraTransform()
    {
        // Use the fixed elevation angle for pitch and currentX for smooth horizontal rotation
        Quaternion rotation = Quaternion.Euler(elevationAngle, currentX, 0);

        // Position the camera at the set distance from the target along that angle
        Vector3 position = target.transform.position + rotation * new Vector3(0, 0, -distanceFromTarget);
        transform.position = position;

        // Keep the target locked dead-center in view
        transform.LookAt(target.transform.position);
    }
}