using UnityEngine;

public class PhysicsRaycastBlock : MonoBehaviour
{
    void Awake()
    {
        Collider collider = gameObject.AddComponent<BoxCollider>();
        collider.isTrigger = true;
    }
}
