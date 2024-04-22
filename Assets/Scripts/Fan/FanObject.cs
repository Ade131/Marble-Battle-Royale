using UnityEngine;

public class FanObject : MonoBehaviour
{
    public float strength = 5f; // Strength of the fan's push

    private void OnTriggerStay(Collider other)
    {
        if (other.attachedRigidbody)
        {
            Vector3 direction = (other.transform.position - transform.position).normalized;
            other.attachedRigidbody.AddForce(direction * strength, ForceMode.Acceleration);
        }
    }
}