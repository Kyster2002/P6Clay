using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.Formats.Alembic.Importer;

public class DripFillController : MonoBehaviour, IPointerClickHandler
{
    // -- PUBLIC FIELDS --
    public float fillSpeed = 0.2f;
    public ParticleSystem dripParticles;
    public Material rippleMaterial;
    public Material originalMaterial;
    public GameObject bucketPrefab;  // Prefab reference (assign this in the inspector)
    public float fillStartDelay = 1.5f; // 🛠️ Adjustable delay before filling starts

    public float bucketHeightAboveBox = 1f;  // How high above the object to spawn

    // Global reference for the currently selected object.
    public static DripFillController lastSelectedObject = null;

    // -- PRIVATE FIELDS --
    private float currentFillLevel = 0f;  // Ranges from 0 to 1.
    private bool isDripping = true;        // True for drip fill (with wobble).
    private bool animateWobble = false;    // Controls whether wobble is applied.
    private Vector3 originalScale;
    private Vector3 originalPosition;
    private Transform boxTransform;        // Cached transform for the "Box" child.
    private bool buttonClicked = false;    // Flag for particle activation.
    private GameObject spawnedBucket; // To keep track of the instantiated bucket
    private bool isPlaced = false;
    private Renderer boxRenderer;   // Store the Renderer for the Box
    private bool forceVisible = false; // When false, the Box remains hidden at low fill


    // -- SHADER PROPERTY IDs --
    private static readonly int _FillAmount = Shader.PropertyToID("_FillAmount");
    private static readonly int _RippleStrength = Shader.PropertyToID("_RippleStrength");
    private static readonly int _ObjectHeight = Shader.PropertyToID("_ObjectHeight");

    // Particle system offset in global space.
    public float particleYOffset = 2f;

    // Public property so other scripts (like WallFillSlider) can read the current fill level.
    public float FillLevel
    {
        get { return currentFillLevel; }
    }
    void Start()
    {
        // Find the "Box" child.
        boxTransform = FindDeepChild(transform, "Box");
        if (boxTransform != null)
        {
            boxRenderer = boxTransform.GetComponent<Renderer>(); // Cache the renderer
            if (boxRenderer != null)
            {
                // Save original scale and position
                originalScale = boxTransform.localScale;
                originalPosition = boxTransform.localPosition;

                // Assign the ripple material
                rippleMaterial = boxRenderer.material; // ✅ This keeps your Ripple Shader!

                // Copy textures and properties from the original material (if available)
                Material originalMat = boxRenderer.sharedMaterial; // Original material before runtime instancing
                if (originalMat != null)
                {
                    if (originalMat.HasProperty("_BaseMap"))
                        rippleMaterial.SetTexture("_BaseMap", originalMat.GetTexture("_BaseMap"));

                    if (originalMat.HasProperty("_BumpMap"))
                        rippleMaterial.SetTexture("_BumpMap", originalMat.GetTexture("_BumpMap"));

                    if (originalMat.HasProperty("_OcclusionMap"))
                        rippleMaterial.SetTexture("_OcclusionMap", originalMat.GetTexture("_OcclusionMap"));

                    if (originalMat.HasProperty("_Smoothness"))
                        rippleMaterial.SetFloat("_Smoothness", originalMat.GetFloat("_Smoothness"));
                }
                // Make the Box invisible initially
                boxRenderer.enabled = false;
            }
            else
            {
                Debug.LogError($"{gameObject.name}: 'Box' child found but no renderer attached!");
            }
        }
        else
        {
            Debug.LogError($"{gameObject.name}: 'Box' child not found!");
        }

        // Pass object height to the shader if needed
        if (rippleMaterial != null)
        {
            rippleMaterial.SetFloat(_ObjectHeight, originalScale.y);
        }

        currentFillLevel = 0f;
        isDripping = false;
    }


