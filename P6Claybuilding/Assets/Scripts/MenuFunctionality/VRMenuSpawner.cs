using UnityEngine;
using UnityEngine.InputSystem;

public class VRMenuSpawner : MonoBehaviour
{
    [Header("References")]
    public Transform rightController;
    public Transform headCamera;
    public GameObject menuCanvas;
    public PrefabPlacer placer; // << Added this
    public InputActionReference toggleMenuAction;

    [Header("Settings")]
    public Vector3 offset = new Vector3(0, 0.1f, 0.2f);
    public float followSpeed = 10f;

    private bool menuVisible = false;

    void Start()
    {
        if (menuCanvas != null)
            menuCanvas.SetActive(false);

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

        Vector3 targetPosition = rightController.position + rightController.TransformDirection(offset);
        menuCanvas.transform.position = Vector3.Lerp(menuCanvas.transform.position, targetPosition, Time.deltaTime * followSpeed);

        Vector3 lookDirection = menuCanvas.transform.position - headCamera.position;
        lookDirection.y = 0;
        menuCanvas.transform.rotation = Quaternion.LookRotation(lookDirection);
    }

    void ToggleMenu(InputAction.CallbackContext ctx)
    {
        menuVisible = !menuVisible;

        if (menuCanvas != null)
        {
            menuCanvas.SetActive(menuVisible);
        }

        if (placer != null)
        {
            placer.SetMenuOpen(menuVisible); // << stable!
        }
    }
}
