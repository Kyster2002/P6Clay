using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class AutoSetupPrefab : MonoBehaviour
{
    public Material defaultRippleMaterial;   // Default material for ripple effect
    public ParticleSystem particleSystemPrefab; // Prefab for the particle system
    public Slider globalFillSlider; // Fill slider for scaling
    public GameObject bucketPrefabAssetFromProject;

    [Header("Optional Clay Textures for Ripple Material")]
    public Texture2D clayDiffuseTexture;
    public Texture2D clayNormalTexture;


    [Header("Whitelist of Object Names to Apply Ripple To")]
    public List<string> allowedNames = new List<string> { "Box" }; // Easily editable in Inspector

    private List<GameObject> trackedObjects = new List<GameObject>(); // List of prefabs already configured

    void Update()
    {
        CheckForNewPrefabs();
    }

    void ApplyTexturesToRippleMaterial(Material rippleMat)
    {
        if (rippleMat.HasProperty("_BaseMap") && clayDiffuseTexture != null)
        {
            rippleMat.SetTexture("_BaseMap", clayDiffuseTexture);
        }
        if (rippleMat.HasProperty("_NormalMap") && clayNormalTexture != null)
        {
            rippleMat.SetTexture("_NormalMap", clayNormalTexture);
            rippleMat.EnableKeyword("_NORMALMAP"); // ✅ Enable normal map keyword
        }
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

        DripFillController dripFill = obj.GetComponent<DripFillController>()
                                     ?? obj.AddComponent<DripFillController>();
        dripFill.SetupBucketPrefab(bucketPrefabAssetFromProject);

        WallFillSlider wallFill = obj.GetComponent<WallFillSlider>()
                               ?? obj.AddComponent<WallFillSlider>();

        if (globalFillSlider != null)
        {
            wallFill.fillSlider = globalFillSlider;
            wallFill.selectedDripFillController = dripFill;
        }
        else
        {
            Debug.LogWarning("⚠ Global Fill Slider not assigned in AutoSetupPrefab!");
        }

        // Apply ripple and particle to BOTH Box and Box_Closed
        SetupBoxVariants(obj, dripFill);

        // Find and force "Box_Closed" as the default box
        Transform boxClosed = FindDeepChild(obj.transform, "Box_Closed");
        if (boxClosed != null)
        {
            dripFill.SetTargetBox(boxClosed);
            dripFill.SetFillLevel(0f);

            // ✅ Explicitly assign the correct particle system from the "Box_Closed" hierarchy
            ParticleSystem closedBoxParticles = boxClosed.GetComponentInChildren<ParticleSystem>(true);
            if (closedBoxParticles != null)
            {
                dripFill.dripParticles = closedBoxParticles;
                Debug.Log("✅ Assigned dripParticles from Box_Closed.");
            }
            else
            {
                Debug.LogWarning("⚠ No particle system found under Box_Closed.");
            }

            dripFill.UpdateDripEffect(); // 💥 This ensures ripple shader + scaling are applied
        }
        else
        {
            Debug.LogWarning("⚠ 'Box_Closed' not found during setup.");
        }

        Debug.Log($"✔ {obj.name} - Fully Configured.");
    }



    void SetupBoxVariants(GameObject wall, DripFillController dripFill)
    {
        string[] targets = { "Box", "Box_Closed" };

        foreach (string name in targets)
        {
            Transform box = FindDeepChild(wall.transform, name);
            if (box == null)
            {
                Debug.LogWarning($"⚠ Could not find {name} on {wall.name}");
                continue;
            }

            // Ripple material setup
            MeshRenderer meshRenderer = box.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                bool isWhitelisted = false;
                foreach (string allowed in allowedNames)
                {
                    if (box.name.Contains(allowed))
                    {
                        isWhitelisted = true;
                        break;
                    }
                }

                if (isWhitelisted)
                {
                    Material[] originalMaterials = meshRenderer.sharedMaterials;
                    Material[] updatedMaterials = new Material[originalMaterials.Length];

                    for (int i = 0; i < originalMaterials.Length; i++)
                    {
                        if (i == 0)
                        {
                            Material newRippleMat = new Material(defaultRippleMaterial);

                            if (originalMaterials[i].HasProperty("_BaseColor") && newRippleMat.HasProperty("_BaseColor"))
                            {
                                newRippleMat.SetColor("_BaseColor", originalMaterials[i].GetColor("_BaseColor"));
                            }

                            ApplyTexturesToRippleMaterial(newRippleMat);
                            updatedMaterials[i] = newRippleMat;

                            // First matching box sets rippleMaterial
                            if (dripFill.rippleMaterial == null)
                                dripFill.rippleMaterial = newRippleMat;
                        }
                        else
                        {
                            updatedMaterials[i] = originalMaterials[i];
                        }
                    }

                    meshRenderer.materials = updatedMaterials;
                }
            }

            // Particle system setup
            if (particleSystemPrefab != null && box.GetComponentInChildren<ParticleSystem>() == null)
            {
                ParticleSystem newParticles = Instantiate(particleSystemPrefab, box);
                newParticles.transform.localPosition = new Vector3(0, 0.5f, 0);
                newParticles.transform.localRotation = Quaternion.identity;
                newParticles.transform.localScale = Vector3.one;
                newParticles.gameObject.SetActive(false);

                // First valid assignment
                if (dripFill.dripParticles == null)
                    dripFill.dripParticles = newParticles;

                Debug.Log($"✨ Particle system added to {name}");
            }
        }
    }


    void SetupParticleSystems(GameObject wall, DripFillController dripFill)
    {
        if (particleSystemPrefab == null)
        {
            Debug.LogError("❌ Particle system prefab is missing!");
            return;
        }

        string[] boxNames = { "Box", "Box_Closed" };

        foreach (string boxName in boxNames)
        {
            Transform boxTransform = FindDeepChild(wall.transform, boxName);
            if (boxTransform == null)
            {
                Debug.LogWarning($"⚠ Could not find {boxName} on {wall.name}");
                continue;
            }

            bool alreadyHasParticle = boxTransform.GetComponentInChildren<ParticleSystem>(true);
            if (alreadyHasParticle) continue;

            ParticleSystem newParticles = Instantiate(particleSystemPrefab, boxTransform);
            newParticles.transform.localPosition = new Vector3(0, 0.5f, 0);
            newParticles.transform.localRotation = Quaternion.identity;
            newParticles.transform.localScale = Vector3.one;
            newParticles.gameObject.SetActive(false);

            if (dripFill.dripParticles == null)
                dripFill.dripParticles = newParticles;

            Debug.Log($"✨ Particle system added to {boxName}");
        }
    }



    Transform FindDeepChild(Transform parent, string exactName)
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

}
