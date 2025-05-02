using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.Formats.Alembic.Importer;
using System.Collections.Generic;

/// <summary>
/// DripFillController: Drives the “liquid clay” fill effect on a wall mesh.
///  - Handles drip-based fill (with wobble)
///  - Handles smooth fill via a bucket + Alembic pour animation
///  - Updates shader parameters for ripple + fill level
///  - Manages particle system and bucket spawning/positioning
/// Implements IPointerClickHandler to start drip on pointer clicks.
/// </summary>
public class DripFillController : MonoBehaviour, IPointerClickHandler
{
    // -- PUBLIC CONFIG FIELDS --

    /// <summary>Base speed for fill animations (scaled by random factor for drip).</summary>
    public float fillSpeed = 0.2f;
    /// <summary>Particle system used for drip effects.</summary>
    public ParticleSystem dripParticles;
    /// <summary>Material instance that drives the ripple/shader effect.</summary>
    public Material rippleMaterial;
    /// <summary>Original material (if needed for restoration).</summary>
    public Material originalMaterial;
    /// <summary>Prefab used for bucket in smooth-fill animation (assign in inspector).</summary>
    public GameObject bucketPrefab;
    /// <summary>Delay before starting smooth fill (seconds).</summary>
    public float fillStartDelay = 1.5f;

    [Header("Bucket Offsets Per Rotation Phase (Y is always height)")]
    public Vector3 offsetPhase0 = new Vector3(0f, 0.3f, 0.18f);
    public Vector3 offsetPhase90 = new Vector3(0f, 0.3f, 0.18f);
    public Vector3 offsetPhase180 = new Vector3(0f, 0.3f, -0.18f);
    public Vector3 offsetPhase270 = new Vector3(0f, 0.3f, -0.18f);

    [Header("Bucket Rotations Per Phase")]
    public Vector3 bucketRotPhase0 = new Vector3(0f, -90f, 0f);
    public Vector3 bucketRotPhase90 = new Vector3(0f, 0f, 0f);
    public Vector3 bucketRotPhase180 = new Vector3(0f, -90f, 0f);
    public Vector3 bucketRotPhase270 = new Vector3(0f, 0f, 0f);


    [Header("Particle Offsets Per Rotation Phase (Y is height)")]
    public Vector3 particleOffsetPhase0 = new Vector3(-0.01f, 0.35f, 0f);
    public Vector3 particleOffsetPhase90 = new Vector3(0.07f, 0.35f, -0.01f);
    public Vector3 particleOffsetPhase180 = new Vector3(0.05f, 0.35f, 0.45f);
    public Vector3 particleOffsetPhase270 = new Vector3(0f, 0.35f, 0.46f);

    /// <summary>Vertical offset above the box where the bucket will spawn.</summary>
    public float bucketHeightAboveBox = 1f;

    /// <summary>Static reference to the last clicked/selected DripFillController.</summary>
    public static DripFillController lastSelectedObject = null;

    // -- PRIVATE STATE FIELDS --

    private float currentFillLevel = 0f;    // 0–1 fill fraction
    private bool isDripping = true;    // true while drip animation runs
    private bool animateWobble = false;   // if true, ripple strength oscillates
    private Vector3 originalScale;          // full box scale
    private Vector3 originalPosition;       // base box position
    private Transform boxTransform;         // active box variant transform
    private bool buttonClicked = false;   // clicked flag for UI
    private GameObject spawnedBucket;       // runtime bucket instance
    private bool isPlaced = false;   // set when the wall is “placed”
    private Renderer boxRenderer;           // renderer of the active box
    private bool forceVisible = false;   // if false, box invisible at low fill
    private static int bucketSpawnIndex = 0; // unused rotation tracker
    private int bucketRotationPhase = -1;   // 0/1/2/3 corresponds to Y rotation
    private Dictionary<string, Vector3> _initialScales = new Dictionary<string, Vector3>();
    private Dictionary<string, Vector3> _initialPositions = new Dictionary<string, Vector3>();

    // Shader property IDs (cached for performance)
    private static readonly int _FillAmount = Shader.PropertyToID("_FillAmount");
    private static readonly int _RippleStrength = Shader.PropertyToID("_RippleStrength");
    private static readonly int _ObjectHeight = Shader.PropertyToID("_ObjectHeight");

    /// <summary>Y offset applied to particle System position (unused if per-phase offsets apply).</summary>
    public float particleYOffset = 2f;

    /// <summary>Public read-only access to the current fill level.</summary>
    public float FillLevel => currentFillLevel;

