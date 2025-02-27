using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class AutoSetupPrefab : MonoBehaviour
{
    public Material defaultRippleMaterial;
    public ParticleSystem particleSystemPrefab;
    public Slider globalFillSlider;
    private List<GameObject> trackedObjects = new List<GameObject>();

    void Update()
    {
        CheckForNewPrefabs();
    }

    void CheckForNewPrefabs()
    {
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (GameObject obj in allObjects)
        {
            if (!trackedObjects.Contains(obj) && obj.name.Contains("Clone")) // Only process new clones
            {
                // ✅ Ignore Particle Systems completely
                if (obj.GetComponent<ParticleSystem>() != null)
                {
                    continue; // Skip this object silently
                }

                SetupPrefab(obj);
                trackedObjects.Add(obj);
            }
        }
    }



    void SetupPrefab(GameObject obj)
    {
        Debug.Log($"🛠 Auto-configuring: {obj.name}");

        // ✅ Ensure it has a Rigidbody
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = obj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
        }

        // ✅ Ensure it has the required scripts
        DripFillController dripFill = obj.GetComponent<DripFillController>();
        if (dripFill == null)
        {
            dripFill = obj.AddComponent<DripFillController>();
        }

        WallFillSlider sliderScript = obj.GetComponent<WallFillSlider>();
        if (sliderScript == null)
        {
            sliderScript = obj.AddComponent<WallFillSlider>();
        }

        // ✅ Assign Global Slider (if found)
        Slider globalSlider = FindAnyObjectByType<Slider>(); // 🔹 Modern replacement
        if (globalSlider != null)
        {
            sliderScript.fillSlider = globalSlider;
            sliderScript.dripFillController = obj.GetComponent<DripFillController>();
        }

        if (obj.GetComponent<CollisionDebugger>() == null)
        {
            obj.AddComponent<CollisionDebugger>();
        }

        // ✅ Assign Material and ensure DripFillController has reference
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null && defaultRippleMaterial != null)
        {
            renderer.material = defaultRippleMaterial;
            dripFill.rippleMaterial = renderer.material;
        }

        // ✅ Ensure the prefab has a Particle System
        if (particleSystemPrefab != null)
        {
            ParticleSystem existingParticles = obj.GetComponentInChildren<ParticleSystem>();
            if (existingParticles == null)
            {
                ParticleSystem newParticles = Instantiate(particleSystemPrefab, obj.transform);
                dripFill.dripParticles = newParticles;
                Debug.Log($"✨ Added Particle System to {obj.name}");
            }
            else
            {
                dripFill.dripParticles = existingParticles;
            }
        }

        Debug.Log($"✔ {obj.name} is now fully configured.");
    }






}
