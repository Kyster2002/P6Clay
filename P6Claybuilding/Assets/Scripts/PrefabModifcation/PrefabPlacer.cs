using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PrefabPlacer : MonoBehaviour
{
    [Header("References")]
    public Transform rayOrigin; // e.g., Left Controller
    public PrefabButtonSpawner spawnerRef; // Assigned in inspector

    [Header("Input")]
    public InputActionReference placeAction; // Left trigger
    public InputActionReference undoAction;  // Left secondary button

    [Header("Placement Settings")]
    public float maxRayDistance = 100f;
    public LayerMask placementLayers;
    public float gridSize = 0.5f;
    public Material ghostMaterial;

    private GameObject ghostInstance;
    private GameObject lastPlacedInstance;
    public bool isMenuOpen = true;
    private List<GameObject> placedObjects = new List<GameObject>();

    [Header("Wall Selection")]
    public InputActionReference highlightAction; // assign in Inspector
    public LayerMask wallLayer; // choose in Inspector
    public Color highlightColor = Color.yellow;

    private GameObject selectedWall;
    private Material originalWallMaterial;


    private void OnEnable()
    {
        if (highlightAction != null) highlightAction.action.performed += OnHighlightWall;
        if (placeAction != null) placeAction.action.performed += OnPlace;
        if (undoAction != null) undoAction.action.performed += OnUndo;
    }

    private void OnDisable()
    {
        if (highlightAction != null) highlightAction.action.performed -= OnHighlightWall;
        if (placeAction != null) placeAction.action.performed -= OnPlace;
        if (undoAction != null) undoAction.action.performed -= OnUndo;
    }

    void Start()
    {
        if (placeAction != null)
        {
            Debug.Log($"Place action enabled: {placeAction.action.enabled}");
            placeAction.action.Enable();
        }

        if (undoAction != null)
        {
            Debug.Log($"Undo action enabled: {undoAction.action.enabled}");
            undoAction.action.Enable();
        }

        if (highlightAction != null)
        {
            highlightAction.action.Enable();
            Debug.Log($"[WallSelector] Highlight action enabled: {highlightAction.action.enabled}");
        }
    }


    private void Update()
    {
        HandleGhostPreview();

        if (placeAction != null && placeAction.action.triggered)
        {
            Debug.Log("Place action triggered in Update()");
        }

    }

    void HandleGhostPreview()
    {
        if (!isMenuOpen) return;

        GameObject prefab = spawnerRef.selectedPrefab;
        if (prefab == null || rayOrigin == null) return;

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, placementLayers)) return;

        Vector3 snappedPos = SnapToGrid(hit.point + Vector3.up * 0.01f);

        if (ghostInstance == null || ghostInstance.name != prefab.name + "(Ghost)")
        {
            if (ghostInstance != null) Destroy(ghostInstance);

            ghostInstance = Instantiate(prefab, snappedPos, prefab.transform.rotation);
            ghostInstance.name = prefab.name + "(Ghost)";
            DisableColliders(ghostInstance);
            ApplyGhostMaterial(ghostInstance);
        }
        else
        {
            ghostInstance.transform.position = snappedPos;
        }
    }

    public void ClearGhost()
    {


        if (ghostInstance != null)
        {
            Destroy(ghostInstance);
            ghostInstance = null;
            Debug.Log("Ghost cleared due to menu close");
        }
    }



    void OnPlace(InputAction.CallbackContext context)
    {
        Debug.Log("Place button pressed");

        if (spawnerRef.selectedPrefab == null)
        {
            Debug.LogWarning("No prefab selected!");
            return;
        }

        if (ghostInstance == null)
        {
            Debug.LogWarning("Ghost instance missing!");
            return;
        }

        Debug.Log("Attempting to place at ghost position...");

        // Test collider state
        Collider[] ghostColliders = ghostInstance.GetComponentsInChildren<Collider>();
        Debug.Log($"Ghost has {ghostColliders.Length} colliders");
        foreach (Collider col in ghostColliders)
        {
            Debug.Log($"{col.name} collider enabled: {col.enabled}");
        }

        // Force destroy ghost before placing
        Vector3 pos = ghostInstance.transform.position;
        Quaternion rot = ghostInstance.transform.rotation;

        Destroy(ghostInstance);
        ghostInstance = null;

        GameObject placed = Instantiate(spawnerRef.selectedPrefab, pos, rot);
        lastPlacedInstance = placed;
        placedObjects.Add(placed);

        Debug.Log($"Placed prefab at {pos}");
    }

    void OnUndo(InputAction.CallbackContext context)
    {
        Debug.Log("Undo button pressed");

        if (placedObjects.Count > 0)
        {
            GameObject last = placedObjects[placedObjects.Count - 1];
            if (last != null)
            {
                Destroy(last);
                Debug.Log("Undid last placed object");
            }
            placedObjects.RemoveAt(placedObjects.Count - 1);
        }
        else
        {
            Debug.LogWarning("Nothing to undo!");
        }

    }


    Vector3 SnapToGrid(Vector3 pos)
    {
        float x = Mathf.Round(pos.x / gridSize) * gridSize;
        float y = Mathf.Round(pos.y / gridSize) * gridSize;
        float z = Mathf.Round(pos.z / gridSize) * gridSize;
        return new Vector3(x, y, z);
    }

    void DisableColliders(GameObject obj)
    {
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
    }

    void ApplyGhostMaterial(GameObject instance)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.material = ghostMaterial;
        }
    }

    public void ForceRefreshGhost()
    {
        if (ghostInstance != null)
        {
            Destroy(ghostInstance);
            ghostInstance = null;
        }
    }

    public void SetMenuOpen(bool open)
    {
        isMenuOpen = open;

        if (!open)
        {
            ClearGhost();
        }
    }

    void OnHighlightWall(InputAction.CallbackContext ctx)
    {
        Debug.Log("Wall highlight triggered");

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.yellow, 2f);

        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, wallLayer))
        {
            Transform root = hit.collider.transform.root;
            GameObject wall = root.gameObject;
            Debug.Log("Hit object: " + hit.collider.gameObject.name);
            Debug.Log("Root object: " + hit.collider.transform.root.name);

            Debug.Log("Wall hit: " + wall.name);

            // Clear previous selection if any
            if (selectedWall != null && originalWallMaterial != null)
            {
                Renderer[] prevRenderers = selectedWall.GetComponentsInChildren<Renderer>();
                foreach (Renderer r in prevRenderers)
                {
                    r.material = originalWallMaterial;
                }
                Debug.Log("Previous wall material restored.");
            }

            // Select new
            Renderer[] renderers = wall.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.LogWarning("No renderers found on selected wall.");
                return;
            }

            selectedWall = wall;
            originalWallMaterial = renderers[0].material; // store one of them for restoration

            foreach (Renderer rend in renderers)
            {
                Material highlightMat = new Material(rend.material); // clone to break shared instance
                highlightMat.color = highlightColor;
                rend.material = highlightMat;

                Debug.Log($"Renderer: {rend.name}, Material changed.");
            }
        }
        else
        {
            Debug.Log("No wall hit.");
        }
    }

}
