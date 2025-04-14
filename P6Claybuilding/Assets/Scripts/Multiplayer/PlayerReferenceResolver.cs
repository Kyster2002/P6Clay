using System.Collections;
using UnityEngine;

public class PlayerReferenceResolver : MonoBehaviour
{
    public Transform rayOrigin { get; private set; }
    public PrefabButtonSpawner spawnerRef { get; private set; }
    public WallFillSlider wallFillSlider { get; private set; }
    public LineRenderer visualRay { get; private set; }
    public Transform rightController { get; private set; }
    public Transform headCamera { get; private set; }

    private bool referencesResolved = false;
    public bool AreReferencesResolved => referencesResolved;

    public event System.Action OnReferencesReady;

    private void Start()
    {
        StartCoroutine(ResolveReferences());
    }

    private IEnumerator ResolveReferences()
    {
        while (!referencesResolved)
        {
            TrySetupReferences();
            referencesResolved = AllReferencesFound();
            yield return new WaitForSecondsRealtime(0.5f);
        }

        OnReferencesReady?.Invoke();
    }

    private bool AllReferencesFound()
    {
        return rayOrigin != null && rightController != null && headCamera != null;
    }

    private void TrySetupReferences()
    {
        if (rightController == null)
            rightController = FindDeepChild(transform, "Right Controller");

        if (headCamera == null)
            headCamera = FindDeepChild(transform, "Main Camera");

        if (rayOrigin == null)
        {
            var xrOrigin = GameObject.Find("XR Origin (XR Rig)");
            if (xrOrigin != null)
            {
                rayOrigin = FindDeepChild(xrOrigin.transform, "Left Controller");
                if (rayOrigin != null)
                    Debug.Log("✅ Found Left Controller!");
                else
                    Debug.LogWarning("⚠️ Could not find Left Controller under XR Origin!");
            }
        }


        if (spawnerRef == null)
        {
            var eventSystem = GameObject.Find("EventSystem");
            if (eventSystem != null)
                spawnerRef = eventSystem.GetComponentInChildren<PrefabButtonSpawner>();
        }

        if (wallFillSlider == null)
        {
            var spawnedMenu = GameObject.Find("SpawnedMenu");  // 🌱 Find your dynamic menu
            if (spawnedMenu != null)
            {
                wallFillSlider = spawnedMenu.GetComponentInChildren<WallFillSlider>(true); // true = even disabled
                if (wallFillSlider != null)
                    Debug.Log("✅ Found WallFillSlider inside SpawnedMenu!");
                else
                    Debug.LogWarning("⚠️ WallFillSlider not found inside SpawnedMenu!");
            }
            else
            {
                Debug.LogWarning("⚠️ SpawnedMenu not found, can't search for WallFillSlider.");
            }
        }

        if (rayOrigin != null && visualRay == null)
        {
            visualRay = rayOrigin.GetComponentInChildren<LineRenderer>(true);
            if (visualRay != null)
            {
                Debug.Log("✅ Found LineRenderer under Left Controller!");

                // 🛠️ Parent and reset position here:
                visualRay.transform.SetParent(rayOrigin);
                visualRay.transform.localPosition = Vector3.zero;
                visualRay.transform.localRotation = Quaternion.identity;
                Debug.Log("✅ VisualRay parented and aligned to Left Controller!");
            }
            else
            {
                Debug.LogWarning("⚠️ No LineRenderer found under Left Controller!");
            }
        }
        // After finding the left controller inside PlayerReferenceResolver.cs

        SimpleLaser simpleLaser = FindObjectOfType<SimpleLaser>();
        if (simpleLaser != null)
        {
            simpleLaser.SetController(rayOrigin);

        }

    }

    private Transform FindDeepChild(Transform parent, string targetName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == targetName)
                return child;

            Transform found = FindDeepChild(child, targetName);
            if (found != null)
                return found;
        }
        return null;
    }
}
