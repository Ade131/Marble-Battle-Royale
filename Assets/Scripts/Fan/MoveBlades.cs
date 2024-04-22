using UnityEngine;

public class FanBlades : MonoBehaviour
{
    public float rotationSpeed = 300f;  // Rotation speed in degrees per second

    void Update()
    {
        // Rotate the fan blades around their forward axis
        transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
    }
}