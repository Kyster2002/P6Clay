using UnityEngine;
using UnityEngine.InputSystem;

public class VRMenuSpawner : MonoBehaviour
{
    [Header("References")]
    public Transform rightController; // The controller where the menu will spawn
    public Transform headCamera; // XR Origin's main camera
    public GameObject menuCanvas; // The world space UI canvas
    public InputActionReference toggleMenuAction; // Input action to toggle visibility

    [Header("Settings")]
    public Vector3 offset = new Vector3(0, 0.1f, 0.2f); // Offset from the controller
    public float followSpeed = 10f;

    private bool menuVisible = false;

    void Start()
    {
        if (menuCanvas != null)
            menuCanvas.SetActive(false);

        // Enable the action map if it's not managed by PlayerInput
        if (toggleMenuAction != null && !toggleMenuAction.action.actionMap.enabled)
        {
            toggleMenuAction.action.actionMap.Enable();
        }
    }


    void OnEnable()
    {
        if (toggleMenuAction != null)
            toggleMenuAction.action.performed += ToggleMenu;
    }

    void OnDisable()
    {
        if (toggleMenuAction != null)
            toggleMenuAction.action.performed -= ToggleMenu;
    }

    void Update()
    {
        if (!menuVisible || menuCanvas == null || rightController == null || headCamera == null)
            return;

        // Follow position above controller
        Vector3 targetPosition = rightController.position + rightController.TransformDirection(offset);
        menuCanvas.transform.position = Vector3.Lerp(menuCanvas.transform.position, targetPosition, Time.deltaTime * followSpeed);

        // Face the player’s head
        Vector3 lookDirection = menuCanvas.transform.position - headCamera.position;
        lookDirection.y = 0; // Optional: Y-axis only
        menuCanvas.transform.rotation = Quaternion.LookRotation(lookDirection);
    }

    void ToggleMenu(InputAction.CallbackContext ctx)
    {
        menuVisible = !menuVisible;

        if (menuCanvas != null)
        {
            menuCanvas.SetActive(menuVisible);
        }
    }
}
