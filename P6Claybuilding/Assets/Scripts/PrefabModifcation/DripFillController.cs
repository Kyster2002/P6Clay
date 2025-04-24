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

    [Header("Bucket Offsets Per Rotation Phase (Y is always height)")]
    public Vector3 offsetPhase0 = new Vector3(0f, 0.3f, 0.18f);
    public Vector3 offsetPhase90 = new Vector3(0f, 0.3f, 0.18f);
    public Vector3 offsetPhase180 = new Vector3(0f, 0.3f, -0.18f);
    public Vector3 offsetPhase270 = new Vector3(0f, 0.3f, -0.18f);

    [Header("Particle Offsets Per Rotation Phase (Y is height)")]
    public Vector3 particleOffsetPhase0 = new Vector3(-0.01f, 0.35f, 0f);
    public Vector3 particleOffsetPhase90 = new Vector3(0.07f, 0.35f, -0.01f);
    public Vector3 particleOffsetPhase180 = new Vector3(0.05f, 0.35f, 0.45f);
    public Vector3 particleOffsetPhase270 = new Vector3(0f, 0.35f, 0.46f);

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
    private static int bucketSpawnIndex = 0;  // Used to track rotation correction
    private int bucketRotationPhase = -1; // -1 means uninitialized


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
        // ✅ Only run setup if SetTargetBox() hasn’t already been called
        if (boxTransform == null)
        {
            Transform defaultBox = FindDeepChild(transform, "Box");
            if (defaultBox != null)
            {
                SetTargetBox(defaultBox); // Use the default fallback
            }
            else
            {
                Debug.LogError($"{gameObject.name}: Default 'Box' not found and no target set.");
            }
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


    public void UpdateDripEffect()
    {

        // Ensure bucketRotationPhase is initialized even when not using StartSmoothFillWithoutDrip()
        if (bucketRotationPhase == -1)
        {
            float yRot = Mathf.Round(boxTransform.eulerAngles.y) % 360f;
            if (Mathf.Approximately(yRot, 0f)) bucketRotationPhase = 0;
            else if (Mathf.Approximately(yRot, 90f)) bucketRotationPhase = 1;
            else if (Mathf.Approximately(yRot, 180f)) bucketRotationPhase = 2;
            else if (Mathf.Approximately(yRot, 270f)) bucketRotationPhase = 3;
            else bucketRotationPhase = 0; // fallback
        }

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
                Vector3 particleOffset = Vector3.zero;

                switch (bucketRotationPhase)
                {
                    case 0: particleOffset = particleOffsetPhase0; break;
                    case 1: particleOffset = particleOffsetPhase90; break;
                    case 2: particleOffset = particleOffsetPhase180; break;
                    case 3: particleOffset = particleOffsetPhase270; break;
                }

                particleOffset.y = 0.35f;

                Vector3 topWorldPos = boxRenderer.bounds.max;
                Vector3 desiredParticlePos = topWorldPos + boxTransform.rotation * particleOffset;

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
        // --- Update Bucket Position Using Box Renderer Bounds ---
        if (spawnedBucket != null && boxTransform != null)
        {
            Renderer boxRenderer = boxTransform.GetComponent<Renderer>();
            if (boxRenderer != null)
            {
                Vector3 topWorldPos = boxRenderer.bounds.max;

                // Use rotation phase-specific bucket offset
                Vector3 bucketOffset = Vector3.zero;
                switch (bucketRotationPhase)
                {
                    case 0: bucketOffset = offsetPhase0; break;
                    case 1: bucketOffset = offsetPhase90; break;
                    case 2: bucketOffset = offsetPhase180; break;
                    case 3: bucketOffset = offsetPhase270; break;
                }

                Vector3 desiredBucketPos = topWorldPos + boxTransform.rotation * bucketOffset;
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
        if (dripParticles != null && boxTransform != null)
        {
            DripParticleController pCtrl = dripParticles.GetComponent<DripParticleController>();
            if (pCtrl != null)
            {
                pCtrl.Setup(boxTransform);
            }

            // ✅ RESET PARTICLE SYSTEM STATE
            dripParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            dripParticles.Clear();
            dripParticles.gameObject.SetActive(false); // 🧠 This ensures it doesn't auto-play
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
        DestroyBucketIfExists();

        float yRot = Mathf.Round(boxTransform.eulerAngles.y) % 360f;

        // Set rotation phase
        if (Mathf.Approximately(yRot, 0f)) bucketRotationPhase = 0;
        else if (Mathf.Approximately(yRot, 90f)) bucketRotationPhase = 1;
        else if (Mathf.Approximately(yRot, 180f)) bucketRotationPhase = 2;
        else if (Mathf.Approximately(yRot, 270f)) bucketRotationPhase = 3;
        else bucketRotationPhase = 0; // fallback

        if (bucketPrefab != null)
        {
            Vector3 offset = Vector3.zero;
            Quaternion rotation = Quaternion.identity;

            switch (bucketRotationPhase)
            {
                case 0:
                    offset = offsetPhase0;
                    rotation = Quaternion.Euler(0f, 270f, 0f);
                    break;
                case 1:
                    offset = offsetPhase90;
                    rotation = Quaternion.Euler(0f, 360f, 0f);
                    break;
                case 2:
                    offset = offsetPhase180;
                    rotation = Quaternion.Euler(0f, 270f, 0f);
                    break;
                case 3:
                    offset = offsetPhase270;
                    rotation = Quaternion.Euler(0f, 0f, 0f);
                    break;
            }

            Vector3 spawnPosition = boxTransform.position + boxTransform.rotation * offset;
            spawnedBucket = Instantiate(bucketPrefab, spawnPosition, rotation);
            spawnedBucket.transform.SetParent(transform, worldPositionStays: true);

            AlembicAutoPlay autoPlay = spawnedBucket.GetComponent<AlembicAutoPlay>();
            if (autoPlay == null)
            {
                Debug.LogWarning("⚠️ Spawned bucket is missing AlembicAutoPlay script!");
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

        // 💥 Ensure bucket is destroyed if we're manually overriding fill
        DestroyBucketIfExists();
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

    public void SetTargetBox(Transform box)
    {
        boxTransform = box;
        boxRenderer = box?.GetComponent<Renderer>();

        if (boxRenderer != null)
        {
            originalScale = boxTransform.localScale;
            originalPosition = boxTransform.localPosition;

            rippleMaterial = boxRenderer.material;
            rippleMaterial.SetFloat(_ObjectHeight, originalScale.y);

            isDripping = false;
            animateWobble = false;
            currentFillLevel = 0f;

            UpdateDripEffect();
            boxRenderer.enabled = false;
        }
    }




}
