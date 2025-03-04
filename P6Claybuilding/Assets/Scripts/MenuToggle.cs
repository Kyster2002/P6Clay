using UnityEngine;

public class MenuToggle : MonoBehaviour
{
    public GameObject menuPanel; // Assign the menu panel in the inspector

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab)) // Detect keypress
        {
            bool isActive = !menuPanel.activeSelf; // Toggle state
            menuPanel.SetActive(isActive);

            // Unlock or lock the cursor based on menu state
            Cursor.lockState = isActive ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isActive;
        }
    }
}
