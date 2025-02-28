using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance;  // Singleton for easy global access
    private DripFillController selectedObject; // Currently selected object
    private Camera mainCamera;

    void Awake()
    {
        // Ensure only one instance exists (Singleton pattern)
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        mainCamera = Camera.main;
    }

    void Update()
    {
        // Detect left-clicks that are NOT on UI elements
        if (Mouse.current.leftButton.wasPressedThisFrame && !EventSystem.current.IsPointerOverGameObject())
        {
            DetectObjectUnderMouse();
        }
    }

    void DetectObjectUnderMouse()
    {
        // Cast a ray from the camera to the mouse position
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            DripFillController newSelection = hit.collider.GetComponent<DripFillController>();
            if (newSelection != null)
            {
                selectedObject = newSelection;
                Debug.Log($"✔ Selected: {selectedObject.gameObject.name}");
            }
        }
    }

    // Returns the currently selected object
    public DripFillController GetSelectedObject() => selectedObject;
}
