using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System;
using System.Collections;
using TMPro;

// PrefabPlacer: Handles VR-based placement of wall prefabs, ghost preview,
// selection, rotation, undo, and highlight effects.
public class PrefabPlacer : MonoBehaviour
{
    [Header("Valid Placement Tags")]
    // Surfaces tagged with these values can have prefabs placed on them.
    public List<string> validSurfaceTags = new List<string>() { "Ground" };

    [Header("Highlight Settings")]
    // Multiplier for vertical scaling of highlight pillars.
    public float highlightYScaleMultiplier = 1.0f;
    // UI text displaying the current wall variant.
    public TMP_Text wallToggleLabel;

    [Header("Highlight Settings")]
    // Duration (in seconds) for the highlight fade effect.
    private float highlightDuration = 2f;
    // Reference to the active highlight coroutine to allow stopping it.
    private Coroutine highlightFadeCoroutine;
    // List of currently instantiated corner highlight objects.
    private List<GameObject> currentCornerHighlights = new List<GameObject>();

    [Header("References")]
    // Origin point for placement raycasts (e.g., controller transform).
    public Transform rayOrigin;
    // Component that spawns buttons and tracks which prefab is selected.
    public PrefabButtonSpawner spawnerRef;
    // Slider component used to control fill level on the selected wall.
    public WallFillSlider wallFillSlider;

    [Header("Input")]
    // Input actions for placement, undo, rotation, and highlighting.
    public InputActionReference placeAction;
    public InputActionReference undoAction;
    public InputActionReference rotateAction;
    public InputActionReference highlightAction;

    [Header("Placement Settings")]
    // Maximum distance for placement raycasts.
    public float maxRayDistance = 100f;
    // Layers considered valid for placement targets.
    public LayerMask placementLayers;
    // Grid size for snapping placed objects.
    public float gridSize = 0.5f;
    // Material applied to the ghost preview instance.
    public Material ghostMaterial;
    // Speed factor for smoothing ghost movement.
    [SerializeField] float ghostSmoothSpeed = 15f;

    // Instance of the current ghost preview.
    private GameObject ghostInstance;
    // Reference to the last placed object in the scene.
    private GameObject lastPlacedInstance;
    // Flag indicating whether the VR menu is open.
    public bool isMenuOpen = false;
    // List of all placed objects for undo and bulk deletion.
    private List<GameObject> placedObjects = new List<GameObject>();

    [Header("Wall Selection")]
    // Layer used for placed wall objects to enable selection raycasts.
    public LayerMask wallLayer;
    // Color applied to a wall when it is highlighted.
    public Color highlightColor = Color.yellow;

    // Currently selected wall GameObject.
    private GameObject selectedWall;
    // Stores original materials for each renderer on the selected wall.
    private Dictionary<Renderer, Material[]> originalWallMaterials = new Dictionary<Renderer, Material[]>();
    // Stores original base colors for fading highlight effects.
    private Dictionary<Renderer, Color[]> originalWallColors = new Dictionary<Renderer, Color[]>();

    [Header("Menu References")]
    // UI panels for prefab selection versus animation controls.
    public GameObject prefabMenu;
    public GameObject animationMenu;
    // Component responsible for positioning the VR menu.
    public VRMenuSpawner menuSpawnerRef;

    [Header("Rotation Control")]
    // Degrees per second to rotate the ghost on continuous input.
    public float rotateSpeed = 90f;
    // Threshold for determining face orientation when auto-laying down.
    public float faceBias = 0.005f;
    // Time required to hold rotation input before auto-laying down.
    private float rotationHoldTime = 0.5f;
    // Timer tracking duration of the current rotation-input hold.
    private float rotateTimer = 0f;
    // Flag indicating whether the ghost has been laid flat during this hold.
    private bool hasLaidDown = false;
    // Quaternion storing the ghost’s manual rotation state.
    private Quaternion manualRotation = Quaternion.identity;


    [Header("Visual Ray")]
    // LineRenderer used to display a laser pointer.
    public LineRenderer visualRay;
    // Length of the visual ray laser.
    public float rayLength = 10f;

