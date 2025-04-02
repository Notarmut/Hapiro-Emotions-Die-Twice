using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float rotationSpeed = 15f;
    [SerializeField] private float acceleration = 10f;

    [Header("Dash")]
    [SerializeField] private float dashForce = 15f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1.5f;
    private bool _isDashing;
    private float _dashTimer;

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform; // Assign in Inspector

    private CharacterController _controller;
    private Vector3 _moveDirection;
    private float _currentSpeed;

    void Start()
    {
        _controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        HandleMovement();
        HandleDash();
    }

    void HandleMovement()
    {
        // Raw WASD input (no Input System)
        Vector3 input = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) input.z += 1;
        if (Input.GetKey(KeyCode.S)) input.z -= 1;
        if (Input.GetKey(KeyCode.D)) input.x += 1;
        if (Input.GetKey(KeyCode.A)) input.x -= 1;
        Vector3 inputDir = input.normalized;

        // Camera-relative movement
        Vector3 camForward = Vector3.Scale(cameraTransform.forward, new Vector3(1, 0, 1)).normalized;
        _moveDirection = (input.z * camForward + input.x * cameraTransform.right).normalized;

        // Smooth acceleration
        _currentSpeed = Mathf.Lerp(_currentSpeed, moveSpeed, acceleration * Time.deltaTime);

        // Apply movement
        _controller.Move(_moveDirection * _currentSpeed * Time.deltaTime);

        // Rotate player to face direction
        if (_moveDirection != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(_moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }

    void HandleDash()
    {
        _dashTimer -= Time.deltaTime;

        // Dash with Spacebar
        if (Input.GetKey(KeyCode.LeftShift) && !_isDashing && _dashTimer <= 0)
        {
            StartCoroutine(Dash());
        }
    }

    System.Collections.IEnumerator Dash()
    {
        _isDashing = true;
        _dashTimer = dashCooldown;

        Vector3 dashDir = _moveDirection.sqrMagnitude > 0.1f ? _moveDirection : transform.forward;
        float timer = 0;

        while (timer < dashDuration)
        {
            _controller.Move(dashDir * dashForce * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }

        _isDashing = false;
    }
}