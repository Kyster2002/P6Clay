using UnityEngine;
using UnityEngine.Formats.Alembic.Importer;

public class AlembicAutoPlay : MonoBehaviour
{
    public AlembicStreamPlayer streamPlayer;

    void Start()
    {
        if (streamPlayer == null)
        {
            streamPlayer = GetComponent<AlembicStreamPlayer>();
        }

        if (streamPlayer != null)
        {
            streamPlayer.CurrentTime = 0f; // ✅ Reset the animation when spawned
        }
    }

    void Update()
    {
        if (streamPlayer != null)
        {
            streamPlayer.CurrentTime += Time.deltaTime;
        }
    }
}