    /// <summary>
    /// Subscribes to the VR input action callbacks when this component is enabled.
    /// </summary>
    private void OnEnable()
    {
        // Subscribe to input action events when this component is enabled.
        placeAction.action.performed += OnPlace;
        undoAction.action.performed += OnUndo;
        rotateAction.action.performed += OnRotate;
        highlightAction.action.performed += OnHighlightWall;
    }

    /// <summary>
    /// Unsubscribes from the VR input action callbacks when this component is disabled.
    /// </summary>
    private void OnDisable()
    {
        // Unsubscribe from input action events to prevent memory leaks.
        placeAction.action.performed -= OnPlace;
        undoAction.action.performed -= OnUndo;
        rotateAction.action.performed -= OnRotate;
        highlightAction.action.performed -= OnHighlightWall; // ensure highlight unhooked
    }

    /// <summary>
    /// Called once at the start of the scene. Begins a coroutine to disable the laser pointer next frame.
    /// </summary>
    void Start()
    {
        // Disable the visual ray on the very next frame to avoid flicker.
        StartCoroutine(DisableLaserNextFrame());
    }

    /// <summary>
    /// Coroutine that waits one frame, then disables the LineRenderer used as the laser pointer.
    /// </summary>
    private IEnumerator DisableLaserNextFrame()
    {
        // Wait one frame before disabling.
        yield return null;

        if (visualRay != null)
        {
            visualRay.enabled = false;
        }
    }

    /// <summary>
    /// Called every frame. Updates the laser pointer, manages the ghost preview and rotation input,
    /// and handles quick-triggered place and rotate actions.
    /// </summary>
    private void Update()
    {
        // Handle per-frame updates: ray visibility, ghost preview, rotation logic.
        UpdateVisualRay();
        HandleGhostPreview();
        HandleRotationInput();

        // Track how long the rotate button has been held down.
        if (rotateAction != null && rotateAction.action.IsPressed())
        {
            rotateTimer += Time.deltaTime;

            // After hold threshold, automatically lay the ghost flat.
            if (rotateTimer >= rotationHoldTime && !hasLaidDown)
            {
                hasLaidDown = true;
                if (ghostInstance != null)
                {
                    Vector3 forward = ghostInstance.transform.forward;
                    Vector3 right = ghostInstance.transform.right;

                    // Choose the axis most aligned with up/down to lay the ghost flat.
                    Vector3 layDownAxis = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > Mathf.Abs(Vector3.Dot(right, Vector3.up))
                        ? forward
                        : right;

                    // Compute rotation that flattens along the chosen axis.
                    manualRotation = Quaternion.LookRotation(
                        Vector3.ProjectOnPlane(layDownAxis, Vector3.up),
                        Vector3.up
                    );
                }
            }
        }
        else if (rotateAction != null && rotateAction.action.WasReleasedThisFrame())
        {
            // If rotate button released before laying down, apply a quick 90° turn.
            if (!hasLaidDown)
            {
                manualRotation *= Quaternion.Euler(0, 90, 0);
            }

            // Reset timers and flags for next hold.
            rotateTimer = 0f;
            hasLaidDown = false;
        }

        // Debug log if place action fires here (often handled in OnPlace).
        if (placeAction != null && placeAction.action.triggered)
        {
            Debug.Log("Place action triggered in Update()");
        }

        // Also allow rotateAction.triggered to rotate instantly.
        if (rotateAction != null && rotateAction.action.triggered)
        {
            ghostInstance.transform.Rotate(Vector3.up, 90f);
        }
    }

    /// <summary>
    /// Callback for the rotateAction performed event: instantly rotates the ghost by 90° around Y axis
    /// and saves that rotation for subsequent frames.
    /// </summary>
    void OnRotate(InputAction.CallbackContext ctx)
    {
        if (ghostInstance == null) return;

        // Rotate the ghost 90° around world Y axis in response to the input event.
        ghostInstance.transform.Rotate(Vector3.up, 90f, Space.World);

        // Store the new rotation so it persists in Update().
        manualRotation = ghostInstance.transform.rotation;
        Debug.Log("Rotated 90° via .performed");
    }

