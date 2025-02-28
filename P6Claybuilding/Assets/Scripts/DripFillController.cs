using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class DripFillController : MonoBehaviour, IPointerClickHandler
{
    public static DripFillController lastSelectedObject = null;

    public float fillSpeed = 0.2f;
    private float currentFillLevel = 0f;
    private bool isDripping = true;
    private Vector3 originalScale;
    private Vector3 originalPosition;
    public ParticleSystem dripParticles;
    public Material rippleMaterial;
    private bool buttonClicked = false; // ✅ Tracks if the button was clicked
    private bool shouldRipple = false;


    private static readonly int FillAmount = Shader.PropertyToID("_FillAmount");
    private static readonly int ObjectHeight = Shader.PropertyToID("_ObjectHeight");

    void Start()
    {
        originalScale = transform.localScale;
        originalPosition = transform.position;

        Debug.Log($"🔹 {gameObject.name} - Start(): originalScale={originalScale}, originalPosition={originalPosition}");

        if (rippleMaterial == null)
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                rippleMaterial = renderer.material;
                Debug.Log($"✔ {gameObject.name} - Assigned material: {rippleMaterial.name}");
            }
            else
            {
                Debug.LogError($"❌ {gameObject.name} - No Renderer found!");
            }
        }

        if (rippleMaterial != null)
        {
            rippleMaterial.SetFloat(ObjectHeight, originalScale.y);
        }

        currentFillLevel = 0f;
        transform.localScale = new Vector3(originalScale.x, 0.01f * originalScale.y, originalScale.z);
        ApplyDripEffect();
    }

    void Update()
    {
        if (rippleMaterial != null)
        {
            float wobbleFactor = Mathf.Sin(Time.time * 10f) * 0.05f; // ✅ Adjust frequency/amplitude as needed
            rippleMaterial.SetFloat("_FillAmount", currentFillLevel + wobbleFactor);
        }
    }


    void ApplyDripEffect()
    {

        // ✅ Fix scale calculations ONLY for the cube
        transform.localScale = new Vector3(
            originalScale.x,
            Mathf.Lerp(0.01f * originalScale.y, originalScale.y, currentFillLevel),
            originalScale.z
        );

        // ✅ Fix position calculations (no INF/NaN errors)
        float newYPosition = originalPosition.y + (originalScale.y * (currentFillLevel - 1) / 2);
        if (float.IsNaN(newYPosition) || float.IsInfinity(newYPosition))
        {
            Debug.LogError($"❌ {gameObject.name} - Invalid Y position! Resetting.");
            newYPosition = originalPosition.y;
        }
        transform.position = new Vector3(transform.position.x, newYPosition, transform.position.z);

        // ✅ Ensure ripple effect only happens when the drip animation is running
        bool shouldRipple = isDripping && currentFillLevel < 1.0f;

        // ✅ Update shader properties
        if (rippleMaterial != null)
        {
            rippleMaterial.SetFloat("_FillAmount", currentFillLevel);
            rippleMaterial.SetFloat("_ObjectHeight", originalScale.y);
            rippleMaterial.SetFloat("_RippleStrength", shouldRipple ? 0.3f : 0.0f); // ✅ Ripple only during drip animation
        }

        // ✅ Ensure particle system is ONLY toggled on/off (NEVER SCALED)
        if (dripParticles != null)
        {
            if (buttonClicked)
            {
                if (!dripParticles.gameObject.activeSelf && currentFillLevel < 1f)
                {
                    dripParticles.gameObject.SetActive(true);
                    dripParticles.Play();
                    Debug.Log($"🔥 {gameObject.name} - Drip Particles Activated!");
                }
                else if (currentFillLevel >= 1f && dripParticles.isPlaying)
                {
                    dripParticles.Stop();
                    StartCoroutine(DeactivateParticles());
                }
            }
        }

        Debug.Log($"✔ {gameObject.name} - Shader updated, Ripple active: {shouldRipple}");
    }






    public void SetFillLevel(float value)
    {
        StopAllCoroutines();
        isDripping = false; // ✅ Prevents rippling when using the slider
        currentFillLevel = Mathf.Clamp(value, 0f, 1f);
        ApplyDripEffect();
    }




    public void OnPointerClick(PointerEventData eventData)
    {
        lastSelectedObject = this;
        buttonClicked = true; // ✅ Mark the button as clicked
        Debug.Log($"✔ {gameObject.name} - Button clicked, particles enabled!");
    }


    public void ResetDripEffect()
    {
        Debug.Log($"🔄 {gameObject.name} - ResetDripEffect()");
        StopAllCoroutines();
        isDripping = true;
        currentFillLevel = 0f;

        // ✅ Store current position so it doesn't reset
        Vector3 preservedPosition = transform.position;

        ApplyDripEffect();

        // ✅ Restore the preserved position
        transform.position = preservedPosition;

        if (dripParticles != null)
        {
            dripParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            dripParticles.Clear();
            dripParticles.gameObject.SetActive(false); // ✅ Hide it again on reset
        }

        StartCoroutine(DripFillAnimation());
    }



    IEnumerator DripFillAnimation()
    {
        while (isDripping && currentFillLevel < 1f)
        {
            float randomSpeed = Random.Range(fillSpeed * 0.8f, fillSpeed * 1.2f);
            currentFillLevel += Time.deltaTime * randomSpeed;
            currentFillLevel = Mathf.Min(currentFillLevel, 1f);

            ApplyDripEffect();

            if (dripParticles != null)
            {
                if (currentFillLevel < 1f && !dripParticles.isPlaying)
                {
                    dripParticles.gameObject.SetActive(true);
                    dripParticles.Play();
                }
                else if (currentFillLevel >= 1f && dripParticles.isPlaying)
                {
                    dripParticles.Stop();
                    StartCoroutine(DeactivateParticles()); // ✅ Only deactivate after stopping
                }
            }

            yield return null;
        }
    }


    public void RefreshMaterial()
    {
        if (rippleMaterial != null)
        {
            rippleMaterial.SetFloat("_ObjectHeight", originalScale.y);
            rippleMaterial.SetFloat("_FillAmount", currentFillLevel);

            Renderer objRenderer = GetComponent<Renderer>();
            if (objRenderer != null)
            {
                objRenderer.material = rippleMaterial;
                Debug.Log($"✔ {gameObject.name} - Shader refreshed.");
            }
        }
    }

    IEnumerator DeactivateParticles()
    {
        yield return new WaitForSeconds(0.1f); // ✅ Ensures particles fully stop before hiding
        if (dripParticles != null && !dripParticles.isPlaying)
        {
            dripParticles.gameObject.SetActive(false);
        }
    }



}