    void Update()
    {
        // If the object is placed, prevent further updates.
        if (isPlaced)
        {
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

        // --- New: Destroy Bucket when FillLevel is Complete ---
        if (spawnedBucket != null && currentFillLevel >= 1f)
        {
            Destroy(spawnedBucket);
            spawnedBucket = null;
        }
    }


    void UpdateDripEffect()
    {
        if (rippleMaterial == null || boxTransform == null)
            return;

        // --- Update Shader Fill & Ripple ---
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

        // --- Toggle Renderer Visibility Based on Fill Level & Animation Flag ---
        if (!forceVisible && currentFillLevel <= 0.01f)
        {
            // Keep the Box invisible when fill is at minimum and an animation hasn’t been triggered
            if (boxRenderer.enabled)
                boxRenderer.enabled = false;
        }
        else
        {
            // Ensure the Box is visible when we are animating or the fill is beyond the minimum level
            if (!boxRenderer.enabled)
                boxRenderer.enabled = true;
        }

        // --- Update Box Scale & Position (Expanding Upward Only) ---
        float newHeight = Mathf.Lerp(0.01f * originalScale.y, originalScale.y, currentFillLevel);
        boxTransform.localScale = new Vector3(originalScale.x, newHeight, originalScale.z);
        boxTransform.localPosition = originalPosition;

        // --- Update Particle System Position Using Renderer Bounds ---
        if (dripParticles != null && boxTransform != null)
        {
            Renderer boxRenderer = boxTransform.GetComponent<Renderer>();
            if (boxRenderer != null)
            {
                Vector3 topWorldPos = boxRenderer.bounds.max;

                // ✅ Adjustable Offsets
                Vector3 particleOffset = new Vector3(0f, 0.35f, 0f); // <- You can expose this as public if you want
                Vector3 desiredParticlePos = topWorldPos + particleOffset;

                dripParticles.transform.position = desiredParticlePos;
            }

            // Activate/deactivate particles
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

        // --- Update Bucket Position Using Box Renderer Bounds ---
        if (spawnedBucket != null && boxTransform != null)
        {
            Renderer boxRenderer = boxTransform.GetComponent<Renderer>();
            if (boxRenderer != null)
            {
                Vector3 topWorldPos = boxRenderer.bounds.max;

                // ✅ Adjustable Offsets for bucket
                Vector3 bucketOffset = new Vector3(0f, 0.5f, 0.2f); // <- You can expose this as public if you want
                Vector3 desiredBucketPos = topWorldPos + bucketOffset;

                spawnedBucket.transform.position = desiredBucketPos;
            }
        }
    }


    public void SetupBucketPrefab(GameObject prefab)
    {
        bucketPrefab = prefab;
    }


    public void ConfigureDripParticleController()
    {
        if (dripParticles != null)
        {
            DripParticleController pCtrl = dripParticles.GetComponent<DripParticleController>();
            if (pCtrl != null && boxTransform != null)
            {
                pCtrl.Setup(boxTransform);
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        lastSelectedObject = this;
        buttonClicked = true;
        ResetDripEffect();
    }

    public void ResetDripEffect()
    {
        Debug.Log($"{gameObject.name}: ResetDripEffect()");

        DestroyBucketIfExists();   // 🔥 Add this line to remove any leftover bucket

        StopAllCoroutines();
        isDripping = true;
        animateWobble = true;
        isPlaced = false;  // In case it was previously placed.
        currentFillLevel = 0f;

        if (dripParticles != null)
        {
            dripParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            dripParticles.Clear();
        }

        StartCoroutine(DripFillAnimation());
    }


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


    public void StartSmoothFillWithoutDrip()
    {
        Debug.Log($"{gameObject.name}: StartSmoothFillWithoutDrip()");
        StopAllCoroutines();
        isDripping = false;
        animateWobble = true;
        currentFillLevel = 0f;

        float slowFillSpeed = fillSpeed * 1f;

        if (spawnedBucket == null && bucketPrefab != null)
        {
            // Spawn new bucket
            Bounds totalBounds = new Bounds(transform.position, Vector3.zero);
            foreach (Renderer r in GetComponentsInChildren<Renderer>())
            {
                totalBounds.Encapsulate(r.bounds);
            }

            Vector3 boundsCenter = totalBounds.center;
            Vector3 boundsExtents = totalBounds.extents;

            Vector3 spawnPosition = boundsCenter + new Vector3(0f, boundsExtents.y + bucketHeightAboveBox, -boundsExtents.z);

            // ✅ Instantiate and set this object (transform) as the parent
            spawnedBucket = Instantiate(bucketPrefab, spawnPosition, bucketPrefab.transform.rotation, this.transform);

            // ✅ Reset animation when spawned
            AlembicAutoPlay autoPlay = spawnedBucket.GetComponent<AlembicAutoPlay>();
            if (autoPlay == null)
            {
                Debug.LogWarning("⚠️ Spawned bucket is missing AlembicAutoPlay script!");
            }
        }
        else if (spawnedBucket != null)
        {
            // ✅ Reset animation even if reusing existing bucket
            AlembicStreamPlayer streamPlayer = spawnedBucket.GetComponent<AlembicStreamPlayer>();
            if (streamPlayer != null)
            {
                streamPlayer.CurrentTime = 0f;
            }
        }

        StartCoroutine(SmoothFillAnimation(slowFillSpeed));
    }



    IEnumerator SmoothFillAnimation(float speed)
{
    // 🛑 First, wait before starting the fill
    if (fillStartDelay > 0f)
    {
        yield return new WaitForSeconds(fillStartDelay);
    }

    // 🏁 Then start filling normally
    while (currentFillLevel < 1f)
    {
        currentFillLevel += Time.deltaTime * speed;
        yield return null;
    }
    currentFillLevel = 1f;
    isDripping = false;
    StartCoroutine(FadeOutRipple());
}

    public void DestroyBucketIfExists()
    {
        if (spawnedBucket != null)
        {
            Destroy(spawnedBucket);
            spawnedBucket = null;
            Debug.Log("🪣 Destroyed bucket because we're switching to drip mode.");
        }
    }


    public void SetFillLevel(float value)
    {
        StopAllCoroutines();
        isDripping = false;
        animateWobble = false;
        currentFillLevel = Mathf.Clamp01(value);
        UpdateDripEffect();
    }

    IEnumerator FadeOutRipple()
    {
        float startStrength = rippleMaterial.GetFloat(_RippleStrength);
        float duration = 0.5f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float newStrength = Mathf.Lerp(startStrength, 0f, elapsed / duration);
            rippleMaterial.SetFloat(_RippleStrength, newStrength);
            yield return null;
        }
        rippleMaterial.SetFloat(_RippleStrength, 0f);
    }

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

    // Call this method when the object is placed so that particle effects (and drip) are halted.
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


}
