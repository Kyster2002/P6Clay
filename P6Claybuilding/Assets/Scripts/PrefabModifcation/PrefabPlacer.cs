using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System;
using System.Collections;
using TMPro;


public class PrefabPlacer : MonoBehaviour
{


    [Header("Valid Placement Tags")]
    public List<string> validSurfaceTags = new List<string>() { "Ground" };

    [Header("Highlight Settings")]
    public float highlightYScaleMultiplier = 1.0f; // You can adjust this in the inspector

    public TMP_Text wallToggleLabel;

    [Header("Highlight Settings")]
    private float highlightDuration = 2f; // Duration of highlight fade in/out
    private Coroutine highlightFadeCoroutine;
    private List<GameObject> currentCornerHighlights = new List<GameObject>();

    [Header("References")]
    public Transform rayOrigin;
    public PrefabButtonSpawner spawnerRef;
    public WallFillSlider wallFillSlider;

    [Header("Input")]
    public InputActionReference placeAction;
    public InputActionReference undoAction;
    public InputActionReference rotateAction;
    public InputActionReference highlightAction;

    [Header("Placement Settings")]
    public float maxRayDistance = 100f;
    public LayerMask placementLayers;
    public float gridSize = 0.5f;
    public Material ghostMaterial;
    [SerializeField] float ghostSmoothSpeed = 15f;

    private GameObject ghostInstance;
    private GameObject lastPlacedInstance;
    public bool isMenuOpen = false;
    private List<GameObject> placedObjects = new List<GameObject>();

    [Header("Wall Selection")]
    public LayerMask wallLayer;
    public Color highlightColor = Color.yellow;

    private GameObject selectedWall;
    private Dictionary<Renderer, Material[]> originalWallMaterials = new Dictionary<Renderer, Material[]>();
    private Dictionary<Renderer, Color[]> originalWallColors = new Dictionary<Renderer, Color[]>();


    [Header("Menu References")]
    public GameObject prefabMenu;
    public GameObject animationMenu;
    public VRMenuSpawner menuSpawnerRef;

    [Header("Rotation Control")]
    public float rotateSpeed = 90f;
    public float faceBias = 0.005f;
    private float rotationHoldTime = 0.5f;
    private float rotateTimer = 0f;
    private bool hasLaidDown = false;
    private Quaternion manualRotation = Quaternion.identity;

    [Header("Visual Ray")]
    public LineRenderer visualRay;
    public float rayLength = 10f;



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

    private IEnumerator FlashHighlight(GameObject wall)
    {
        if (wall == null) yield break;

        Renderer[] renderers = wall.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) yield break;

        float elapsed = 0f;
        float halfDuration = highlightDuration / 2f;

        // First half: Fade in
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
                    Color fromColor = (i < originalColors.Length) ? originalColors[i] : Color.white;
                    Color toColor = highlightColor;
                    Color lerpedColor = Color.Lerp(fromColor, toColor, t);

