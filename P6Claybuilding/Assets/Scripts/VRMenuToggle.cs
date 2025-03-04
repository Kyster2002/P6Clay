using UnityEngine;
using UnityEngine.XR;

public class VRMenuToggle : MonoBehaviour
{
    public Transform controllerTransform; // Assign the VR controller transform
    public CanvasGroup menuCanvasGroup; // Assign the CanvasGroup from the menu panel
    public float fadeSpeed = 2f;
    private bool menuVisible = false;

    void Update()
    {
        Vector3 controllerUp = controllerTransform.up; // Get the controller's up direction

        // Check if the controller is turned around (adjust the angle as needed)
        if (Vector3.Dot(controllerUp, Vector3.up) > 0.7f)
        {
            menuVisible = true;
        }
        else
        {
            menuVisible = false;
        }

        // Smoothly fade in/out the menu
        float targetAlpha = menuVisible ? 1f : 0f;
        menuCanvasGroup.alpha = Mathf.Lerp(menuCanvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
        menuCanvasGroup.blocksRaycasts = menuVisible; // Enable interaction when visible
    }
}
