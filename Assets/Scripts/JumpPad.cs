using UnityEngine;
using System.Collections;

public class JumpPad : MonoBehaviour
{
    public float jumpForce = 1000f; // strength of jump
    public float delay = 0.1f;      // jump delay
    private AudioSource _audioSource; // Jump audio component

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        if (!_audioSource)
        {
            Debug.LogWarning("Missing audio for jump pad");
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger Entered"); // Debugging
        if (other.attachedRigidbody)
        {
            StartCoroutine(DelayedJump(other.attachedRigidbody));
        }
    }

    IEnumerator DelayedJump(Rigidbody playerRigidbody)
    {
        yield return new WaitForSeconds(delay);

        if (_audioSource && _audioSource.clip)
        {
            _audioSource.Play();
        }
        
        // Apply an upward force to the Rigidbody after the delay
        Vector3 up = transform.up;
        playerRigidbody.AddForce(up * jumpForce, ForceMode.Impulse);
        Debug.Log("Force Applied");
    }
}