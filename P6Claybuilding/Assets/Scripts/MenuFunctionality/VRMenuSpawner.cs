using UnityEngine;
using UnityEngine.InputSystem;

public class VRMenuSpawner : MonoBehaviour
{
    [Header("References")]
    public PlayerReferenceResolver references;
    public InputActionReference toggleMenuAction;

    [Header("Menu Prefab")]
    [SerializeField] private GameObject menuPrefab;
    public GameObject menuCanvas;

    private bool menuVisible = false;
    private bool menuSpawned = false;
    private Transform rightController;
    private Transform headCamera;

    [Header("Follow Settings")]
    public Vector3 offset = new Vector3(0, 0f, 1f);
    public float followSpeed = 10f;

    private void Start()
    {
        references = GetComponent<PlayerReferenceResolver>();
        references.OnReferencesReady += OnReferencesReady;

        if (toggleMenuAction != null && !toggleMenuAction.action.actionMap.enabled)
            toggleMenuAction.action.actionMap.Enable();

        toggleMenuAction.action.performed += ToggleMenu;
    }

    private void Update()
    {
        if (!menuVisible || !menuSpawned || menuCanvas == null || rightController == null || headCamera == null)
            return;

        Vector3 targetPosition = rightController.position + rightController.TransformDirection(offset);
        menuCanvas.transform.position = Vector3.Lerp(menuCanvas.transform.position, targetPosition, Time.deltaTime * followSpeed);

        Vector3 lookDirection = menuCanvas.transform.position - headCamera.position;
        lookDirection.y = 0;
        if (lookDirection != Vector3.zero)
            menuCanvas.transform.rotation = Quaternion.LookRotation(lookDirection);
    }

    private void OnReferencesReady()
    {
        rightController = references.rightController;
        headCamera = references.headCamera;

        // 🛠️ Dynamically setup PrefabPlacer
        PrefabPlacer placer = FindObjectOfType<PrefabPlacer>();
        if (placer != null)
        {
            placer.rayOrigin = references.rayOrigin;
            placer.spawnerRef = references.spawnerRef;
            placer.wallFillSlider = references.wallFillSlider;
        }
        else
        {
            Debug.LogError("❌ PrefabPlacer not found in scene!");
        }

        SpawnMenu();
    }


    private void ToggleMenu(InputAction.CallbackContext ctx)
    {
        if (!menuSpawned)
            return;

        menuVisible = !menuVisible;
        if (menuCanvas != null)
            menuCanvas.SetActive(menuVisible);
    }

    private void SpawnMenu()
    {
        if (menuPrefab == null)
            return;

        menuCanvas = Instantiate(menuPrefab);
        menuCanvas.name = "SpawnedMenu";

        menuCanvas.transform.SetParent(null);
        menuCanvas.transform.localScale = Vector3.one;
        menuCanvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;

        if (headCamera != null)
            menuCanvas.GetComponent<Canvas>().worldCamera = headCamera.GetComponent<Camera>();

        menuCanvas.SetActive(true);
        menuSpawned = true;
        menuVisible = true;

        // 🛠️ NEW: Initialize button spawner
        var buttonSpawner = menuCanvas.GetComponentInChildren<PrefabButtonSpawner>();
        if (buttonSpawner != null)
        {
            buttonSpawner.Initialize();

            // 🛠️ OPTIONAL: Link to PrefabPlacer if needed
            var prefabPlacer = FindObjectOfType<PrefabPlacer>();
            if (prefabPlacer != null)
                prefabPlacer.spawnerRef = buttonSpawner;
        }
    }


    private void OnDestroy()
    {
        if (toggleMenuAction != null)
            toggleMenuAction.action.performed -= ToggleMenu;
    }
}