    /// <summary>
    /// Callback for the placeAction performed event: attempts to place the selected prefab at the ghost position.
    /// </summary>
    void OnPlace(InputAction.CallbackContext context)
    {
        // Delegate to TryPlace() to instantiate and finalize the placement.
        TryPlace();
    }

    /// <summary>
    /// Updates or creates the ghost preview based on raycasting from the controller,
    /// snapping to valid surfaces and applying smoothing and manual rotation.
    /// </summary>
    void HandleGhostPreview()
    {
        // Only update ghost if the menu is open.
        if (!isMenuOpen) return;

        GameObject prefab = spawnerRef.selectedPrefab;
        if (prefab == null || rayOrigin == null) return;

        // Cast a ray forward from the controller to find where to position the ghost.
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        Debug.DrawRay(ray.origin, ray.direction * 1000f, Color.green, 0.1f);

        // Exit if no valid surface hit within max distance.
        if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, placementLayers))
            return;

        string hitTag = hit.collider.transform.root.tag;
        if (!validSurfaceTags.Contains(hitTag)) return;

        Vector3 snappedPos;

        // Snap to grid if placing on ground, otherwise use more complex surface logic.
        if (hit.collider.CompareTag("Ground"))
        {
            snappedPos = SnapToGrid(hit.point + Vector3.up * 0.01f);
        }
        else
        {
            snappedPos = GetSnapToSurfacePosition(hit, prefab, ray.direction);
        }

        // Create the ghost instance on first valid hit.
        if (ghostInstance == null)
        {
            ghostInstance = Instantiate(prefab, snappedPos, prefab.transform.rotation);
            ghostInstance.name = prefab.name + "(Ghost)";

            // Prevent ghost from interfering with raycasts and collisions.
            SetLayerRecursively(ghostInstance, LayerMask.NameToLayer("Ignore Raycast"));
            DisableColliders(ghostInstance);
            ApplyGhostMaterial(ghostInstance);

            // Apply any previously determined manual rotation.
            ghostInstance.transform.rotation = manualRotation;
        }

        // Smoothly interpolate ghost position and apply stored rotation every frame.
        if (ghostInstance != null)
        {
            ghostInstance.transform.position = Vector3.Lerp(
                ghostInstance.transform.position,
                snappedPos,
                Time.deltaTime * ghostSmoothSpeed
            );

            ghostInstance.transform.rotation = manualRotation;
        }
    }



    /// <summary>
    /// Destroys the ghost preview GameObject if it exists, and clears the reference.
    /// Useful for cleaning up when closing menus or canceling placement.
    /// </summary>
    public void ClearGhost()
    {
        if (ghostInstance != null)
        {
            Destroy(ghostInstance);
            ghostInstance = null;
            Debug.Log("Ghost cleared due to menu close");
        }
    }

    /// <summary>
    /// Coroutine that applies a quick “flash” highlight to the given wall:
    /// first lerps all materials from their original colors to the highlightColor,
    /// then back to the originals over the total duration.
    /// </summary>
    private IEnumerator FlashHighlight(GameObject wall)
    {
        if (wall == null) yield break;

        // Gather all renderers on the wall
        Renderer[] renderers = wall.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) yield break;

        float elapsed = 0f;
        float halfDuration = highlightDuration / 2f;

        // --- Fade in to highlightColor ---
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);

            foreach (Renderer rend in renderers)
            {
                // Skip any renderer we didn’t record original colors for
                if (!originalWallColors.ContainsKey(rend)) continue;

                Color[] originalColors = originalWallColors[rend];
                for (int i = 0; i < rend.materials.Length; i++)
                {
                    // Determine start and end colors
                    Color fromColor = (i < originalColors.Length) ? originalColors[i] : Color.white;
                    Color toColor = highlightColor;
                    // Interpolate between them
                    Color lerpedColor = Color.Lerp(fromColor, toColor, t);

                    // Apply to _BaseColor if available, otherwise to material.color
                    if (rend.materials[i].HasProperty("_BaseColor"))
                        rend.materials[i].SetColor("_BaseColor", lerpedColor);
                    else
                        rend.materials[i].color = lerpedColor;
                }
            }

            yield return null;
        }

        // Reset timer for fade-out
        elapsed = 0f;

        // --- Fade out back to original colors ---
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);

            foreach (Renderer rend in renderers)
            {
                if (!originalWallColors.ContainsKey(rend)) continue;

                Color[] originalColors = originalWallColors[rend];
                for (int i = 0; i < rend.materials.Length; i++)
                {
                    // Swap from/to for reverse lerp
                    Color fromColor = highlightColor;
                    Color toColor = (i < originalColors.Length) ? originalColors[i] : Color.white;
                    Color lerpedColor = Color.Lerp(fromColor, toColor, t);

                    if (rend.materials[i].HasProperty("_BaseColor"))
                        rend.materials[i].SetColor("_BaseColor", lerpedColor);
                    else
                        rend.materials[i].color = lerpedColor;
                }
            }

            yield return null;
        }

        // Clear reference so new highlight can start fresh
        highlightFadeCoroutine = null;
    }

    /// <summary>
    /// Generates small cylindrical pillars at each bottom corner of the wall mesh
    /// to visually indicate selection. Pillars are half the wall height and colored.
    /// </summary>
    void CreateCornerHighlights(GameObject wall)
    {
        if (wall == null) return;

        // Try to find the mesh root under either variant
        Transform meshRoot = FindDeepChild(wall.transform, "Box")
                           ?? FindDeepChild(wall.transform, "Box_Closed");
        if (meshRoot == null)
        {
            Debug.LogWarning($"❌ Could not find 'Box' or 'Box_Closed' on {wall.name}");
            return;
        }

        MeshFilter meshFilter = meshRoot.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogWarning($"❌ No MeshFilter or mesh found on {meshRoot.name}");
            return;
        }

        Mesh mesh = meshFilter.sharedMesh;
        Bounds meshBounds = mesh.bounds;
        Transform meshTransform = meshFilter.transform;

        // Calculate the four bottom-corner positions in local space
        Vector3[] localCorners = new Vector3[4];
        float yBase = 0f;
        localCorners[0] = new Vector3(meshBounds.min.x, yBase, meshBounds.min.z);
        localCorners[1] = new Vector3(meshBounds.max.x, yBase, meshBounds.min.z);
        localCorners[2] = new Vector3(meshBounds.min.x, yBase, meshBounds.max.z);
        localCorners[3] = new Vector3(meshBounds.max.x, yBase, meshBounds.max.z);

        // Determine pillar height as half the wall height
        float fullHeight = meshBounds.size.y * meshTransform.localScale.y;
        float pillarHeight = fullHeight * 0.5f;

        foreach (Vector3 localCorner in localCorners)
        {
            // Convert to world, then back to wall-local
            Vector3 worldPos = meshTransform.TransformPoint(localCorner);
            Vector3 localToWall = wall.transform.InverseTransformPoint(worldPos);

            // Create a thin cylinder at that corner
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.transform.SetParent(wall.transform);
            pillar.transform.localPosition = localToWall + new Vector3(0f, pillarHeight, 0f);
            pillar.transform.localScale = new Vector3(0.02f, pillarHeight, 0.02f);

            // Color it and remove its collider
            pillar.GetComponent<Renderer>().material.color = Color.yellow;
            Destroy(pillar.GetComponent<Collider>());

            currentCornerHighlights.Add(pillar);
        }
    }

    /// <summary>
    /// Recursively searches through a transform’s children for one with the exact given name.
    /// Returns null if not found.
    /// </summary>
    private Transform FindDeepChild(Transform parent, string exactName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == exactName)
                return child;

            Transform result = FindDeepChild(child, exactName);
            if (result != null)
                return result;
        }
        return null;
    }

    /// <summary>
    /// Toggles between the “Box” and “Box_Closed” child variants of the currently selected wall:
    /// 1) deactivates the current one and activates the other,
    /// 2) tells DripFillController to switch its target box,
    /// 3) preserves the current fill level,
    /// 4) reparents and reactivates any drip particles on the new box.
    /// </summary>
    public void ToggleWallBoxVersion()
    {
        if (selectedWall == null)
        {
            Debug.LogWarning("❌ No wall is currently selected to toggle.");
            return;
        }

        // Find both box variants
        Transform box = FindDeepChild(selectedWall.transform, "Box");
        Transform boxClosed = FindDeepChild(selectedWall.transform, "Box_Closed");
        if (box == null || boxClosed == null)
        {
            Debug.LogWarning("⚠️ Either 'Box' or 'Box_Closed' is missing under the selected wall.");
            return;
        }

        bool wasBoxActive = box.gameObject.activeSelf;
        bool wasBoxClosedActive = boxClosed.gameObject.activeSelf;
        Debug.Log($"🧩 Before toggle: Box active = {wasBoxActive}, Box_Closed active = {wasBoxClosedActive}");

        // Activate the opposite variant
        Transform newTarget;
        if (wasBoxActive)
        {
            box.gameObject.SetActive(false);
            boxClosed.gameObject.SetActive(true);
            newTarget = boxClosed;
        }
        else if (wasBoxClosedActive)
        {
            boxClosed.gameObject.SetActive(false);
            box.gameObject.SetActive(true);
            newTarget = box;
        }
        else
        {
            // Fallback if both were inactive
            box.gameObject.SetActive(true);
            boxClosed.gameObject.SetActive(false);
            newTarget = box;
        }

        // Switch the DripFillController to the new box while keeping fill level
        DripFillController drip = selectedWall.GetComponent<DripFillController>();
        if (drip != null)
        {
            float previousFill = drip.FillLevel;
            drip.SetTargetBox(newTarget);
            drip.SetFillLevel(previousFill);

            // Reparent and show drip particles on the new box
            if (drip.dripParticles != null)
            {
                drip.dripParticles.transform.SetParent(newTarget, worldPositionStays: false);
                drip.dripParticles.transform.localPosition = new Vector3(0, 0.5f, 0);
                drip.dripParticles.gameObject.SetActive(true);
                Debug.Log("✅ Re-parented and re-activated dripParticles on the new box.");
            }

            Debug.Log("✅ Updated DripFillController to match toggled box with preserved fill.");
        }

        Debug.Log($"✅ After toggle: Box = {box.gameObject.activeSelf}, Box_Closed = {boxClosed.gameObject.activeSelf}");
    }
    /// <summary>
    /// Casts a ray to detect walls when the highlight action is performed.
    /// If a wall is hit:
    ///  - Toggles selection if the same wall is clicked again.
    ///  - Deselects any previous wall, then selects the new one:
    ///      • Saves original materials
    ///      • Creates corner highlight pillars
    ///      • Switches from prefab menu to animation menu
    ///      • Starts a flash highlight effect
    /// </summary>
    void OnHighlightWall(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
            return;

        // Fire a yellow debug ray from the controller
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        Debug.DrawRay(ray.origin, ray.direction * 1000f, Color.yellow, 2f);

        // Only consider hits on objects in the wallLayer mask
        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, wallLayer))
        {
            GameObject wall = hit.collider.transform.root.gameObject;

            // If clicking the already-selected wall, deselect it
            if (selectedWall == wall)
            {
                DeselectWall();
                return;
            }

            // Ensure the hit object is actually on the “Walls” layer
            if (wall.layer == LayerMask.NameToLayer("Walls"))
            {
                // Clean up any previous selection
                DeselectWall();
                selectedWall = wall;

                // Gather all renderers so originals can be restored later
                Renderer[] renderers = wall.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0)
                {
                    Debug.LogWarning("⚠️ No renderers found on selected wall.");
                    return;
                }
                originalWallMaterials.Clear();
                foreach (Renderer rend in renderers)
                    originalWallMaterials[rend] = rend.materials;

                // Build the little corner pillars to show selection
                CreateCornerHighlights(selectedWall);

                // Tell the UI slider which DripFillController to drive
                DripFillController dripFill = selectedWall.GetComponent<DripFillController>();
                if (wallFillSlider != null)
                    wallFillSlider.SetDripFillController(dripFill);
                else
                    Debug.LogWarning("⚠️ No WallFillSlider assigned in PrefabPlacer!");

                // Hide the ghost preview while animating
                if (ghostInstance != null)
                    ghostInstance.SetActive(false);

                // Switch menus
                prefabMenu.SetActive(false);
                animationMenu.SetActive(true);

                // Start the fade-in/out highlight coroutine
                if (highlightFadeCoroutine != null)
                    StopCoroutine(highlightFadeCoroutine);
                highlightFadeCoroutine = StartCoroutine(FlashHighlight(selectedWall));
            }
            else
            {
                Debug.LogWarning("❌ Wall is NOT on 'Walls' layer. No animation menu shown.");
            }
        }
        else
        {
            // If nothing was hit, clear any selection
            DeselectWall();
        }
    }

    /// <summary>
    /// Deselects any currently selected wall:
    ///  - Restores original materials
    ///  - Removes corner highlight pillars
    ///  - Clears slider target and ghost
    ///  - Switches back to the prefab menu
    /// </summary>
    private void DeselectWall()
    {
        if (selectedWall != null)
        {
            // Put back each renderer’s original materials
            foreach (var pair in originalWallMaterials)
            {
                Renderer rend = pair.Key;
                if (rend == null) continue;
                rend.materials = pair.Value;
            }
            Debug.Log("🔄 All original materials restored after deselection.");

            // Destroy all highlight pillars
            foreach (GameObject pillar in currentCornerHighlights)
            {
                if (pillar != null)
                    Destroy(pillar);
            }
            currentCornerHighlights.Clear();
        }

        // Clear references
        selectedWall = null;
        originalWallMaterials.Clear();

        // Reset slider to nothing
        SetupWallForFillSlider(selectedWall);

        // Remove any ghost preview instead of re-showing it
        ClearGhost();

        // Prevent auto-ghost recreation
        spawnerRef.selectedPrefab = null;

        // Show prefab menu again
        prefabMenu.SetActive(true);
        animationMenu.SetActive(false);
    }

    /// <summary>
    /// If a wall and slider exist, sets the slider’s target DripFillController
    /// so that moving the slider adjusts that wall’s fill level.
    /// </summary>
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

    /// <summary>
    /// Restarts the drip fill animation on the currently selected wall,
    /// which resets fill level and replay the dripping effect.
    /// </summary>
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

    /// <summary>
    /// Begins the smooth (no‐drip) fill animation on the currently selected wall,
    /// which uses the Alembic pour animation with a bucket.
    /// </summary>
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

    /// <summary>
    /// Finalizes placement of the ghost preview into the scene:
    ///  - Snaps to grid
    ///  - Instantiates the selected prefab at the ghost’s transform
    ///  - Destroys the ghost
    ///  - Adds the new wall to the placedObjects list
    ///  - Calls OnPlaced to stop any particle/drip activity
    /// </summary>
    void TryPlace()
    {
        if (spawnerRef.selectedPrefab == null || ghostInstance == null) return;

        // Capture position and rotation from the ghost
        Vector3 finalPos = SnapToGrid(ghostInstance.transform.position);
        Quaternion finalRot = ghostInstance.transform.rotation;

        // Remove the preview
        Destroy(ghostInstance);
        ghostInstance = null;

        // Instantiate the real wall
        GameObject placed = Instantiate(spawnerRef.selectedPrefab, finalPos, finalRot);
        placed.tag = "Ground";

        // Track it and reset manualRotation for next ghost
        lastPlacedInstance = placed;
        placedObjects.Add(placed);
        manualRotation = finalRot;

        // Disable dripping/particles once placed
        DripFillController controller = placed.GetComponent<DripFillController>();
        if (controller != null)
        {
            controller.OnPlaced();
        }
    }

    /// <summary>
    /// Handles the undo action: if a wall is selected, destroys it and cleans up.
    /// </summary>
    void OnUndo(InputAction.CallbackContext context)
    {
        // If a wall is selected, delete it
        if (selectedWall != null)
        {
            Destroy(selectedWall);
            Debug.Log("Deleted selected object.");

            // Remove from placedObjects list if present
            if (placedObjects.Contains(selectedWall))
            {
                placedObjects.Remove(selectedWall);
            }

            // Clear selection visuals and references
            DeselectWall();
        }
        else
        {
            Debug.LogWarning("No object selected for deletion. Please select an object first.");
        }
    }

    /// <summary>
    /// Deletes every placed wall in the scene and resets state.
    /// </summary>
    public void DeleteAllPlacedObjects()
    {
        // Destroy all GameObjects tracked in placedObjects
        foreach (GameObject obj in placedObjects)
        {
            if (obj != null)
                Destroy(obj);
        }

        // Clear the tracking list
        placedObjects.Clear();

        // Remove any selection highlights
        DeselectWall();

        Debug.Log("Deleted all placed objects.");
    }

    /// <summary>
    /// Snaps a world position to the defined grid size.
    /// </summary>
    Vector3 SnapToGrid(Vector3 pos)
    {
        float x = Mathf.Round(pos.x / gridSize) * gridSize;
        float y = Mathf.Round(pos.y / gridSize) * gridSize;
        float z = Mathf.Round(pos.z / gridSize) * gridSize;
        return new Vector3(x, y, z);
    }

    /// <summary>
    /// Disables every Collider on the given GameObject and its children.
    /// </summary>
    void DisableColliders(GameObject obj)
    {
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
    }

    /// <summary>
    /// Applies the ghostMaterial to all renderers on the instance for the preview.
    /// </summary>
    void ApplyGhostMaterial(GameObject instance)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.material = ghostMaterial;
        }
    }

    /// <summary>
    /// Forces destruction of the current ghost preview, if any.
    /// </summary>
    public void ForceRefreshGhost()
    {
        if (ghostInstance != null)
        {
            Destroy(ghostInstance);
            ghostInstance = null;
        }
    }

    /// <summary>
    /// Opens or closes the VR placement menu and clears the ghost when closing.
    /// </summary>
    public void SetMenuOpen(bool open)
    {
        isMenuOpen = open;

        if (!open)
        {
            // Ensure no ghost remains when menu is closed
            ClearGhost();

            // Also hide the visual ray immediately
            if (visualRay != null)
                visualRay.enabled = false;
        }
    }

    /// <summary>
    /// Computes the combined bounds of all renderers under the given object.
    /// </summary>
    Bounds GetBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(obj.transform.position, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
        {
            bounds.Encapsulate(r.bounds);
        }
        return bounds;
    }

    /// <summary>
    /// Recursively sets the layer for an object and all its descendants.
    /// </summary>
    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    /// <summary>
    /// Calculates a non-overlapping snap position against an arbitrary surface:
    /// biases top-surface placement, then offsets by extents of prefab and target.
    /// </summary>
    Vector3 GetSnapToSurfacePosition(RaycastHit hit, GameObject prefab, Vector3 rayDirection)
    {
        Bounds prefabBounds = GetBounds(prefab);
        Bounds targetBounds = hit.collider.bounds;

        Vector3 faceDirection;

        // Determine if pointing down onto the top surface
        bool aimingDown = Vector3.Dot(rayDirection, Vector3.down) > 0.5f;
        float nearTop = Mathf.Abs(hit.point.y - targetBounds.max.y);
        bool closeToTop = nearTop < 0.2f; // 20 cm threshold

        if ((aimingDown && nearTop < 1f) || closeToTop)
        {
            faceDirection = Vector3.up;
        }
        else
        {
            // Use major axis of the hit normal
            faceDirection = GetMajorAxisWithThreshold(hit.normal, rayDirection);
        }

        // Compute offsets to avoid penetration
        float prefabOffset = Vector3.Scale(prefabBounds.extents, faceDirection).magnitude;
        float targetOffset = Vector3.Scale(targetBounds.extents, faceDirection).magnitude;

        Vector3 snapOffset = faceDirection * (prefabOffset + targetOffset);
        Vector3 pivotOffset = prefabBounds.center - prefab.transform.position;
        Vector3 snapped = hit.collider.transform.position + snapOffset - pivotOffset;

        // Nudge upward if overlapping
        float nudgeAmount = 0.01f;
        int maxAttempts = 20;
        for (int i = 0; i < maxAttempts; i++)
        {
            Collider[] overlaps = Physics.OverlapBox(
                snapped,
                prefabBounds.extents * 0.9f,
                Quaternion.identity,
                placementLayers
            );
            if (overlaps.Length == 0)
                break;
            snapped += Vector3.up * nudgeAmount;
        }

        return snapped;
    }

    /// <summary>
    /// Optional helper: attempts a secondary upward raycast to find a flatter top surface.
    /// </summary>
    void TryAssistTopPlacement(RaycastHit baseHit)
    {
        Vector3 assistStart = baseHit.point + rayOrigin.forward * 0.2f;
        Ray assistRay = new Ray(assistStart, Vector3.up);

        if (Physics.Raycast(assistRay, out RaycastHit assistHit, 2f, placementLayers))
        {
            if (Vector3.Dot(assistHit.normal, Vector3.up) > 0.75f)
            {
                Debug.Log("Assist hit top surface!");
                // Could override the snapped position here
            }
        }
    }

    /// <summary>
    /// Re-enables every Collider on the given GameObject and its children.
    /// </summary>
    void EnableColliders(GameObject obj)
    {
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = true;
        }
    }

    /// <summary>
    /// Chooses the dominant axis of the surface normal, with a threshold to avoid nearly-flat cases.
    /// </summary>
    Vector3 GetMajorAxisWithThreshold(Vector3 normal, Vector3 rayDirection)
    {
        Vector3 abs = new Vector3(
            Mathf.Abs(normal.x),
            Mathf.Abs(normal.y),
            Mathf.Abs(normal.z)
        );
        float threshold = 0.25f;

        // Prioritize nearly-horizontal surfaces as “top”
        if (Vector3.Dot(normal, Vector3.up) > 0.75f)
            return Vector3.up;

        // Pick the largest component above threshold
        if (abs.x > abs.y && abs.x > abs.z && abs.x > threshold)
            return new Vector3(Mathf.Sign(normal.x), 0, 0);
        if (abs.y > abs.x && abs.y > abs.z && abs.y > threshold)
            return new Vector3(0, Mathf.Sign(normal.y), 0);
        if (abs.z > threshold)
            return new Vector3(0, 0, Mathf.Sign(normal.z));

        // Fallback to up-axis
        return Vector3.up;
    }

    /// <summary>
    /// Handles immediate rotation input outside of hold logic: rotates ghost 90° on trigger press.
    /// </summary>
    void HandleRotationInput()
    {
        if (rotateAction != null && rotateAction.action.triggered && ghostInstance != null)
        {
            ghostInstance.transform.Rotate(Vector3.up, 90f, Space.World);
            manualRotation = ghostInstance.transform.rotation;
            Debug.Log("Rotated ghost 90°");
        }
    }

    /// <summary>
    /// Toggles the visibility and position of the laser pointer (visualRay)
    /// based on whether the menu is open and active.
    /// </summary>
    void UpdateVisualRay()
    {
        if (visualRay == null || rayOrigin == null || menuSpawnerRef == null)
            return;

        // Laser only enabled when menu canvas is active
        bool shouldEnableLaser = isMenuOpen
            && menuSpawnerRef.menuCanvas != null
            && menuSpawnerRef.menuCanvas.activeSelf;

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
