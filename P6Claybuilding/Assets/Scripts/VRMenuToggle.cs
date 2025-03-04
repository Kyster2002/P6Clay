using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class VRMenuToggle : MonoBehaviour
{
    public Transform controllerTransform; // Assign the VR controller transform
    public CanvasGroup menuCanvasGroup; // Assign the CanvasGroup from the menu panel
    public Camera playerCamera; // Assign the player's camera
    public XRNode inputSource; // Set the XRNode to Left or Right Hand
    public InputHelpers.Button toggleButton = InputHelpers.Button.PrimaryButton; // Define the button to use for toggling the menu (e.g., PrimaryButton)
    public float fadeSpeed = 2f; // The speed of fading the menu in and out
    public Vector3 menuOffset = new Vector3(0, 0, 0.2f); // Adjust this to move the menu in front of the controller
    private bool menuVisible = false;

    void Start()
    {
        // Initialize menu to be invisible
        menuCanvasGroup.alpha = 0f;
        menuCanvasGroup.blocksRaycasts = false;
    }

    void Update()
    {
        // Get the input state from the controller (checking for button press)
        bool isPressed = false;
        InputHelpers.IsPressed(InputDevices.GetDeviceAtXRNode(inputSource), toggleButton, out isPressed);

        // Toggle the menu visibility if the button is pressed
        if (isPressed)
        {
            menuVisible = !menuVisible;

            // Smoothly fade in/out the menu
            float targetAlpha = menuVisible ? 1f : 0f;
            menuCanvasGroup.alpha = Mathf.Lerp(menuCanvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);

            // Enable interaction when visible
            menuCanvasGroup.blocksRaycasts = menuVisible;
        }

        // Position the menu in front of the controller when visible
        if (menuVisible)
        {
            Vector3 targetPosition = controllerTransform.position + controllerTransform.forward * menuOffset.z + controllerTransform.up * menuOffset.y;

            // Update menu position
            menuCanvasGroup.transform.position = targetPosition;

            // Make the canvas always face the user (camera)
            Vector3 directionToFace = playerCamera.transform.position - menuCanvasGroup.transform.position;
            directionToFace.y = 0; // Keep the rotation only in the Y-axis so it stays upright
            Quaternion targetRotation = Quaternion.LookRotation(directionToFace);
            menuCanvasGroup.transform.rotation = targetRotation;
        }
    }
}
