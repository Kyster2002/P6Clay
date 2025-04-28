using UnityEngine;
using UnityEngine.Formats.Alembic.Importer;

/// <summary>
/// AlembicAutoPlay: Automatically plays an Alembic (cached geometry) animation
/// from start to finish via its AlembicStreamPlayer component.
/// </summary>
public class AlembicAutoPlay : MonoBehaviour
{
    /// <summary>
    /// Reference to the AlembicStreamPlayer component that drives the animation.
    /// Can be assigned in the Inspector, or auto-found on Start().
    /// </summary>
    public AlembicStreamPlayer streamPlayer;

    /// <summary>
    /// Unity Start callback: ensures the streamPlayer reference is set,
    /// and resets its playback time to the beginning (0).
    /// </summary>
    void Start()
    {
        // If not assigned in Inspector, grab the component on this GameObject
        if (streamPlayer == null)
            streamPlayer = GetComponent<AlembicStreamPlayer>();

        // If we now have a valid player, reset its time so it plays from the start
        if (streamPlayer != null)
            streamPlayer.CurrentTime = 0f;
    }

    /// <summary>
    /// Unity Update callback: advances the Alembic animation by deltaTime every frame.
    /// </summary>
    void Update()
    {
        if (streamPlayer != null)
            streamPlayer.CurrentTime += Time.deltaTime;
    }
}
