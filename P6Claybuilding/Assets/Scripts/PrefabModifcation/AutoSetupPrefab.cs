using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// AutoSetupPrefab: Automatically configures newly instantiated wall prefabs by:
///  1) Adding or finding required components (DripFillController, WallFillSlider)
///  2) Applying the global fill slider reference
///  3) Setting up ripple materials (with optional clay textures)
///  4) Spawning and assigning particle systems
///  5) Selecting “Box_Closed” as the default fill target
/// Runs every frame to detect new “Clone” objects in the scene.
/// </summary>
public class AutoSetupPrefab : MonoBehaviour
{
    [Header("Prefab Configuration")]
    /// <summary>Base ripple shader material to clone for each wall.</summary>
    public Material defaultRippleMaterial;
    /// <summary>Particle system prefab used for drip effects.</summary>
    public ParticleSystem particleSystemPrefab;
    /// <summary>Global UI slider to control fill amount on all walls.</summary>
    public Slider globalFillSlider;
    /// <summary>Bucket prefab to spawn when doing the smooth (no-drip) fill animation.</summary>
    public GameObject bucketPrefabAssetFromProject;

    [Header("Optional Clay Textures for Ripple Material")]
    /// <summary>Albedo texture to apply to the ripple material.</summary>
    public Texture2D clayDiffuseTexture;
    /// <summary>Normal map to apply to the ripple material.</summary>
    public Texture2D clayNormalTexture;

    [Header("Whitelist of Object Names to Apply Ripple To")]
    /// <summary>Only apply ripple shader to objects whose name contains one of these strings.</summary>
    public List<string> allowedNames = new List<string> { "Box" };

    /// <summary>Tracks which prefabs have already been configured to avoid duplicates.</summary>
    private List<GameObject> trackedObjects = new List<GameObject>();

    /// <summary>
    /// Unity Update callback: checks for any newly-instantiated clone prefabs each frame.
    /// </summary>
    void Update()
    {
        CheckForNewPrefabs();
    }

    /// <summary>
    /// Applies the optional clay textures to a cloned ripple material, if the shader supports those properties.
    /// </summary>
    /// <param name="rippleMat">The material instance to configure.</param>
    void ApplyTexturesToRippleMaterial(Material rippleMat)
    {
        if (rippleMat.HasProperty("_BaseMap") && clayDiffuseTexture != null)
        {
            rippleMat.SetTexture("_BaseMap", clayDiffuseTexture);
        }
        if (rippleMat.HasProperty("_NormalMap") && clayNormalTexture != null)
        {
            rippleMat.SetTexture("_NormalMap", clayNormalTexture);
            rippleMat.EnableKeyword("_NORMALMAP"); // enable normal-mapping in shader
        }
    }

/// <summary>
///  Searches the scene for any new wall-prefab clones that haven’t yet been configured.
///  Only configures objects whose name contains "Clone" *and* that actually have a "Box"
///  child (to avoid mis-detecting buckets or other clones), and that haven’t been tracked yet.
///  Skips objects without any renderers (unless they’re buttons).
/// </summary>
void CheckForNewPrefabs()
{
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (GameObject obj in allObjects)
        {
            // Only consider un-tracked clones that have a "Box" child (real walls)
            if (obj.name.Contains("Clone")
                && FindDeepChild(obj.transform, "Box") != null
                && !trackedObjects.Contains(obj))
            {
                Renderer[] childRenderers = obj.GetComponentsInChildren<Renderer>(true);

                if (childRenderers.Length > 0)
                {
                    SetupPrefab(obj);
                    trackedObjects.Add(obj);
                }
                else
                {
                    // Skip invisible clones like UI buttons
                    if (!obj.name.Contains("Button"))
                    {
                        Debug.LogWarning($"⚠ Skipping {obj.name} - No Renderers found in children.");
                    }
                }
            }
        }
    }


