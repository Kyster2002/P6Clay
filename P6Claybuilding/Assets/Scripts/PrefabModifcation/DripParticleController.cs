using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class DripParticleController : MonoBehaviour
{
    private ParticleSystem particleSystem;
    private Transform targetObject; // What we're dripping onto
    private ParticleSystem.MainModule mainModule;

    [Header("Drip Settings")]
    public float sizeMultiplier = 0.05f; // How big droplets are relative to object width
    public float speedMultiplier = 0.5f; // How fast droplets fall
    public float gravityMultiplier = 1.5f; // Heavier or lighter feel
    public float offsetAboveObject = 0.1f; // How far above object to hover

    void Awake()
    {
        particleSystem = GetComponent<ParticleSystem>();
        mainModule = particleSystem.main;
    }

    public void Setup(Transform target)
    {
        targetObject = target;
        AdjustParticles();
    }

    void Update()
    {
        if (targetObject != null)
        {
            // Keep particle system above the object
            Renderer rend = targetObject.GetComponent<Renderer>();
            if (rend != null)
            {
                Vector3 boundsTop = rend.bounds.max;
                transform.position = boundsTop + Vector3.up * offsetAboveObject;
            }
        }
    }

    void AdjustParticles()
    {
        if (targetObject == null) return;

        Renderer rend = targetObject.GetComponent<Renderer>();
        if (rend == null) return;

        float width = rend.bounds.size.x;
        float depth = rend.bounds.size.z;
        float minSize = Mathf.Min(width, depth);

        // IMPORTANT: Temporarily stop the system from playing while adjusting
        bool wasPlaying = particleSystem.isPlaying;
        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // Adjust particle size based on object width/depth
        mainModule.startSize = minSize * sizeMultiplier;

        // Adjust speed and gravity
        mainModule.startSpeed = mainModule.startSpeed.constant * speedMultiplier;
        mainModule.gravityModifier = gravityMultiplier;

        // Restore play state (only if it was already playing)
        if (wasPlaying)
        {
            particleSystem.Play();
        }
    }

}
