using UnityEngine;

public class WaterRotater : MonoBehaviour
{
    public float angle = 15f;
    public float speed = 2f;

    Quaternion baseRot;

    private void Start()
    {
        baseRot = transform.rotation;
    }

    private void Update()
    {
        float z = Mathf.Sin(Time.time * speed) * angle;
        transform.localRotation = baseRot * Quaternion.Euler(0, 0, z);
    }
}
