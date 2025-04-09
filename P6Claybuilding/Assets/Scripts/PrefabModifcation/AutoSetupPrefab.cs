using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class AutoSetupPrefab : MonoBehaviour
{
    public Material defaultRippleMaterial;   // Default material for ripple effect
    public ParticleSystem particleSystemPrefab; // Prefab for the particle system
    public Slider globalFillSlider; // ✅ Fill slider for scaling

    [Header("Whitelist of Object Names to Apply Ripple To")]
    public List<string> allowedNames = new List<string> { "Box" }; // ✅ Easily editable in Inspector

    private List<GameObject> trackedObjects = new List<GameObject>(); // List of prefabs already configured

    void Update()
    {
        CheckForNewPrefabs();
    }

    void CheckForNewPrefabs()
    {
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Clone") && !trackedObjects.Contains(obj))
            {
                Renderer[] childRenderers = obj.GetComponentsInChildren<Renderer>(true);

                if (childRenderers.Length > 0)
                {
                    SetupPrefab(obj);
                    trackedObjects.Add(obj);
                }
                else
                {
                    // If the object name contains "Button", skip without logging a warning.
                    if (!obj.name.Contains("Button"))
                    {
                        Debug.LogWarning($"⚠ Skipping {obj.name} - No Renderers found in children.");
                    }
                }
            }
        }
    }


    void SetupPrefab(GameObject obj)
    {
        Debug.Log($"🛠 Configuring: {obj.name}");

        MeshRenderer firstRenderer = obj.GetComponentInChildren<MeshRenderer>();

        if (firstRenderer != null)
        {
            DripFillController dripFill = obj.GetComponent<DripFillController>() ?? obj.AddComponent<DripFillController>();
            WallFillSlider wallFill = obj.GetComponent<WallFillSlider>() ?? obj.AddComponent<WallFillSlider>();

            // 🔵 Whitelist for naming check
            bool isBox = obj.name.Contains("Box") || firstRenderer.gameObject.name.Contains("Box");

            if (isBox)
            {
                // 🔵 Handle multiple materials correctly
                Material[] originalMaterials = firstRenderer.sharedMaterials; // Preserve all original materials
                Material[] updatedMaterials = new Material[originalMaterials.Length];

                for (int i = 0; i < originalMaterials.Length; i++)
                {
                    if (i == 0) // Only replace first material (or whichever index your Box material is)
                    {
                        Material newRippleMat = new Material(defaultRippleMaterial);

                        // ✅ Copy color from original material
                        if (originalMaterials[i].HasProperty("_BaseColor") && newRippleMat.HasProperty("_BaseColor"))
                        {
                            newRippleMat.SetColor("_BaseColor", originalMaterials[i].GetColor("_BaseColor"));
                        }

                        updatedMaterials[i] = newRippleMat;

                        dripFill.rippleMaterial = newRippleMat; // Save the ripple material
                    }
                    else
                    {
                        updatedMaterials[i] = originalMaterials[i]; // Copy untouched materials
                    }
                }

                firstRenderer.materials = updatedMaterials; // Reapply materials array
            }

            // 🔵 Assign Slider + DripFillController to WallFillSlider
            if (globalFillSlider != null)
            {
                wallFill.fillSlider = globalFillSlider;
                wallFill.selectedDripFillController = dripFill;

            }
            else
            {
                Debug.LogWarning("⚠ Global Fill Slider not assigned in AutoSetupPrefab!");
            }

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

        if (dripFill.dripParticles != null)
        {
            Debug.Log($"✔ {obj.name} - Using existing Particle System.");
            return;
        }

        // 🔥 Place the particle system on top of the Box child if possible
        Transform boxTransform = FindDeepChild(obj.transform, "Box");
        if (boxTransform != null)
        {
            ParticleSystem newParticles = Instantiate(particleSystemPrefab, boxTransform);
            newParticles.transform.localPosition = new Vector3(0, 0.5f, 0); // ⚡ You can tweak this offset
            newParticles.transform.localRotation = Quaternion.identity;
            newParticles.transform.localScale = Vector3.one; // No weird scaling

            dripFill.dripParticles = newParticles;
            newParticles.gameObject.SetActive(false);

            Debug.Log($"✨ {obj.name} - Added Particle System above Box.");
        }
        else
        {
            Debug.LogWarning($"⚠️ {obj.name} - Could not find Box child to parent Particle System to.");
        }
    }

    // Helper method (reuse from DripFillController)
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

}
