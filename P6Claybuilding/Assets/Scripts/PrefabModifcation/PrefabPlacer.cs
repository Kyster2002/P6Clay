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
    [SerializeField] float ghostSmoothSpeed = 15f; // tweakable in Inspector

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

    [Header("Valid Placement Tags")]
    public List<string> validSurfaceTags = new List<string>() { "Ground" }; // can edit in Inspector


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
        if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, placementLayers))
        {
            Debug.Log("No object hit by raycast.");
            return;
        }

        // Allow placement only on tagged surfaces
        string hitTag = hit.collider.transform.root.tag;
        if (!validSurfaceTags.Contains(hitTag))
        {
            Debug.Log($"Hit tag '{hitTag}' not allowed.");
            return;
        }

        // Now calculate snapped position relative to the surface
        Vector3 snappedPos;

        if (hit.collider.CompareTag("Ground"))
        {
            snappedPos = SnapToGrid(hit.point + Vector3.up * 0.01f); // flat grid on ground
        }
        else
        {
            snappedPos = GetSnapToSurfacePosition(hit, prefab); // edge snap for walls
        }

        Debug.Log($"Snapped ghost to {snappedPos} on hit '{hit.collider.name}'");

        // CREATE or MOVE ghost
        if (ghostInstance == null || ghostInstance.name != prefab.name + "(Ghost)")
        {
            if (ghostInstance != null) Destroy(ghostInstance);

            Quaternion targetRotation = Quaternion.LookRotation(-hit.normal); // look away from surface
            ghostInstance = Instantiate(prefab, snappedPos, targetRotation);
            ghostInstance.name = prefab.name + "(Ghost)";
            SetLayerRecursively(ghostInstance, LayerMask.NameToLayer("Ignore Raycast")); // ✅ Add this
            DisableColliders(ghostInstance);
            ApplyGhostMaterial(ghostInstance);

        }
        else
        {
            // ✅ This must run — or ghost stays in place forever!
            ghostInstance.transform.position = Vector3.Lerp(ghostInstance.transform.position, snappedPos, Time.deltaTime * ghostSmoothSpeed);

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

        Vector3 pos = ghostInstance.transform.position;
        Quaternion rot = ghostInstance.transform.rotation;

        Destroy(ghostInstance);
        ghostInstance = null;

        GameObject placed = Instantiate(spawnerRef.selectedPrefab, pos, rot);

        // ✅ Give it a tag that allows future placement on it
        placed.tag = "Ground"; // or whatever tag you put in `validSurfaceTags`

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

    Bounds GetBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(obj.transform.position, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
        {
            bounds.Encapsulate(r.bounds);
        }
        return bounds;
    }

    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }


    Vector3 GetSnapToSurfacePosition(RaycastHit hit, GameObject prefab)
    {
        Vector3 normal = hit.normal.normalized;
        Vector3 axis = GetMajorAxis(normal);

        Bounds prefabBounds = GetBounds(prefab);
        float offset = Vector3.Scale(prefabBounds.extents, axis).magnitude;

        Vector3 snapOffset = axis * offset;

        Vector3 pivotOffset = prefabBounds.center - prefab.transform.position;

        Vector3 snapped = hit.point + snapOffset - pivotOffset;

        return snapped;
    }



    Vector3 GetMajorAxis(Vector3 normal)
    {
        Vector3 abs = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));

        float threshold = 0.5f; // ✅ tweak this! smaller = stricter, bigger = more forgiving

        if (abs.x > threshold && abs.x >= abs.y && abs.x >= abs.z)
            return new Vector3(Mathf.Sign(normal.x), 0, 0);
        if (abs.y > threshold && abs.y >= abs.x && abs.y >= abs.z)
            return new Vector3(0, Mathf.Sign(normal.y), 0);
        return new Vector3(0, 0, Mathf.Sign(normal.z));
    }


}
