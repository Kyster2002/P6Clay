using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class DripParticleController : MonoBehaviour
{
    private ParticleSystem particleSystemComponent;
    private Transform targetObject;
    private ParticleSystem.MainModule mainModule;

    [Header("Drip Settings")]
    public float offsetAboveObject = 0.1f;

    void Awake()
    {
        particleSystemComponent = GetComponent<ParticleSystem>();
        mainModule = particleSystemComponent.main;
    }

    public void Setup(Transform target)
    {
        targetObject = target;
        AdjustShape();
    }

    void Update()
    {
        if (targetObject != null)
        {
            Renderer rend = targetObject.GetComponent<Renderer>();
            if (rend != null)
            {
                Vector3 boundsTop = rend.bounds.max;
                transform.position = boundsTop + Vector3.up * offsetAboveObject;
            }
        }
    }

    void AdjustShape()
    {
        if (targetObject == null) return;

        Renderer rend = targetObject.GetComponent<Renderer>();
        if (rend == null) return;

        var shape = particleSystemComponent.shape;
        shape.shapeType = ParticleSystemShapeType.Box;

        // Set the box scale to match the target's X and Z, Y stays 0.5
        shape.scale = new Vector3(
            rend.bounds.size.x,
            0.5f,                  // Fixed Y scale
            rend.bounds.size.z
        );
    }
}
