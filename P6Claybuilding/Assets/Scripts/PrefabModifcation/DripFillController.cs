using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class DripFillController : MonoBehaviour, IPointerClickHandler
{
    // -- PUBLIC FIELDS --
    public float fillSpeed = 0.2f;
    public ParticleSystem dripParticles;
    public Material rippleMaterial;
    public Material originalMaterial;

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

    // NEW: Flag to indicate the object has been placed.
    private bool isPlaced = false;

    // -- SHADER PROPERTY IDs --
    private static readonly int _FillAmount = Shader.PropertyToID("_FillAmount");
    private static readonly int _RippleStrength = Shader.PropertyToID("_RippleStrength");
    private static readonly int _ObjectHeight = Shader.PropertyToID("_ObjectHeight");

    // Particle system offset in global space.
    public float particleYOffset = 1f;

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
            Renderer boxRenderer = boxTransform.GetComponent<Renderer>();
            if (boxRenderer != null)
            {
                rippleMaterial = boxRenderer.material;
                originalScale = boxTransform.localScale;
                originalPosition = boxTransform.localPosition;
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
            rippleMaterial.SetFloat(_RippleStrength, 0f);
        }
        else
        {
            rippleMaterial.SetFloat(_FillAmount, currentFillLevel);
            rippleMaterial.SetFloat(_RippleStrength, animateWobble ? 0.3f : 0f);
        }

        // --- Update Box Scale & Position (Expanding Upward Only) ---
        // This section should reflect your previously corrected logic—
        // assuming that the Box is set to expand only upward.
        float newHeight = Mathf.Lerp(0.01f * originalScale.y, originalScale.y, currentFillLevel);
        boxTransform.localScale = new Vector3(originalScale.x, newHeight, originalScale.z);
        // Here, we assume that your Box's pivot is correctly set (or has been adjusted)
        // so that only the top rises. (i.e. the bottom remains in place.)
        boxTransform.localPosition = originalPosition;

        // --- Update Particle System Position Using Renderer Bounds ---
        if (dripParticles != null)
        {
            Renderer boxRenderer = boxTransform.GetComponent<Renderer>();
            if (boxRenderer != null)
            {
                // Use the renderer's bounds; bounds.max.y is the actual top in world space.
                Vector3 topWorldPos = boxRenderer.bounds.max;
                float offsetY = 0.1f; // Adjust this value to set how far above the fill you want the particles.
                Vector3 desiredParticlePos = topWorldPos + Vector3.up * offsetY;
                dripParticles.transform.position = desiredParticlePos;
            }

            // Activate particle system only if we are in drip mode and the box is still filling.
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
            float randomSpeed = Random.Range(fillSpeed * 0.8f, fillSpeed * 1.2f);
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
        animateWobble = false;
        currentFillLevel = 0f;
        StartCoroutine(SmoothFillAnimation());
    }

    IEnumerator SmoothFillAnimation()
    {
        while (currentFillLevel < 1f)
        {
            currentFillLevel += Time.deltaTime * fillSpeed;
            yield return null;
        }
        currentFillLevel = 1f;
        isDripping = false;
        StartCoroutine(FadeOutRipple());
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
