using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;


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
    private PlayerReferenceResolver references;

    private IEnumerator Start()
    {
        // First, do your existing stuff
        if (menuCanvas != null)
            menuCanvas.SetActive(false);

        if (toggleMenuAction != null && !toggleMenuAction.action.actionMap.enabled)
        {
            toggleMenuAction.action.actionMap.Enable();
        }

        // Now, get the references
        references = GetComponent<PlayerReferenceResolver>();

        if (references == null)
        {
            Debug.LogError("❌ PlayerReferenceResolver not found on this player!");
            yield break;
        }

        // Wait until references are ready
        while (!references.AreReferencesResolved)
        {
            yield return null;
        }

        Debug.Log("✅ VRMenuSpawner is now linked to PlayerReferences!");

        // Supply the references
        rightController = references.rightController;
        headCamera = references.headCamera;
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
