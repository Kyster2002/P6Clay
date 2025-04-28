using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// VRMenuSpawner: Manages showing/hiding the VR menu canvas in response to a controller input,
/// and keeps it positioned and oriented relative to the player’s right hand and head.
/// </summary>
public class VRMenuSpawner : MonoBehaviour
{
    [Header("References")]
    /// <summary>Transform of the right-hand controller (where the menu should spawn near).</summary>
    public Transform rightController;
    /// <summary>Transform of the player’s head (used to orient the menu to face the player).</summary>
    public Transform headCamera;
    /// <summary>The UI canvas GameObject that contains the VR menus.</summary>
    public GameObject menuCanvas;
    /// <summary>Reference to the PrefabPlacer so it can enable/disable placement logic.</summary>
    public PrefabPlacer placer;
    /// <summary>Input action that toggles the menu’s visibility.</summary>
    public InputActionReference toggleMenuAction;

    [Header("Settings")]
    /// <summary>Local offset from the controller where the menu will appear.</summary>
    public Vector3 offset = new Vector3(0, 0.1f, 0.2f);
    /// <summary>Speed at which the menu canvas moves to follow the controller.</summary>
    public float followSpeed = 10f;

    // Tracks whether the menu is currently visible.
    private bool menuVisible = false;

    /// <summary>
    /// Unity Start callback: ensures the menu starts hidden and input action is enabled.
    /// </summary>
    void Start()
    {
        // Hide the menu on startup
        if (menuCanvas != null)
            menuCanvas.SetActive(false);

        // Make sure the input action map is enabled so ToggleMenu will fire
        if (toggleMenuAction != null && !toggleMenuAction.action.actionMap.enabled)
            toggleMenuAction.action.actionMap.Enable();
    }

    /// <summary>
    /// Subscribes to the toggleMenuAction when this component is enabled.
    /// </summary>
    void OnEnable()
    {
        if (toggleMenuAction != null)
            toggleMenuAction.action.performed += ToggleMenu;
    }

    /// <summary>
    /// Unsubscribes from the toggleMenuAction when this component is disabled.
    /// </summary>
    void OnDisable()
    {
        if (toggleMenuAction != null)
            toggleMenuAction.action.performed -= ToggleMenu;
    }

    /// <summary>
    /// Unity Update callback: if the menu is visible, smoothly moves it to follow
    /// the controller and rotates it to face the player’s head.
    /// </summary>
    void Update()
    {
        if (!menuVisible || menuCanvas == null || rightController == null || headCamera == null)
            return;

        // Calculate the target world position for the menu
        Vector3 targetPosition = rightController.position + rightController.TransformDirection(offset);
        // Smoothly interpolate the menu’s position
        menuCanvas.transform.position = Vector3.Lerp(
            menuCanvas.transform.position,
            targetPosition,
            Time.deltaTime * followSpeed
        );

        // Rotate the menu so it always faces the player (only around Y axis)
        Vector3 lookDirection = menuCanvas.transform.position - headCamera.position;
        lookDirection.y = 0;
        menuCanvas.transform.rotation = Quaternion.LookRotation(lookDirection);
    }

    /// <summary>
    /// Input callback to toggle the menu’s visibility on/off.
    /// Also informs the PrefabPlacer of the new state so it can enable/disable placement.
    /// </summary>
    void ToggleMenu(InputAction.CallbackContext ctx)
    {
        // Flip visibility flag
        menuVisible = !menuVisible;

        // Show or hide the canvas GameObject
        if (menuCanvas != null)
            menuCanvas.SetActive(menuVisible);

        // Let the PrefabPlacer know so it can disable ghost previews when menu closes
        if (placer != null)
            placer.SetMenuOpen(menuVisible);
    }
}
