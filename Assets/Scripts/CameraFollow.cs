using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private static CameraFollow _singleton;
    public float distance = 8f;             //camera settings
    public float height = 2f;
    private Transform target;
    private float yaw;
    private float pitch;
    public float mouseSensitivity = 5f;
    public float collisionBuffer = 0.5f;    //Buffer distance to avoid clipping with the obstacle
    public LayerMask groundLayer;           //Layer mask to specify which layer to check for collisions

    public static CameraFollow Singleton
    {
        get => _singleton;
        set
        {
            if (value == null)
            {
                _singleton = null;
            }
            else if (_singleton == null)
            {
                _singleton = value;
            }
            else if (_singleton != value)
            {
                Destroy(value);
                Debug.LogError($"There should only ever be one instance of {nameof(CameraFollow)}!");
            }
        }
    }

    private void Awake()
    {
        Singleton = this;
    }

    private void Start()
    {
        pitch = transform.eulerAngles.x;
        yaw = transform.eulerAngles.y;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        HandleCameraRotation();
        HandleCameraPosition();
    }

    //Attach camera movement to mouse
    //Find the camera rotation in order to set W forward direction
    private void HandleCameraRotation()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {


            var mouseX = Input.GetAxis("Mouse X");
            var mouseY = Input.GetAxis("Mouse Y");

            yaw += mouseX * mouseSensitivity;
            pitch -= mouseY * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, -30f, 80f);

            transform.rotation = Quaternion.Euler(pitch, yaw, 0);
        }
    }

    //Don't let camera clip through ground
    private void HandleCameraPosition()
    {
        var desiredPosition = target.position + transform.rotation * new Vector3(0, height, -distance);
        RaycastHit hit;

        //check for a collision between the camera and the target using the 'ground' layer
        if (Physics.Raycast(target.position, desiredPosition - target.position, out hit, distance, groundLayer))
        {
            //adjust the camera position to stop at the collision point minus a buffer
            transform.position = hit.point - (desiredPosition - target.position).normalized * collisionBuffer;
        }
        else
        {
            //if no collision, position the camera at the desired position
            transform.position = desiredPosition;
        }
    }

    private void OnDestroy()
    {
        if (Singleton == this)
            Singleton = null;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
