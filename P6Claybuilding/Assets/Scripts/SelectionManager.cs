using UnityEngine;
using UnityEngine.InputSystem; // ✅ New Input System
using UnityEngine.EventSystems;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance;  // ✅ Singleton for global access
    private DripFillController selectedObject; // ✅ Stores the currently selected object
    private Camera mainCamera;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        mainCamera = Camera.main;
    }

    void Update()
    {
        // 🔹 Check for a click using the new Input System
        if (Mouse.current.leftButton.wasPressedThisFrame && !EventSystem.current.IsPointerOverGameObject())
        {
            DetectObjectUnderMouse();
        }
    }

    void DetectObjectUnderMouse()
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            DripFillController newSelection = hit.collider.GetComponent<DripFillController>();

            if (newSelection != null)
            {
                selectedObject = newSelection;
                Debug.Log($"✔ Selected Object: {selectedObject.gameObject.name}");
            }
        }
    }

    public DripFillController GetSelectedObject()
    {
        return selectedObject;
    }
}
