using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class AutoSetupPrefab : MonoBehaviour
{
    public Material defaultRippleMaterial;   // Default material for ripple effect
    public ParticleSystem particleSystemPrefab; // Prefab for the particle system
    public Slider globalFillSlider; // Global UI slider to control fill levels

    private List<GameObject> trackedObjects = new List<GameObject>(); // List of prefabs that have already been configured

    void Update()
    {
        CheckForNewPrefabs();
    }

    void CheckForNewPrefabs()
    {
        // Find all objects in the scene
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (GameObject obj in allObjects)
        {
            // Only process new prefabs that contain "Clone" in their name (instantiated objects)
            if (!trackedObjects.Contains(obj) && obj.name.Contains("Clone"))
            {
                if (obj.GetComponent<Renderer>() == null)
                {
                    Debug.LogWarning($"⚠ Skipping {obj.name} - No Renderer detected.");
                    continue;
                }

                SetupPrefab(obj);
                trackedObjects.Add(obj);
            }
        }
    }

    void SetupPrefab(GameObject obj)
    {
        Debug.Log($"🛠 Configuring: {obj.name}");

        // ✅ Add DripFillController ONLY if the object is NOT a particle system
        if (obj.GetComponent<MeshRenderer>() != null)
        {
            DripFillController dripFill = obj.GetComponent<DripFillController>() ?? obj.AddComponent<DripFillController>();

            // ✅ Assign material only if MeshRenderer exists
            if (defaultRippleMaterial != null)
            {
                Material newMat = new Material(defaultRippleMaterial);
                obj.GetComponent<MeshRenderer>().material = newMat;
                dripFill.rippleMaterial = newMat;
            }

            // ✅ Ensure it has a WallFillSlider
            WallFillSlider sliderScript = obj.GetComponent<WallFillSlider>() ?? obj.AddComponent<WallFillSlider>();
            if (globalFillSlider != null)
            {
                sliderScript.fillSlider = globalFillSlider;
                sliderScript.dripFillController = dripFill;
            }

            // ✅ Assign Particle System (NEXT STEP)
            SetupParticleSystem(obj, dripFill);
        }
        else
        {
            Debug.Log($"⚠ Skipping {obj.name} - It appears to be a particle system.");
        }

        Debug.Log($"✔ {obj.name} - Fully Configured.");
    }




    void SetupParticleSystem(GameObject obj, DripFillController dripFill)
    {
        if (particleSystemPrefab == null)
        {
            Debug.LogError($"❌ {obj.name} - Particle system prefab is missing! Assign it in the Inspector.");
            return;
        }

        // ✅ Ensure the prefab does NOT already have a particle system
        if (dripFill.dripParticles != null)
        {
            Debug.Log($"✔ {obj.name} - Using existing Particle System.");
            return;
        }

        // ✅ Instantiate as a child, but do NOT add DripFillController
        ParticleSystem newParticles = Instantiate(particleSystemPrefab, obj.transform);
        newParticles.transform.localPosition = new Vector3(0, 0.85f, 0);
        newParticles.transform.localRotation = Quaternion.identity;

        // ✅ Prevent scaling issues
        newParticles.transform.localScale = Vector3.one;

        // ✅ Assign it to DripFillController, but don't modify it in that script
        dripFill.dripParticles = newParticles;

        // ✅ Disable it by default
        newParticles.gameObject.SetActive(false);

        Debug.Log($"✨ {obj.name} - Added Particle System above object.");
    }


}
