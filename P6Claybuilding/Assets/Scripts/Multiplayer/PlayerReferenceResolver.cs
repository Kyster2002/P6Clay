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
        Debug.Log("✅ All references successfully resolved!");
    }

    private bool AllReferencesFound()
    {
        return rayOrigin != null && spawnerRef != null && wallFillSlider != null;
    }

    private void TrySetupReferences()
    {

        // Find Right Controller
        if (rightController == null)
        {
            GameObject rightHand = GameObject.Find("Right Controller");
            if (rightHand != null)
            {
                rightController = rightHand.transform;
                Debug.Log("✅ Right Controller found globally!");
            }
            else
            {
                Debug.LogWarning("⚠️ Right Controller still not found, retrying...");
            }
        }

        // Find Head Camera
        if (headCamera == null)
        {
            GameObject camera = GameObject.Find("Main Camera");
            if (camera != null)
            {
                headCamera = camera.transform;
                Debug.Log("✅ Main Camera (Head Camera) found globally!");
            }
            else
            {
                Debug.LogWarning("⚠️ Main Camera still not found, retrying...");
            }
        }

        // Find Ray Origin
        if (rayOrigin == null)
        {
            // Search globally if local search fails
            GameObject leftHand = GameObject.Find("Left Controller");
            if (leftHand != null)
            {
                rayOrigin = leftHand.transform;
                Debug.Log("✅ Ray Origin (Left Controller) found globally!");
            }
            else
            {
                Debug.LogWarning("⚠️ Left Controller still not found, retrying...");
            }
        }


        // Find Spawner Ref
        if (spawnerRef == null)
        {
            var eventSystem = GameObject.Find("EventSystem");
            if (eventSystem != null)
            {
                spawnerRef = eventSystem.GetComponentInChildren<PrefabButtonSpawner>();
                if (spawnerRef != null)
                {
                    Debug.Log("✅ PrefabButtonSpawner found!");
                }
            }
        }

        // Find Wall Fill Slider
        if (wallFillSlider == null)
        {
            wallFillSlider = FindObjectOfType<WallFillSlider>();
            if (wallFillSlider != null)
            {
                Debug.Log("✅ WallFillSlider found!");
            }
        }

        // Find Visual Ray
        if (rayOrigin != null && visualRay == null)
        {
            visualRay = rayOrigin.GetComponentInChildren<LineRenderer>();
            if (visualRay != null)
            {
                Debug.Log("✅ Visual Ray (LineRenderer) found!");
            }
        }
    }
}
