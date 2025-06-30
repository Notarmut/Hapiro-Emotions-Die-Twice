using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonControllerr : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float acceleration = 10f;
    public float deceleration = 15f;

    [Header("Camera Reference")]
    public Transform cameraTransform;

    [Header("Roll Settings")]
    public KeyCode rollKey = KeyCode.Space;
    public float rollSpeed = 8f;
    public float rollRotationSpeed = 720f;
    public float rollDuration = 0.6f;
    public AudioClip rollSound; // Roll sound effect

    [Header("Roll Cooldown")]
    public float rollCooldown = 1.0f;
    private float lastRollTime = -Mathf.Infinity;

    [Header("Attack Lunge Settings")]
    public float lungeForce = 5f;
    public float lungeDuration = 0.2f;
    public LayerMask obstacleLayers;
    public float lungeCheckDistance = 1f;

    private bool isAttacking = false;
    private float lungeTimer = 0f;
    private Vector3 lungeDirection;

    private CharacterController controller;
    private Vector3 moveDirection;
    private Vector3 velocity;

    private bool allowMovement = true;
    private bool isRolling = false;
    private AudioSource audioSource; // Audio source for roll sound

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        
        // Get or create audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Update()
    {
        if (isRolling)
        {
            transform.Rotate(Vector3.right, rollRotationSpeed * Time.deltaTime);
            return;
        }

        if (isAttacking)
        {
            controller.Move(lungeDirection * lungeForce * Time.deltaTime);
            lungeTimer -= Time.deltaTime;
            if (lungeTimer <= 0f)
            {
                isAttacking = false;
                allowMovement = true;
            }
            return;
        }

        if (!allowMovement)
            return;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

        ThirdPersonCamera cam = Camera.main.GetComponent<ThirdPersonCamera>();
        bool isLockedOn = cam != null && cam.IsLockedOn() && cam.GetLockOnTarget() != null;

        if (Input.GetKeyDown(rollKey) && Time.time >= lastRollTime + rollCooldown)
        {
            StartCoroutine(PerformRoll());
            return;
        }

        if (isLockedOn)
        {
            Vector3 dirToTarget = cam.GetLockOnTarget().position - transform.position;
            dirToTarget.y = 0f;
            if (dirToTarget != Vector3.zero)
            {
                Quaternion lookRot = Quaternion.LookRotation(dirToTarget);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * rotationSpeed);
            }

            Vector3 desiredMove = transform.right * horizontal + transform.forward * vertical;
            desiredMove.Normalize();
            moveDirection = Vector3.MoveTowards(moveDirection, desiredMove * moveSpeed, acceleration * Time.deltaTime);
        }
        else
        {
            if (inputDir.magnitude >= 0.1f)
            {
                float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
                Quaternion targetRot = Quaternion.Euler(0f, targetAngle, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);

                Vector3 desiredDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                moveDirection = Vector3.MoveTowards(moveDirection, desiredDir * moveSpeed, acceleration * Time.deltaTime);
            }
            else
            {
                moveDirection = Vector3.MoveTowards(moveDirection, Vector3.zero, deceleration * Time.deltaTime);
            }
        }

        controller.Move(moveDirection * Time.deltaTime);
        velocity.y += Physics.gravity.y * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    public void ApplyAttackLunge()
    {
        StartCoroutine(DelayedLunge());
    }

    private IEnumerator DelayedLunge()
    {
        yield return new WaitForSeconds(0.33f);

        Vector3 origin = transform.position + Vector3.up * 0.5f;
        bool blocked = Physics.Raycast(origin, transform.forward, lungeCheckDistance, obstacleLayers);

        isAttacking = true;
        lungeTimer = lungeDuration;

        if (blocked)
        {
            Debug.Log("Lunge engellendi: Engel algılandı.");
            lungeDirection = Vector3.zero;
        }
        else
        {
            lungeDirection = transform.forward;
        }

        allowMovement = false;
    }

    IEnumerator PerformRoll()
    {
        lastRollTime = Time.time;
        isRolling = true;
        
        // Play roll sound effect
        if (rollSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(rollSound);
        }

        ThirdPersonCamera cam = Camera.main.GetComponent<ThirdPersonCamera>();
        bool isLockedOn = cam != null && cam.IsLockedOn() && cam.GetLockOnTarget() != null;
        PlayerHealth health = GetComponent<PlayerHealth>();

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

        Vector3 rollDirection;

        if (isLockedOn)
        {
            rollDirection = transform.right * horizontal + transform.forward * vertical;
        }
        else
        {
            rollDirection = cameraTransform.right * horizontal + cameraTransform.forward * vertical;
        }

        rollDirection.y = 0f;

        if (inputDir == Vector3.zero)
        {
            rollDirection = -transform.forward;
        }

        rollDirection.Normalize();

        if (rollDirection != Vector3.zero)
        {
            Quaternion rollRot = Quaternion.LookRotation(rollDirection);
            transform.rotation = rollRot;
        }

        if (health != null)
        {
            health.SetTemporaryInvincibility(rollDuration);
        }

        float timer = 0f;
        while (timer < rollDuration)
        {
            controller.Move(rollDirection * rollSpeed * Time.deltaTime);
            transform.Rotate(Vector3.right, rollRotationSpeed * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }

        isRolling = false;
    }

    public bool IsRolling() => isRolling;
}