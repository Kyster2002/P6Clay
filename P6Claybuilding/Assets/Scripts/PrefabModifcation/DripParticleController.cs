using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class DripParticleController : MonoBehaviour
{
    private ParticleSystem particleSystemComponent;
    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.ShapeModule shapeModule;

    // The target object (e.g., the "Box") that this particle system will follow.
    private Transform targetObject;

    [Header("Drip Settings")]
    [Tooltip("Vertical offset (in world units) above the target object's top for particle spawning.")]
    public float offsetAboveObject = 0.1f;
    [Tooltip("The fixed Y scale for the emitter shape. X and Z will match the target's bounds.")]
    public float fixedShapeY = 0.5f;

    void Awake()
    {
        particleSystemComponent = GetComponent<ParticleSystem>();
        mainModule = particleSystemComponent.main;
        shapeModule = particleSystemComponent.shape;
    }

    /// <summary>
    /// Call this once to assign the target object (e.g., the Box to drip onto)
    /// and adjust the emitter shape accordingly.
    /// </summary>
    /// <param name="target">The target object's Transform.</param>
    public void Setup(Transform target)
    {
        targetObject = target;
        // Parent this particle system under the target so its local coordinate system matches.
        transform.SetParent(targetObject, worldPositionStays: false);
        transform.localPosition = Vector3.zero; // Center it relative to target.
        AdjustShapeFromBounds(); // Adjust the shape to scale with the target.
    }

    void Update()
    {
        if (targetObject != null)
        {
            Renderer rend = targetObject.GetComponent<Renderer>();
            if (rend != null)
            {
                // Use the target's bounds.max to find the top of the object in world space.
                Vector3 topWorldPos = rend.bounds.max;
                // Set the particle system's world position to be centered (in X and Z) at the top,
                // then lift it slightly upward using offsetAboveObject.
                transform.position = new Vector3(
                    topWorldPos.x,
                    topWorldPos.y + offsetAboveObject,
                    topWorldPos.z
                );
            }
        }
    }


    /// <summary>
    /// Adjusts the Shape module's scale so that it exactly matches the target object's bounding
    /// box in X and Z, with the Y dimension set to fixedShapeY.
    /// </summary>
    public void AdjustShapeFromBounds()
    {
        if (targetObject == null)
            return;

        Renderer rend = targetObject.GetComponent<Renderer>();
        if (rend == null)
            return;

        // Get the target's world-space bounds.
        Bounds bounds = rend.bounds;

        // Because the particle system is parented to the target, we need the size in the target's local space.
        // InverseTransformVector converts a world vector into the local space of the target.
        Vector3 localSize = targetObject.InverseTransformVector(bounds.size);

        // Set the shape module's scale:
        // Use localSize.x for width, fixedShapeY for the Y (height of the emission area), and localSize.z for depth.
        shapeModule.shapeType = ParticleSystemShapeType.Box;
        shapeModule.scale = new Vector3(localSize.x, fixedShapeY, localSize.z);

        // Reset shape module's position to center (so the emitter is centered in local space).
        shapeModule.position = Vector3.zero;
    }
}