    /// <summary>
    /// Configures a single wall prefab instance:
    ///  - Ensures DripFillController and WallFillSlider components exist
    ///  - Wires the global slider and bucket prefab
    ///  - Applies ripple shader and particle systems to both Box variants
    ///  - Defaults the controller to Box_Closed at fill level 0
    /// </summary>
    /// <param name="obj">The prefab GameObject to configure.</param>
    void SetupPrefab(GameObject obj)
    {
        Debug.Log($"🛠 Configuring: {obj.name}");

        // Add or get the drip-fill logic
        DripFillController dripFill = obj.GetComponent<DripFillController>()
                                     ?? obj.AddComponent<DripFillController>();
        dripFill.SetupBucketPrefab(bucketPrefabAssetFromProject);

        // Add or get the slider bridge
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

        // Apply ripple materials & particle spawner to both Box and Box_Closed
        SetupBoxVariants(obj, dripFill);

        // Force Box_Closed as the starting target
        Transform boxClosed = FindDeepChild(obj.transform, "Box_Closed");
        if (boxClosed != null)
        {
            dripFill.SetTargetBox(boxClosed);
            dripFill.SetFillLevel(0f);

            // Assign any existing particle system under Box_Closed
            ParticleSystem closedParticles = boxClosed.GetComponentInChildren<ParticleSystem>(true);
            if (closedParticles != null)
            {
                dripFill.dripParticles = closedParticles;
                Debug.Log("✅ Assigned dripParticles from Box_Closed.");
            }
            else
            {
                Debug.LogWarning("⚠ No particle system found under Box_Closed.");
            }

            // Trigger an initial shader & scale update
            dripFill.UpdateDripEffect();
        }
        else
        {
            Debug.LogWarning("⚠ 'Box_Closed' not found during setup.");
        }

        Debug.Log($"✔ {obj.name} - Fully Configured.");
    }

    /// <summary>
    /// For each Box variant ("Box" and "Box_Closed"):
    ///  - Replaces the first material with a new ripple material (cloned + tinted)
    ///  - Applies clay textures if provided
    ///  - Instantiates the drip particle system prefab (inactive by default)
    /// </summary>
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

            // Ripple material assignment
            MeshRenderer meshRenderer = box.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                // Only replace materials on whitelisted names
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
                            // Clone the default ripple material for the first slot
                            Material newRippleMat = new Material(defaultRippleMaterial);
                            // Copy base color if supported
                            if (originalMaterials[i].HasProperty("_BaseColor") && newRippleMat.HasProperty("_BaseColor"))
                            {
                                newRippleMat.SetColor("_BaseColor", originalMaterials[i].GetColor("_BaseColor"));
                            }
                            // Apply textures and assign
                            ApplyTexturesToRippleMaterial(newRippleMat);
                            updatedMaterials[i] = newRippleMat;

                            // Let the controller know which material to animate
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

            // Particle system instantiation
            if (particleSystemPrefab != null && box.GetComponentInChildren<ParticleSystem>() == null)
            {
                ParticleSystem newParticles = Instantiate(particleSystemPrefab, box);
                newParticles.transform.localPosition = new Vector3(0, 0.5f, 0);
                newParticles.transform.localRotation = Quaternion.identity;
                newParticles.transform.localScale = Vector3.one;
                newParticles.gameObject.SetActive(false);

                // First assignment wins
                if (dripFill.dripParticles == null)
                    dripFill.dripParticles = newParticles;

                Debug.Log($"✨ Particle system added to {name}");
            }
        }
    }

    /// <summary>
    /// Alternative method to ensure each Box variant has a particle system
    /// (not used by default, since SetupBoxVariants already does this).
    /// </summary>
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

            // Skip if already has a particle system
            if (boxTransform.GetComponentInChildren<ParticleSystem>(true))
                continue;

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

    /// <summary>
    /// Recursively finds a child Transform by exact name under a parent hierarchy.
    /// Returns null if not found.
    /// </summary>
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