    /// <summary>
    /// Awake: caches the unmodified full scale/position of both Box variants
    /// so that SetTargetBox can reset correctly later.
    /// </summary>
    void Awake()
    {
        var box = FindDeepChild(transform, "Box");
        var boxClosed = FindDeepChild(transform, "Box_Closed");
        if (box != null)
        {
            _initialScales["Box"] = box.localScale;
            _initialPositions["Box"] = box.localPosition;
        }
        if (boxClosed != null)
        {
            _initialScales["Box_Closed"] = boxClosed.localScale;
            _initialPositions["Box_Closed"] = boxClosed.localPosition;
        }
    }

    /// <summary>
    /// Start: ensures that a target box is set (prefers “Box”),
    /// and initializes fill state.
    /// </summary>
    void Start()
    {
        if (boxTransform == null)
        {
            Transform defaultBox = FindDeepChild(transform, "Box");
            if (defaultBox != null)
                SetTargetBox(defaultBox);
            else
                Debug.LogError($"{gameObject.name}: Default 'Box' not found and no target set.");
        }
        currentFillLevel = 0f;
        isDripping = false;
    }

    /// <summary>
    /// Update: skips if placed; otherwise updates drip effect and
    /// destroys the bucket once fill reaches 1.
    /// </summary>
    void Update()
    {
        if (isPlaced)
        {
            // Stop and hide particles once placed permanently.
            if (dripParticles != null)
            {
                if (dripParticles.isPlaying)
                    dripParticles.Stop();
                if (dripParticles.gameObject.activeSelf)
                    dripParticles.gameObject.SetActive(false);
            }
            return;
        }

        if (rippleMaterial == null || boxTransform == null)
            return;

        UpdateDripEffect();

        // Destroy spawned bucket once fill completes fully
        if (spawnedBucket != null && currentFillLevel >= 1f)
        {
            Destroy(spawnedBucket);
            spawnedBucket = null;
        }
    }

    /// <summary>
    /// Computes the correct bucket offset in local space, based on whether the wall is upright or flat.
    /// Ensures the bucket always 'pours into' the wall face.
    /// </summary>
    Vector3 ComputeBucketOffset()
    {
        bool isFlat = Mathf.Abs(boxTransform.eulerAngles.x - 90f) < 10f;

        // Local offset: -Z points "into the wall" for upright; -Y for flat
        Vector3 localOffset = isFlat
            ? new Vector3(0f, -0.3f, 0f) // Bucket slightly above flat wall
            : new Vector3(0f, bucketHeightAboveBox, -0.3f); // Bucket above facing into upright wall

        return boxTransform.rotation * localOffset;
    }

    Quaternion ComputeBucketRotation()
    {
        switch (bucketRotationPhase)
        {
            case 1: return Quaternion.Euler(bucketRotPhase90);
            case 2: return Quaternion.Euler(bucketRotPhase180);
            case 3: return Quaternion.Euler(bucketRotPhase270);
            default: return Quaternion.Euler(bucketRotPhase0);
        }
    }

