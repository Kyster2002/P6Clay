using UnityEngine;

public class CollisionDebugger : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"🛑 Cube Collision with: {collision.gameObject.name}");
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"⚠️ Cube Trigger Entered by: {other.gameObject.name}");
    }
}
