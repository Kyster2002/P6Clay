using UnityEngine;

public class LateParentFixer : MonoBehaviour
{
    public string parentNameToFind = "Left Controller";
    private bool parented = false;
    private Transform playerRoot;

    void Start()
    {
        var realtimeView = GetComponentInParent<Normal.Realtime.RealtimeView>();
        if (realtimeView != null && realtimeView.isOwnedLocallyInHierarchy)
        {
            playerRoot = realtimeView.transform;
        }
        else
        {
            Debug.LogWarning("⚠️ LateParentFixer: Could not find local player root.");
        }
    }

    void Update()
    {
        if (parented || playerRoot == null)
            return;

        Transform target = FindDeepChild(playerRoot, parentNameToFind);
        if (target != null)
        {
            transform.SetParent(target);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            parented = true;

            Debug.Log($"✅ {gameObject.name} parented to {parentNameToFind} under your LocalPlayer!");
        }
    }

    private Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;

            Transform found = FindDeepChild(child, name);
            if (found != null)
                return found;
        }
        return null;
    }
}