    /// <summary>
    /// Updates shader parameters, box scale/visibility, particle position,
    /// and bucket position based on current fill and rotation phase.
    /// </summary>
    public void UpdateDripEffect()
    {
        // Initialize rotation phase if unset
        if (bucketRotationPhase == -1)
        {
            float yRot = Mathf.Round(boxTransform.eulerAngles.y) % 360f;
            if (Mathf.Approximately(yRot, 0f)) bucketRotationPhase = 0;
            else if (Mathf.Approximately(yRot, 90f)) bucketRotationPhase = 1;
            else if (Mathf.Approximately(yRot, 180f)) bucketRotationPhase = 2;
            else if (Mathf.Approximately(yRot, 270f)) bucketRotationPhase = 3;
            else bucketRotationPhase = 0;
        }

        // Apply fill amount and ripple strength to shader
        if (currentFillLevel >= 0.98f)
        {
            currentFillLevel = 1f;
            rippleMaterial.SetFloat(_FillAmount, 1f);
            rippleMaterial.SetFloat(_RippleStrength, 0.5f);
        }
        else
        {
            rippleMaterial.SetFloat(_FillAmount, currentFillLevel);
            rippleMaterial.SetFloat(_RippleStrength, animateWobble ? 0.3f : 0f);
        }

        // Show/hide box renderer based on fill fraction
        if (!forceVisible && currentFillLevel <= 0.01f)
        {
            if (boxRenderer.enabled)
                boxRenderer.enabled = false;
        }
        else
        {
            if (!boxRenderer.enabled)
                boxRenderer.enabled = true;
        }

        // Scale the box vertically according to fill level
        float newHeight = Mathf.Lerp(0.01f * originalScale.y, originalScale.y, currentFillLevel);
        boxTransform.localScale = new Vector3(originalScale.x, newHeight, originalScale.z);
        boxTransform.localPosition = originalPosition;

        // Update drip particle system position and play/stop logic
        if (dripParticles != null)
        {
            Renderer rend = boxTransform.GetComponent<Renderer>();
            if (rend != null)
            {
                // Choose offset per rotation phase
                Vector3 particleOffset = particleOffsetPhase0;
                switch (bucketRotationPhase)
                {
                    case 1: particleOffset = particleOffsetPhase90; break;
                    case 2: particleOffset = particleOffsetPhase180; break;
                    case 3: particleOffset = particleOffsetPhase270; break;
                }
                particleOffset.y = 0.35f; // override Y component

                Vector3 topWorldPos = rend.bounds.max;
                Vector3 desiredParticlePos = topWorldPos + boxTransform.rotation * particleOffset;
                dripParticles.transform.position = desiredParticlePos;
            }

            if (isDripping && currentFillLevel < 1f)
            {
                if (!dripParticles.gameObject.activeSelf)
                    dripParticles.gameObject.SetActive(true);
                if (!dripParticles.isPlaying)
                    dripParticles.Play();
            }
            else
            {
                if (dripParticles.isPlaying)
                    dripParticles.Stop();
                dripParticles.gameObject.SetActive(false);
            }
        }

        // Update bucket spawn position if present
        if (spawnedBucket != null)
        {
            Renderer rend = boxTransform.GetComponent<Renderer>();
            if (rend != null)
            {
                Vector3 topWorldPos = rend.bounds.max;
                Vector3 bucketOffset = offsetPhase0;
                switch (bucketRotationPhase)
                {
                    case 1: bucketOffset = offsetPhase90; break;
                    case 2: bucketOffset = offsetPhase180; break;
                    case 3: bucketOffset = offsetPhase270; break;
                }
                Vector3 desiredBucketPos = topWorldPos + boxTransform.rotation * bucketOffset;
                spawnedBucket.transform.position = desiredBucketPos;
            }
        }
    }

    /// <summary>
    /// Stores the bucket prefab reference for later instantiation.
    /// </summary>
    public void SetupBucketPrefab(GameObject prefab)
    {
        bucketPrefab = prefab;
    }

