using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Vector3 moveDirection;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    public void OnMoveUp(InputAction.CallbackContext context)
    {
        if (context.performed) moveDirection.z = 1f;
        else if (context.canceled) moveDirection.z = 0f;
    }

    public void OnMoveDown(InputAction.CallbackContext context)
    {
        if (context.performed) moveDirection.z = -1f;
        else if (context.canceled) moveDirection.z = 0f;
    }

    public void OnMoveLeft(InputAction.CallbackContext context)
    {
        if (context.performed) moveDirection.x = -1f;
        else if (context.canceled) moveDirection.x = 0f;
    }

    public void OnMoveRight(InputAction.CallbackContext context)
    {
        if (context.performed) moveDirection.x = 1f;
        else if (context.canceled) moveDirection.x = 0f;
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + moveDirection.normalized * moveSpeed * Time.fixedDeltaTime);
    }
}
