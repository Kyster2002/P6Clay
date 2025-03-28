using UnityEngine;
using UnityEngine.InputSystem;

public class PrefabPlacer : MonoBehaviour
{
    public Transform rayOrigin; // Left controller
    public InputActionReference placeAction; // Trigger input
    public PrefabButtonSpawner spawnerRef; // Button panel spawner

    public float maxRayDistance = 100f;
    public LayerMask placementLayers;

    private void OnEnable()
    {
        if (placeAction != null)
        placeAction.action.Enable(); // 👈 force enable it
        placeAction.action.performed += OnPlace;

        if (spawnerRef != null)
        {
            Debug.Log("PrefabPlacer: Listening for prefab selection...");
        }
    }

    private void OnDisable()
    {
        if (placeAction != null)
            placeAction.action.performed -= OnPlace;
    }

    void OnPlace(InputAction.CallbackContext context)
    {
        GameObject prefabToPlace = spawnerRef.selectedPrefab;

        if (prefabToPlace == null)
        {
            Debug.LogWarning("No prefab selected!");
            return;
        }

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        Debug.DrawRay(ray.origin, ray.direction * maxRayDistance, Color.green, 2f);

        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, placementLayers))
        {
            // Place slightly above the surface
            Vector3 spawnPoint = hit.point + Vector3.up * 0.01f;

            // Use prefab's default rotation
            Quaternion spawnRotation = prefabToPlace.transform.rotation;

            GameObject instance = Instantiate(prefabToPlace, spawnPoint, spawnRotation);
            Debug.Log($"Spawned prefab '{instance.name}' at {spawnPoint}");
        }
        else
        {
            Debug.LogWarning("Raycast did not hit a valid surface.");
        }
    }

}
