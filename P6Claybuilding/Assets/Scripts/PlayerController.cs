using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float sensitivity = 2f; // Adjust for faster/slower look speed
    public Transform cameraTransform; // Assign your camera in the Inspector

    private Vector3 moveDirection;
    private Rigidbody rb;
    private float pitch = 0f; // Vertical rotation (clamped)

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    public void OnMoveUp(InputAction.CallbackContext context)
    {
        moveDirection.z = context.performed ? 1f : 0f;
    }

    public void OnMoveDown(InputAction.CallbackContext context)
    {
        moveDirection.z = context.performed ? -1f : 0f;
    }

    public void OnMoveLeft(InputAction.CallbackContext context)
    {
        moveDirection.x = context.performed ? -1f : 0f;
    }

    public void OnMoveRight(InputAction.CallbackContext context)
    {
        moveDirection.x = context.performed ? 1f : 0f;
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        Vector2 lookInput = context.ReadValue<Vector2>();
        float yaw = lookInput.x * sensitivity;  // Horizontal look
        float pitchChange = -lookInput.y * sensitivity;  // Vertical look (inverted)

        // Apply rotation
        transform.Rotate(Vector3.up, yaw);

        // Clamp vertical rotation to prevent flipping
        pitch = Mathf.Clamp(pitch + pitchChange, -90f, 90f);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void FixedUpdate()
    {
        // Get the camera's forward and right directions (in local space)
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        // Flatten them to prevent moving on the Y-axis (up/down)
        forward.y = 0f;
        right.y = 0f;

        // Normalize the vectors
        forward.Normalize();
        right.Normalize();

        // Combine the input movement with the camera's direction
        Vector3 move = forward * moveDirection.z + right * moveDirection.x;

        // Apply movement
        rb.MovePosition(rb.position + move * moveSpeed * Time.fixedDeltaTime);
    }
}