                    if (rend.materials[i].HasProperty("_BaseColor"))
                        rend.materials[i].SetColor("_BaseColor", lerpedColor);
                    else
                        rend.materials[i].color = lerpedColor;
                }
            }

            yield return null;
        }

        elapsed = 0f;

        // Second half: Fade out
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

        highlightFadeCoroutine = null; // Clear coroutine reference when done
    }

    void CreateCornerHighlights(GameObject wall)
    {
        if (wall == null) return;

        Transform meshRoot = FindDeepChild(wall.transform, "Box");
        if (meshRoot == null) meshRoot = FindDeepChild(wall.transform, "Box_Closed");

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

        Vector3[] localCorners = new Vector3[4];
        float yBase = 0f; // Hardcoded base height in local wall space

        localCorners[0] = new Vector3(meshBounds.min.x, yBase, meshBounds.min.z);
        localCorners[1] = new Vector3(meshBounds.max.x, yBase, meshBounds.min.z);
        localCorners[2] = new Vector3(meshBounds.min.x, yBase, meshBounds.max.z);
        localCorners[3] = new Vector3(meshBounds.max.x, yBase, meshBounds.max.z);

        // Calculate full-height of the box after scale is applied
        float fullHeight = meshBounds.size.y * meshTransform.localScale.y;
        float pillarHeight = fullHeight * 0.5f;

        foreach (Vector3 localCorner in localCorners)
        {
            Vector3 worldPos = meshTransform.TransformPoint(localCorner);
            Vector3 localToWall = wall.transform.InverseTransformPoint(worldPos);

            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.transform.SetParent(wall.transform);
            pillar.transform.localPosition = localToWall + new Vector3(0f, pillarHeight, 0f); // 🟡 Shift upward
            pillar.transform.localScale = new Vector3(0.02f, pillarHeight, 0.02f);

            pillar.GetComponent<Renderer>().material.color = Color.yellow;
            Destroy(pillar.GetComponent<Collider>());

            currentCornerHighlights.Add(pillar);
        }

    }



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



    public void ToggleWallBoxVersion()
    {
        if (selectedWall == null)
        {
            Debug.LogWarning("❌ No wall is currently selected to toggle.");
            return;
        }

        // Find both variants
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

        // Decide which one to activate
        Transform newTarget = null;
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
            // fallback
            box.gameObject.SetActive(true);
            boxClosed.gameObject.SetActive(false);
            newTarget = box;
        }

        // Get your DripFillController
        DripFillController drip = selectedWall.GetComponent<DripFillController>();
        if (drip != null)
        {
            // 1) Preserve the current fill fraction
            float previousFill = drip.FillLevel;

            // 2) Switch the target box (this resets boxTransform & originalScale)
            drip.SetTargetBox(newTarget);

            // 3) Restore the fill fraction on the new box
            drip.SetFillLevel(previousFill);

            // 4) Re-parent your particles if needed
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






    void OnHighlightWall(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
            return;

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        Debug.DrawRay(ray.origin, ray.direction * 1000f, Color.yellow, 2f);

        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, wallLayer))
        {
            Transform root = hit.collider.transform.root;
            GameObject wall = root.gameObject;

            if (selectedWall == wall)
            {
                DeselectWall();
                return;
            }

            if (wall.layer == LayerMask.NameToLayer("Walls"))
            {
                DeselectWall(); // Clean up old selection first

                selectedWall = wall;

                Renderer[] renderers = wall.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0)
                {
                    Debug.LogWarning("⚠️ No renderers found on selected wall.");
                    return;
                }

                // Save original materials
                originalWallMaterials.Clear();
                foreach (Renderer rend in renderers)
                {
                    originalWallMaterials[rend] = rend.materials;
                }

                // 🔥 CREATE CORNER HIGHLIGHTS HERE
                CreateCornerHighlights(selectedWall);

                // Assign DripFillController
                DripFillController dripFill = selectedWall.GetComponent<DripFillController>();
                if (wallFillSlider != null)
                {
                    wallFillSlider.SetDripFillController(dripFill);
                }
                else
                {
                    Debug.LogWarning("⚠️ No WallFillSlider assigned in PrefabPlacer!");
                }

                if (ghostInstance != null)
                    ghostInstance.SetActive(false);

                prefabMenu.SetActive(false);
                animationMenu.SetActive(true);

                // ✅ Start highlight fading
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
            DeselectWall();
        }
    }




    private void DeselectWall()
    {
        if (selectedWall != null)
        {
            // Restore original materials
            foreach (var pair in originalWallMaterials)
            {
                Renderer rend = pair.Key;
                if (rend == null) continue;
                rend.materials = pair.Value;
            }
            Debug.Log("🔄 All original materials restored after deselection.");

            // Destroy all corner highlight pillars
            foreach (GameObject pillar in currentCornerHighlights)
            {
                if (pillar != null)
                    Destroy(pillar);
            }
            currentCornerHighlights.Clear();
        }

        selectedWall = null;
        originalWallMaterials.Clear();

        SetupWallForFillSlider(selectedWall);

        // Instead of reactivating the ghost, clear it
        ClearGhost();

        // Also, clear the prefab selection so that the ghost is not recreated automatically.
        spawnerRef.selectedPrefab = null;

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
        // If a wall (object) is selected via the highlight method, delete that one.
        if (selectedWall != null)
        {
            Destroy(selectedWall);
            Debug.Log("Deleted selected object.");

            // Remove the selected object from the list, if it exists there.
            if (placedObjects.Contains(selectedWall))
            {
                placedObjects.Remove(selectedWall);
            }

            // Clear any highlight or selection effects.
            DeselectWall();
        }
        else
        {
            Debug.LogWarning("No object selected for deletion. Please select an object first.");
        }
    }

    public void DeleteAllPlacedObjects()
    {
        // Loop through all placed objects and destroy them.
        foreach (GameObject obj in placedObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }

        // Clear the list.
        placedObjects.Clear();

        // Clear the selection if it exists.
        DeselectWall();

        Debug.Log("Deleted all placed objects.");
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