    /// <summary>
    /// Configures the DripParticleController on the particle system,
    /// resets it, and ensures it’s inactive until needed.
    /// </summary>
    public void ConfigureDripParticleController()
    {
        if (dripParticles != null && boxTransform != null)
        {
            var pCtrl = dripParticles.GetComponent<DripParticleController>();
            if (pCtrl != null)
                pCtrl.Setup(boxTransform);

            dripParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            dripParticles.Clear();
            dripParticles.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// IPointerClickHandler implementation: marks this as last selected,
    /// triggers a drip-reset when clicked.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        lastSelectedObject = this;
        buttonClicked = true;
        ResetDripEffect();
    }

    /// <summary>
    /// Resets the drip animation: destroys any bucket,
    /// clears coroutines, resets fill, and starts drip coroutine.
    /// </summary>
    public void ResetDripEffect()
    {
        Debug.Log($"{gameObject.name}: ResetDripEffect()");

        DestroyBucketIfExists();
        StopAllCoroutines();
        isDripping = true;
        animateWobble = true;
        isPlaced = false;
        currentFillLevel = 0f;

        if (dripParticles != null)
        {
            dripParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            dripParticles.Clear();
        }

        StartCoroutine(DripFillAnimation());
    }

    /// <summary>
    /// Coroutine for drip-based fill: increments fill randomly,
    /// then transitions to fade-out ripple once full.
    /// </summary>
    IEnumerator DripFillAnimation()
    {
        while (currentFillLevel < 1f)
        {
            float randomSpeed = Random.Range(fillSpeed * 0.5f, fillSpeed * 0.5f);
            currentFillLevel += Time.deltaTime * randomSpeed;
            yield return null;
        }
        currentFillLevel = 1f;
        isDripping = false;
        animateWobble = false;
        StartCoroutine(FadeOutRipple());
    }

    /// <summary>
    /// Begins the smooth fill (no-drip) animation: spawns bucket, aligns it,
    /// then drives fill uniformly after a delay.
    /// </summary>
    public void StartSmoothFillWithoutDrip()
    {
        Debug.Log($"{gameObject.name}: StartSmoothFillWithoutDrip()");

        isDripping = false;
        animateWobble = false;

        DestroyBucketIfExists();
        currentFillLevel = 0f;

        if (bucketPrefab == null) return;

        spawnedBucket = Instantiate(bucketPrefab);

        Transform boxRoot = FindDeepChild(transform, "Box") ?? FindDeepChild(transform, "Box_Closed");
        if (boxRoot != null)
        {
            Renderer rend = boxRoot.GetComponent<Renderer>();
            if (rend != null)
            {
                // Use the new rotation-aware offset logic
                Vector3 offset = ComputeBucketOffset();
                Vector3 spawnPosition = boxTransform.position + offset;

                spawnedBucket.transform.position = spawnPosition;
                spawnedBucket.transform.rotation = ComputeBucketRotation();
                spawnedBucket.transform.localScale = Vector3.one;

                // Do NOT parent under box to avoid inheriting scale
                spawnedBucket.transform.SetParent(null);
            }
            else
            {
                Debug.LogWarning("⚠️ Box has no renderer!");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ Could not find Box or Box_Closed!");
        }

        StartCoroutine(SmoothFillAnimation(fillSpeed));
    }





    private Bounds GetCombinedBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(obj.transform.position, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
            bounds.Encapsulate(r.bounds);
        return bounds;
    }


    /// <summary>
    /// Smooth fill coroutine: waits for fillStartDelay then increments fill until full,
    /// finally fades out ripple effect.
    /// </summary>
    IEnumerator SmoothFillAnimation(float speed)
    {
        if (fillStartDelay > 0f)
            yield return new WaitForSeconds(fillStartDelay);

        while (currentFillLevel < 1f)
        {
            currentFillLevel += Time.deltaTime * speed;
            yield return null;
        }
        currentFillLevel = 1f;
        isDripping = false;
        StartCoroutine(FadeOutRipple());
    }

    /// <summary>
    /// Destroys the spawned bucket if present.
    /// </summary>
    public void DestroyBucketIfExists()
    {
        if (spawnedBucket != null)
        {
            Destroy(spawnedBucket);
            spawnedBucket = null;
            Debug.Log("🪣 Destroyed bucket because we're switching to drip mode.");
        }
    }

    /// <summary>
    /// Sets fill level manually (e.g. via slider), halts animations,
    /// updates visual effect and destroys bucket.
    /// </summary>
    public void SetFillLevel(float value)
    {
        StopAllCoroutines();
        isDripping = false;
        animateWobble = false;

        currentFillLevel = Mathf.Clamp01(value);
        UpdateDripEffect();
        DestroyBucketIfExists();
    }

    /// <summary>
    /// Gradually fades out the ripple strength over 0.5s.
    /// </summary>
    IEnumerator FadeOutRipple()
    {
        float startStrength = rippleMaterial.GetFloat(_RippleStrength);
        float duration = 0.5f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            rippleMaterial.SetFloat(_RippleStrength, Mathf.Lerp(startStrength, 0f, t));
            yield return null;
        }
        rippleMaterial.SetFloat(_RippleStrength, 0f);
    }

    /// <summary>
    /// Recursive find of a child transform by exact name.
    /// </summary>
    Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
            Transform result = FindDeepChild(child, name);
            if (result != null)
                return result;
        }
        return null;
    }

    /// <summary>
    /// Called by PrefabPlacer once the wall is placed: stops all drips/particles.
    /// </summary>
    public void OnPlaced()
    {
        Debug.Log("OnPlaced() called on " + gameObject.name);

        StopAllCoroutines();
        isDripping = false;
        animateWobble = false;
        if (dripParticles != null)
        {
            if (dripParticles.isPlaying)
                dripParticles.Stop();
            dripParticles.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Sets which box variant (Box or Box_Closed) is the active target
    /// for fill/scale/shader animation.
    /// </summary>
    public void SetTargetBox(Transform box)
    {
        boxTransform = box;
        boxRenderer = box?.GetComponent<Renderer>();
        if (boxRenderer != null)
        {
            // Pull full scale/position from cache if available
            string key = box.name;
            if (_initialScales.TryGetValue(key, out var fullScale))
                originalScale = fullScale;
            else
                originalScale = box.localScale;
            if (_initialPositions.TryGetValue(key, out var fullPos))
                originalPosition = fullPos;
            else
                originalPosition = box.localPosition;

            // Configure shader’s object height property
            rippleMaterial = boxRenderer.material;
            rippleMaterial.SetFloat(_ObjectHeight, originalScale.y);

            // Reset fill state
            isDripping = false;
            animateWobble = false;
            currentFillLevel = 0f;

            UpdateDripEffect();
            boxRenderer.enabled = false;
        }
    }
}
