using UnityEngine;

public class FreeCameraController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 10f;
    public float sprintMultiplier = 2f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;
    public bool invertY = false;

    private float pitch = 0f; // X-axis rotation
    private float yaw = 0f;   // Y-axis rotation

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Initialize yaw/pitch to current rotation
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
    }

    private void OnEnable() => UpdateScheduler.RegisterUpdate(OnUpdate);
    private void OnDisable() => UpdateScheduler.UnregisterUpdate(OnUpdate);

    private void OnUpdate()
    {
        HandleMouseLook();
        HandleMovement();
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch += invertY ? mouseY : -mouseY;
        pitch = Mathf.Clamp(pitch, -90f, 90f);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void HandleMovement()
    {
        float moveForward = Input.GetAxis("Vertical");   // W/S
        float moveRight = Input.GetAxis("Horizontal");   // A/D

        // New vertical movement
        float moveUp = 0f;
        if (Input.GetKey(KeyCode.E)) moveUp += 1f;   // ascend
        if (Input.GetKey(KeyCode.Q)) moveUp -= 1f;   // descend

        Vector3 move = transform.forward * moveForward
                     + transform.right * moveRight
                     + transform.up * moveUp;

        if (move.sqrMagnitude > 1f)
            move.Normalize();

        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift)) speed *= sprintMultiplier;

        transform.position += move * speed * Time.deltaTime;
    }
}
