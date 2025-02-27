using UnityEngine;
using UnityEngine.EventSystems; // Required for interaction detection
using System.Collections;
using System.Collections.Generic;

public class DripFillController : MonoBehaviour, IPointerClickHandler
{
    public static DripFillController lastSelectedObject = null; // ✅ Tracks last selected object

    public float fillSpeed = 0.2f;
    private float currentFillLevel = 0f;
    private bool isDripping = true;
    private Vector3 originalScale;
    private Vector3 originalPosition;
    public ParticleSystem dripParticles;
    public Material rippleMaterial;

    private static readonly int FillAmount = Shader.PropertyToID("_FillAmount");
    private static readonly int ObjectHeight = Shader.PropertyToID("_ObjectHeight");

    void Start()
    {
        originalScale = transform.localScale;
        originalPosition = transform.position;

        if (rippleMaterial != null)
        {
            rippleMaterial.SetFloat(ObjectHeight, originalScale.y);
        }

        currentFillLevel = 0f;
        transform.localScale = new Vector3(originalScale.x, 0.01f * originalScale.y, originalScale.z);
        ApplyDripEffect();
    }

    void ApplyDripEffect()
    {
        float curvedFill = Mathf.Pow(currentFillLevel, 1.5f);

        // 🔹 Adjust cube scale properly (based on original scale)
        transform.localScale = new Vector3(
            originalScale.x,
            Mathf.Lerp(0.01f * originalScale.y, originalScale.y, currentFillLevel),
            originalScale.z
        );

        transform.position = new Vector3(
            originalPosition.x,
            originalPosition.y + (originalScale.y * (currentFillLevel - 1) / 2),
            originalPosition.z
        );

        // 🔹 Ensure Material is Assigned & Shader Updates
        if (rippleMaterial == null)
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                rippleMaterial = renderer.material; // ✅ Ensure the reference is set
            }
        }

        if (rippleMaterial != null)
        {
            rippleMaterial.SetFloat("_FillAmount", currentFillLevel);
            rippleMaterial.SetFloat("_ObjectHeight", originalScale.y);
        }
        else
        {
            Debug.LogError("❌ Ripple Material is STILL null on: " + gameObject.name);
        }

        // ✅ Ensure the particle system updates properly
        if (dripParticles != null)
        {
            if (currentFillLevel >= 1f && dripParticles.isPlaying)
            {
                dripParticles.Stop();
            }
            else if (currentFillLevel < 1f && !dripParticles.isPlaying)
            {
                dripParticles.Play();
            }
        }
        else
        {
            Debug.LogError("❌ Drip Particles missing on: " + gameObject.name);
        }
    }






    public void SetFillLevel(float value)
    {
        StopAllCoroutines();
        isDripping = false;

        currentFillLevel = Mathf.Clamp(value, 0f, 1f);
        ApplyDripEffect();

        if (currentFillLevel >= 1f && dripParticles.isPlaying)
        {
            dripParticles.Stop();
        }
        else if (currentFillLevel < 1f && !dripParticles.isPlaying)
        {
            dripParticles.Play();
        }
    }


    // ✅ When clicked, this object becomes the selected one
    public void OnPointerClick(PointerEventData eventData)
    {
        lastSelectedObject = this;
        Debug.Log($"✔ {gameObject.name} is now selected for reset.");
    }

    public void ResetDripEffect()
    {
        StopAllCoroutines();
        isDripping = true;
        currentFillLevel = 0f;

        transform.localScale = new Vector3(originalScale.x, 0.01f * originalScale.y, originalScale.z);
        transform.position = originalPosition;

        if (dripParticles != null)
        {
            dripParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            dripParticles.Clear();
            dripParticles.Play();
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

            if (currentFillLevel >= 1f && dripParticles.isPlaying)
            {
                dripParticles.Stop();
            }

            yield return null;
        }
    }
}
