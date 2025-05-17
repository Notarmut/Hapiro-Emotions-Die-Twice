using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonControllerr : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float acceleration = 20f;
    public float deceleration = 25f;

    [Header("Camera")]
    public Transform cameraTransform;

    private CharacterController controller;
    private Vector3 currentVelocity;
    private Vector3 velocity;
    private Vector3 inputDirection;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HandleMovement();
        ApplyGravity();
    }

    void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        inputDirection = new Vector3(horizontal, 0f, vertical).normalized;

        Vector3 targetDirection = Vector3.zero;

        ThirdPersonCamera cam = Camera.main.GetComponent<ThirdPersonCamera>();

        if (cam.IsLockedOn() && cam.GetLockOnTarget() != null)
        {
            // Rotate to face enemy
            Vector3 dirToEnemy = cam.GetLockOnTarget().position - transform.position;
            dirToEnemy.y = 0f;
            Quaternion lookRot = Quaternion.LookRotation(dirToEnemy);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * rotationSpeed);

            targetDirection = transform.right * horizontal + transform.forward * vertical;
        }
        else
        {
            if (inputDirection.magnitude >= 0.1f)
            {
                float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
                Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);

                targetDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            }
        }

        targetDirection.Normalize();
        float targetSpeed = (inputDirection.magnitude > 0.1f) ? moveSpeed : 0f;

        // Smooth acceleration and deceleration
        currentVelocity = Vector3.MoveTowards(currentVelocity, targetDirection * targetSpeed,
            (inputDirection.magnitude > 0.1f ? acceleration : deceleration) * Time.deltaTime);

        controller.Move(currentVelocity * Time.deltaTime);
    }

    void ApplyGravity()
    {
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f; // small stick to ground

        velocity.y += Physics.gravity.y * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
