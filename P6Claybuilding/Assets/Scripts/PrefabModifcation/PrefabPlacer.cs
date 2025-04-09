using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System;
using System.Collections;


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
    public bool isMenuOpen = false;
    private List<GameObject> placedObjects = new List<GameObject>();

    [Header("Wall Selection")]
    public InputActionReference highlightAction;   // -> Drag "Select" action here
    public LayerMask wallLayer;
    public Color highlightColor = Color.yellow;
    public WallFillSlider wallFillSlider;

    private GameObject selectedWall;
    private Material originalWallMaterial;

    [Header("Valid Placement Tags")]
    public List<string> validSurfaceTags = new List<string>() { "Ground" }; // can edit in Inspector

    [Header("Rotation Control")]
    public InputActionReference rotateAction; // assign a trigger or grip in Inspector
    public float rotateSpeed = 90f; // degrees per second
    public float faceBias = 0.005f;

    private float rotationHoldTime = 0.5f;
    private float rotateTimer = 0f;
    private bool hasLaidDown = false;
    private Quaternion manualRotation = Quaternion.identity;

    [Header("Visual Ray")]
    public LineRenderer visualRay;
    public float rayLength = 10f;
    public VRMenuSpawner menuSpawnerRef; // << ✅ ADD THIS

    [Header("Menu References")]
    public GameObject prefabMenu;      // Drag your PrefabMenu here
    public GameObject animationMenu;   // Drag your AnimationMenu here
    private Dictionary<Renderer, Material[]> originalWallMaterials = new Dictionary<Renderer, Material[]>();
    private Dictionary<Renderer, Color[]> originalWallColors = new Dictionary<Renderer, Color[]>();

    private void OnEnable()
    {

        placeAction.action.performed += OnPlace;
        undoAction.action.performed += OnUndo;
        rotateAction.action.performed += OnRotate;
        highlightAction.action.performed += OnHighlightWall;
    }


    private void OnDisable()
    {
        placeAction.action.performed -= OnPlace;
        undoAction.action.performed -= OnUndo;
        rotateAction.action.performed -= OnRotate;
        highlightAction.action.performed -= OnHighlightWall; // ✅ ADD THIS
    }



    void Start()
    {
        StartCoroutine(DisableLaserNextFrame()); // << 🛡️ new

    }
    private IEnumerator DisableLaserNextFrame()
    {
        yield return null; // 🕓 wait 1 frame

        if (visualRay != null)
        {
            visualRay.enabled = false;
           
        }
    }


    private void Update()
    {

        UpdateVisualRay();
        HandleGhostPreview();
        HandleRotationInput();


        if (rotateAction != null && rotateAction.action.IsPressed())
        {
            rotateTimer += Time.deltaTime;

            if (rotateTimer >= rotationHoldTime && !hasLaidDown)
            {
                hasLaidDown = true;
                if (ghostInstance != null)
                {
                    Vector3 forward = ghostInstance.transform.forward;
                    Vector3 right = ghostInstance.transform.right;

                    // Lay down flat on the most logical axis
                    Vector3 layDownAxis = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > Mathf.Abs(Vector3.Dot(right, Vector3.up))
                        ? forward
                        : right;

                    manualRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(layDownAxis, Vector3.up), Vector3.up);
                }
            }
        }
        else if (rotateAction != null && rotateAction.action.WasReleasedThisFrame())
        {
            if (!hasLaidDown)
            {
                manualRotation *= Quaternion.Euler(0, 90, 0); // Y-axis rotate
            }

            rotateTimer = 0f;
            hasLaidDown = false;
        }


        if (placeAction != null && placeAction.action.triggered)
        {
            Debug.Log("Place action triggered in Update()");
        }

        if (rotateAction != null && rotateAction.action.triggered)
        {
            // 90° Y rotation on press
            ghostInstance.transform.Rotate(Vector3.up, 90f);
        }

    }



    void OnRotate(InputAction.CallbackContext ctx)
    {
        if (ghostInstance == null) return;

        // Rotate the ghost 90 degrees on the Y-axis (Y-axis for this case)
        ghostInstance.transform.Rotate(Vector3.up, 90f, Space.World);

        // Update the stored rotation
        manualRotation = ghostInstance.transform.rotation;  // Save the rotation
        Debug.Log("Rotated 90° via .performed");
    }


    void OnPlace(InputAction.CallbackContext context)
    {
        TryPlace();
    }



    void HandleGhostPreview()
    {
        if (!isMenuOpen) return;

        GameObject prefab = spawnerRef.selectedPrefab;
        if (prefab == null || rayOrigin == null) return;
       


        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        Debug.DrawRay(ray.origin, ray.direction * 1000f, Color.green, 0.1f);

        if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, placementLayers))
        {
            return;
        }

        string hitTag = hit.collider.transform.root.tag;
        if (!validSurfaceTags.Contains(hitTag)) return;

        Vector3 snappedPos;

        if (hit.collider.CompareTag("Ground"))
        {
            snappedPos = SnapToGrid(hit.point + Vector3.up * 0.01f);
        }
        else
        {
            snappedPos = GetSnapToSurfacePosition(hit, prefab, ray.direction);
        }

        // Create ghost if missing
        if (ghostInstance == null)
        {
            ghostInstance = Instantiate(prefab, snappedPos, prefab.transform.rotation); // Regular spawn
            ghostInstance.name = prefab.name + "(Ghost)";

            SetLayerRecursively(ghostInstance, LayerMask.NameToLayer("Ignore Raycast"));
            DisableColliders(ghostInstance);
            ApplyGhostMaterial(ghostInstance);

            // Apply saved rotation immediately after creating ghost
            ghostInstance.transform.rotation = manualRotation;
        }

        // Always update ghost position and rotation smoothly
        if (ghostInstance != null)
        {
            ghostInstance.transform.position = Vector3.Lerp(
                ghostInstance.transform.position,
                snappedPos,
                Time.deltaTime * ghostSmoothSpeed
            );

            ghostInstance.transform.rotation = manualRotation; // Always enforce manualRotation here
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

    void OnHighlightWall(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
            return; // Only react when the button is actually pressed

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        Debug.DrawRay(ray.origin, ray.direction * 1000f, Color.yellow, 2f);

        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, wallLayer))
        {
            Transform root = hit.collider.transform.root;
            GameObject wall = root.gameObject;
            Debug.Log("🎯 Hit object: " + hit.collider.gameObject.name);
            Debug.Log("📦 Root object: " + root.name);
            Debug.Log("🏰 Wall hit: " + wall.name);

            // If the same wall is selected again, deselect it.
            if (selectedWall == wall)
            {
                Debug.Log("🔄 Same wall selected again, deselecting.");
                DeselectWall();
                return;
            }

            // Check that the wall is on the "Walls" layer.
            if (wall.layer == LayerMask.NameToLayer("Walls"))
            {
                Debug.Log("✅ Wall is on 'Walls' layer, ready for animation.");

                // Always deselect any previously selected wall.
                DeselectWall();

                selectedWall = wall;
                Renderer[] renderers = wall.GetComponentsInChildren<Renderer>();

                if (renderers.Length == 0)
                {
                    Debug.LogWarning("⚠️ No renderers found on selected wall.");
                    return;
                }

                // Clear and save original color values for every Renderer.
                originalWallColors.Clear();
                foreach (Renderer rend in renderers)
                {
                    Color[] savedColors = new Color[rend.materials.Length];
                    for (int i = 0; i < rend.materials.Length; i++)
                    {
                        // If the material has _BaseColor, use that; otherwise, use its color property.
                        if (rend.materials[i].HasProperty("_BaseColor"))
                            savedColors[i] = rend.materials[i].GetColor("_BaseColor");
                        else
                            savedColors[i] = rend.materials[i].color;
                    }
                    originalWallColors[rend] = savedColors;
                }

                // Now modify the existing material instances to show the highlight color.
                foreach (Renderer rend in renderers)
                {
                    for (int i = 0; i < rend.materials.Length; i++)
                    {
                        if (rend.materials[i].HasProperty("_BaseColor"))
                            rend.materials[i].SetColor("_BaseColor", highlightColor);
                        else
                            rend.materials[i].color = highlightColor;
                    }
                }

                // Assign DripFillController to WallFillSlider
                DripFillController dripFill = selectedWall.GetComponent<DripFillController>();
                if (wallFillSlider != null)
                {
                    wallFillSlider.SetDripFillController(dripFill);
                }
                else
                {
                    Debug.LogWarning("⚠️ No WallFillSlider assigned in PrefabPlacer!");
                }

                // Hide the ghost while the wall is selected and animating
                if (ghostInstance != null)
                    ghostInstance.SetActive(false);

                prefabMenu.SetActive(false);
                animationMenu.SetActive(true);
            }
            else
            {
                Debug.LogWarning("❌ Wall is NOT on 'Walls' layer. No animation menu shown.");
            }
        }
        else
        {
            Debug.Log("🚫 No wall hit. Deselecting any selected wall.");
            DeselectWall();
        }
    }
    private void DeselectWall()
    {
        if (selectedWall != null)
        {
            // Restore the original colors on all renderers.
            foreach (var pair in originalWallColors)
            {
                Renderer rend = pair.Key;
                Color[] origColors = pair.Value;
                if (rend == null) continue;

                Material[] mats = rend.materials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i].HasProperty("_BaseColor"))
                        mats[i].SetColor("_BaseColor", origColors[i]);
                    else
                        mats[i].color = origColors[i];
                }
            }
            Debug.Log("🔄 All original materials restored after deselection.");
        }

        selectedWall = null;
        originalWallColors.Clear();
        SetupWallForFillSlider(selectedWall);

        // Restore ghost visibility
        if (ghostInstance != null)
            ghostInstance.SetActive(true);

        prefabMenu.SetActive(true);
        animationMenu.SetActive(false);
    }


    void SetupWallForFillSlider(GameObject wall)
    {
        if (wall == null || wallFillSlider == null) return;

        DripFillController drip = wall.GetComponentInChildren<DripFillController>();
        if (drip != null)
        {
            wallFillSlider.SetDripFillController(drip);
            Debug.Log("🔗 WallFillSlider updated to control new selected wall.");
        }
        else
        {
            Debug.LogWarning("⚠️ Selected wall has no DripFillController!");
        }
    }

    public void RestartSelectedWallDrip()
    {
        if (selectedWall != null)
        {
            DripFillController drip = selectedWall.GetComponentInChildren<DripFillController>();
            if (drip != null)
            {
                drip.ResetDripEffect();
                Debug.Log("🔄 Restarted drip animation on selected wall.");
            }
            else
            {
                Debug.LogWarning("⚠️ No DripFillController found on selected wall.");
            }
        }
    }

    public void StartFillAnimationOnSelectedWall()
    {
        if (selectedWall != null)
        {
            DripFillController drip = selectedWall.GetComponentInChildren<DripFillController>();
            if (drip != null)
            {
                drip.StartSmoothFillWithoutDrip();
                Debug.Log("⏳ Started smooth fill (no drip).");
            }
            else
            {
                Debug.LogWarning("⚠️ No DripFillController found on selected wall.");
            }
        }
    }

    void TryPlace()
    {
        if (spawnerRef.selectedPrefab == null || ghostInstance == null) return;

        // Lock in final transform before destroying the ghost
        Vector3 finalPos = ghostInstance.transform.position;
        Quaternion finalRot = ghostInstance.transform.rotation;

        finalPos = SnapToGrid(finalPos);

        Destroy(ghostInstance);
        ghostInstance = null;

        GameObject placed = Instantiate(spawnerRef.selectedPrefab, finalPos, finalRot);
        placed.tag = "Ground";

        lastPlacedInstance = placed;
        placedObjects.Add(placed);

        manualRotation = finalRot;

        // Call OnPlaced to disable particle system after placement
        DripFillController controller = placed.GetComponent<DripFillController>();
        if (controller != null)
        {
            controller.OnPlaced();
        }
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

            if (visualRay != null)
                visualRay.enabled = false; // << ADD THIS
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


    Vector3 GetSnapToSurfacePosition(RaycastHit hit, GameObject prefab, Vector3 rayDirection)
    {
        Bounds prefabBounds = GetBounds(prefab);
        Bounds targetBounds = hit.collider.bounds;

        Vector3 faceDirection;

        // 👇 Determine if we should bias towards the top surface
        bool aimingDown = Vector3.Dot(rayDirection, Vector3.down) > 0.5f;
        float nearTop = Mathf.Abs(hit.point.y - targetBounds.max.y);
        bool closeToTop = nearTop < 0.2f; // 20 cm "top assist" zone — tweak if needed

        if ((aimingDown && nearTop < 1f) || closeToTop)
        {
            // If aiming down very close to top, or just near top in general, place on top
            faceDirection = Vector3.up;
        }
        else
        {
            // Regular snapping logic
            faceDirection = GetMajorAxisWithThreshold(hit.normal, rayDirection);
        }

        // 👇 Calculate offsets
        float prefabOffset = Vector3.Scale(prefabBounds.extents, faceDirection).magnitude;
        float targetOffset = Vector3.Scale(targetBounds.extents, faceDirection).magnitude;

        // 👇 Final position = target center + total offset - pivot correction
        Vector3 snapOffset = faceDirection * (prefabOffset + targetOffset);
        Vector3 pivotOffset = prefabBounds.center - prefab.transform.position;

        Vector3 snapped = hit.collider.transform.position + snapOffset - pivotOffset;
        // Optional anti-overlap correction (fixed version)
        float nudgeAmount = 0.01f; // 1 cm upward each step
        int maxAttempts = 20;

        for (int i = 0; i < maxAttempts; i++)
        {
            Collider[] colliders = Physics.OverlapBox(
                snapped, // <- just snapped position
                prefabBounds.extents * 0.9f,
                Quaternion.identity,
                placementLayers
            );

            if (colliders.Length == 0)
            {
                // No overlaps, position is good
                break;
            }

            // Still overlapping -> nudge upward
            snapped += Vector3.up * nudgeAmount;
        }

        return snapped;
    }

    void TryAssistTopPlacement(RaycastHit baseHit)
    {
        Vector3 assistStart = baseHit.point + rayOrigin.forward * 0.2f; // small nudge forward
        Ray assistRay = new Ray(assistStart, Vector3.up);

        if (Physics.Raycast(assistRay, out RaycastHit assistHit, 2f, placementLayers))
        {
            if (Vector3.Dot(assistHit.normal, Vector3.up) > 0.75f)
            {
                // Found a flat top!
                Debug.Log("Assist hit top surface!");

                // Here you can override the snap position if you want
            }
        }
    }


    void EnableColliders(GameObject obj)
    {
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = true;
        }
    }



    Vector3 GetMajorAxisWithThreshold(Vector3 normal, Vector3 rayDirection)
    {
        Vector3 abs = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));
        float threshold = 0.25f;

        if (Vector3.Dot(normal, Vector3.up) > 0.75f)
        {
            // Surface is facing upwards strongly -> Treat it as a top surface
            return Vector3.up;
        }


        // Regular snapping based on surface normal
        if (abs.x > abs.y && abs.x > abs.z && abs.x > threshold)
            return new Vector3(Mathf.Sign(normal.x), 0, 0);
        if (abs.y > abs.x && abs.y > abs.z && abs.y > threshold)
            return new Vector3(0, Mathf.Sign(normal.y), 0);
        if (abs.z > threshold)
            return new Vector3(0, 0, Mathf.Sign(normal.z));

        return Vector3.up; // fallback
    }

    void HandleRotationInput()
    {
        if (rotateAction != null && rotateAction.action.triggered && ghostInstance != null)
        {
            ghostInstance.transform.Rotate(Vector3.up, 90f, Space.World);
            manualRotation = ghostInstance.transform.rotation;  // Store new rotation
            Debug.Log("Rotated ghost 90°");
        }
    }
    void UpdateVisualRay()
    {
        if (visualRay == null || rayOrigin == null || menuSpawnerRef == null)
            return;

        // Only enable if the menu is actually visible
        bool shouldEnableLaser = isMenuOpen && menuSpawnerRef.menuCanvas != null && menuSpawnerRef.menuCanvas.activeSelf;

        if (visualRay.enabled != shouldEnableLaser)
        {
            visualRay.enabled = shouldEnableLaser;
            visualRay.gameObject.SetActive(shouldEnableLaser);
            Debug.LogWarning(shouldEnableLaser ? "🟢 Laser ENABLED" : "🔴 Laser DISABLED");
        }

        if (shouldEnableLaser)
        {
            Vector3 start = rayOrigin.position;
            Vector3 end = start + rayOrigin.forward * rayLength;
            visualRay.SetPosition(0, start);
            visualRay.SetPosition(1, end);
        }
    }


}
