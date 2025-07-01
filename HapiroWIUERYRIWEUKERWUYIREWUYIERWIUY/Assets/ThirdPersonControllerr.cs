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
    public AudioClip rollSound;

    [Header("Roll Cooldown")]
    public float rollCooldown = 1.0f;
    private float lastRollTime = -Mathf.Infinity;

    [Header("Attack Lunge Settings")]
    public float lungeForce = 5f;
    public float lungeDuration = 0.2f;
    public LayerMask obstacleLayers;
    public float lungeCheckDistance = 1f;

    [Header("Health System")]
    public PlayerHealth playerHealth;

    private CharacterController controller;
    private Vector3 moveDirection;
    private Vector3 velocity;
    private bool allowMovement = true;
    private bool isRolling = false;
    private bool isAttacking = false;
    private float lungeTimer = 0f;
    private Vector3 lungeDirection;
    private AudioSource audioSource;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
    }

    void Update()
    {
        if (playerHealth != null && playerHealth.GetCurrentHealth() <= 0)
        {
            moveDirection = Vector3.zero;
            return;
        }

        if (playerHealth != null && playerHealth.IsHealing())
        {
            moveDirection = Vector3.MoveTowards(moveDirection, Vector3.zero, deceleration * Time.deltaTime);
            controller.Move(moveDirection * Time.deltaTime);
            return;
        }

        if (isRolling || !allowMovement) return;

        HandleMovementInput();
        HandleRollInput();
    }

    void HandleMovementInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

        ThirdPersonCamera cam = Camera.main.GetComponent<ThirdPersonCamera>();
        bool isLockedOn = cam != null && cam.IsLockedOn() && cam.GetLockOnTarget() != null;

        if (isLockedOn)
        {
            HandleLockedOnMovement(horizontal, vertical, cam);
        }
        else
        {
            HandleFreeMovement(inputDir);
        }

        ApplyMovement();
    }

    void HandleLockedOnMovement(float horizontal, float vertical, ThirdPersonCamera cam)
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

    void HandleFreeMovement(Vector3 inputDir)
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

    void ApplyMovement()
    {
        controller.Move(moveDirection * Time.deltaTime);
        velocity.y += Physics.gravity.y * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleRollInput()
    {
        if (Input.GetKeyDown(rollKey) && Time.time >= lastRollTime + rollCooldown)
        {
            StartCoroutine(PerformRoll());
        }
    }

   IEnumerator PerformRoll()
{
    lastRollTime = Time.time;
    isRolling = true;
    
    // Play roll sound
    if (rollSound != null && audioSource != null)
    {
        audioSource.PlayOneShot(rollSound);
    }

    // Calculate roll direction
    ThirdPersonCamera cam = Camera.main.GetComponent<ThirdPersonCamera>();
    bool isLockedOn = cam != null && cam.IsLockedOn() && cam.GetLockOnTarget() != null;

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

    // Set initial rotation
    if (rollDirection != Vector3.zero)
    {
        transform.rotation = Quaternion.LookRotation(rollDirection);
    }

    // Set temporary invincibility
    if (playerHealth != null)
    {
        playerHealth.SetTemporaryInvincibility(rollDuration);
    }

    // Store initial rotation and handle gravity
    Quaternion initialRotation = transform.rotation;
    float timer = 0f;
    
    while (timer < rollDuration)
    {
        // Preserve vertical velocity (gravity)
        Vector3 move = rollDirection * rollSpeed;
        move.y = velocity.y;
        
        // Apply movement
        controller.Move(move * Time.deltaTime);
        
        // Apply gravity for next frame
        if (!controller.isGrounded)
        {
            velocity.y += Physics.gravity.y * Time.deltaTime;
        }
        else
        {
            velocity.y = 0;
        }

        // Temporary visual rotation (doesn't affect actual character rotation)
        transform.Rotate(Vector3.right, rollRotationSpeed * Time.deltaTime, Space.Self);
        
        timer += Time.deltaTime;
        yield return null;
    }

    // Restore upright rotation
    transform.rotation = Quaternion.Euler(0, initialRotation.eulerAngles.y, 0);
    isRolling = false;
}
    public void ApplyAttackLunge()
    {
        if (playerHealth != null && playerHealth.GetCurrentHealth() <= 0) return;
        StartCoroutine(DelayedLunge());
    }

    private IEnumerator DelayedLunge()
    {
        yield return new WaitForSeconds(0.33f);

        Vector3 origin = transform.position + Vector3.up * 0.5f;
        bool blocked = Physics.Raycast(origin, transform.forward, lungeCheckDistance, obstacleLayers);

        isAttacking = true;
        lungeTimer = lungeDuration;
        allowMovement = false;

        lungeDirection = blocked ? Vector3.zero : transform.forward;

        while (lungeTimer > 0f)
        {
            controller.Move(lungeDirection * lungeForce * Time.deltaTime);
            lungeTimer -= Time.deltaTime;
            yield return null;
        }

        isAttacking = false;
        allowMovement = true;
    }

    public void TakeDamage(int damage)
    {
        if (playerHealth != null && !playerHealth.IsInvincible())
        {
            playerHealth.TakeDamage(damage);
        }
    }

    public bool IsRolling() => isRolling;
    public bool IsAttacking() => isAttacking;
}