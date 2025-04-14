using UnityEngine;

public class SimpleLaser : MonoBehaviour
{
    public float laserLength = 10f;

    private Transform controllerTransform;
    private LineRenderer lr;

    void Start()
    {
        lr = GetComponent<LineRenderer>();
    }

    void Update()
    {
        if (controllerTransform == null || lr == null)
            return;

        Vector3 start = controllerTransform.position;
        Vector3 end = start + controllerTransform.forward * laserLength;

        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
    }

    public void SetController(Transform controller)
    {
        controllerTransform = controller;
    }
}
