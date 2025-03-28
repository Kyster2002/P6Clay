using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events; // For UnityEvent

public class VRMenuToggle : MonoBehaviour
{
    public Transform controllerTransform; // Assign RightController
    public CanvasGroup menuCanvasGroup; // Assign the VRMenu Canvas Group
    public Camera playerCamera; // Assign XR Origin's Main Camera
    public InputActionReference toggleAction; // New Input System Action Reference
    public float fadeSpeed = 2f; // Speed of fade animation
    public Vector3 menuOffset = new Vector3(0, 0, 0.2f); // Menu offset from controller
    private bool menuVisible = false;

    public UnityEvent onToggleMenu; // UnityEvent to link with PlayerInput

    void Start()
    {
        // Initialize menu to be invisible
        if (menuCanvasGroup == null)
        {
            Debug.LogError("Menu Canvas Group not assigned!");
            return;
        }

        menuCanvasGroup.alpha = 0f;
        menuCanvasGroup.blocksRaycasts = false;

        // Subscribe to button press event
        if (toggleAction != null)
        {
            toggleAction.action.performed += ToggleMenu;
        }
        else
        {
            Debug.LogError("Toggle Action is not assigned in the Inspector.");
        }
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (toggleAction != null)
        {
            toggleAction.action.performed -= ToggleMenu;
        }
    }

    public void ToggleMenu(InputAction.CallbackContext context)
    {
        menuVisible = !menuVisible;

        // Start Fade Animation
        StopAllCoroutines();
        StartCoroutine(FadeMenu(menuVisible ? 1f : 0f));

        // Enable interaction when visible
        menuCanvasGroup.blocksRaycasts = menuVisible;

        // Call the UnityEvent
        onToggleMenu.Invoke();
    }

    void Update()
    {
        // If the menu is active, position it in front of the controller
        if (menuVisible && controllerTransform != null && playerCamera != null)
        {
            Vector3 targetPosition = controllerTransform.position + controllerTransform.forward * menuOffset.z + controllerTransform.up * menuOffset.y;
            menuCanvasGroup.transform.position = targetPosition;

            // Make the canvas always face the player
            Vector3 directionToFace = playerCamera.transform.position - menuCanvasGroup.transform.position;
            directionToFace.y = 0; // Keep rotation in the Y-axis
            menuCanvasGroup.transform.rotation = Quaternion.LookRotation(-directionToFace);
        }
    }

    System.Collections.IEnumerator FadeMenu(float targetAlpha)
    {
        float startAlpha = menuCanvasGroup.alpha;
        float elapsedTime = 0f;

        while (elapsedTime < 1f)
        {
            menuCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime * fadeSpeed);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        menuCanvasGroup.alpha = targetAlpha;
    }
}
