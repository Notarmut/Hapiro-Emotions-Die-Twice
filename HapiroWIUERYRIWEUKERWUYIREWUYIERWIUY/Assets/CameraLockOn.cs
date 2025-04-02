using UnityEngine;
using Unity.Cinemachine;
using System.Collections;
using System.Collections.Generic;

public class CameraLockOn : MonoBehaviour
{
    [Header("Lock-On Settings")]
    [SerializeField] float lockOnRadius = 10f;
    [SerializeField] float lockOnAngle = 45f;
    [SerializeField] LayerMask targetLayers;
    [SerializeField] LayerMask obstructionLayers;
    [SerializeField] string targetTag = "Enemy";

    [Header("Camera Settings")]
    [SerializeField] CinemachineVirtualCamera virtualCamera;
    [SerializeField] Transform cameraPivot;
    [SerializeField] float lockOnTransitionTime = 0.3f;

    [Header("Camera Transitions")]
    [SerializeField] float lockOnTransitionDuration = 0.3f;
    [SerializeField] Vector3 lockedShoulderOffset = new Vector3(0.8f, 0, 0);
    [SerializeField] float lockedDamping = 0.1f;

    [Header("Tracking Settings")]
    [SerializeField] float maxLockOnDistance = 15f;
    [SerializeField] float targetHeightOffset = 1.5f;
    [SerializeField] float cameraRotationSpeed = 3f;

    [Header("Debug")]
    [SerializeField] bool showDebugGizmos = true;
    [SerializeField] Color lockOnGizmoColor = Color.yellow;

    // Camera components
    private Cinemachine3rdPersonFollow thirdPersonFollow;
    private Vector3 originalShoulderOffset;
    private Vector3 originalDamping;
    private Transform originalLookAt;
    private float originalCameraDistance;

    // State management
    private Coroutine transitionRoutine;
    private Transform currentTarget;
    private bool isLockedOn;
    private Quaternion originalPivotRotation;

    private CinemachineCameraOffset cameraOffset;
    private Vector3 originalCameraOffset;
    private bool isTransitioning;

    void Start()
    {
        // Add this line
        cameraOffset = virtualCamera.GetComponent<CinemachineCameraOffset>();
        originalCameraOffset = cameraOffset != null ? cameraOffset.Offset : Vector3.zero;

        // Existing initialization
        thirdPersonFollow = virtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
        originalShoulderOffset = thirdPersonFollow.ShoulderOffset;
        originalDamping = thirdPersonFollow.Damping;
        originalLookAt = virtualCamera.LookAt;
        originalCameraDistance = thirdPersonFollow.CameraDistance;
        originalPivotRotation = cameraPivot.localRotation;
    }

    void Update()
    {
        HandleLockOnInput();
        if (isLockedOn) UpdateCameraFocus();
    }

    void HandleLockOnInput()
    {
        if (Input.GetButtonDown("LockOn"))
        {
            if (!isLockedOn) TryAcquireTarget();
            else ReleaseTarget();
        }
    }

    void TryAcquireTarget()
    {
        var validTargets = new List<Transform>();
        var hitColliders = Physics.OverlapSphere(transform.position, lockOnRadius, targetLayers);

        foreach (var col in hitColliders)
        {
            if (col.CompareTag(targetTag))
            {
                // Modified visibility check with height offset
                if (IsTargetVisible(col.transform, targetHeightOffset))
                    validTargets.Add(col.transform);
            }
        }

        if (validTargets.Count > 0)
        {
            currentTarget = GetOptimalTarget(validTargets);
            StartCoroutine(LockOnSequence());
        }
    }

    IEnumerator LockOnSequence()
    {
        isTransitioning = true;

        // 1. Initial camera transition
        yield return StartCoroutine(TransitionCamera(true));

        // 2. Continuous tracking
        while (isLockedOn && currentTarget != null)
        {
            MaintainLockOn();
            yield return null;
        }

        isTransitioning = false;
    }

    void MaintainLockOn()
    {
        // Break lock if target is too far
        if (Vector3.Distance(transform.position, currentTarget.position) > maxLockOnDistance)
        {
            ReleaseTarget();
            return;
        }

        // Update camera focus position with offset
        Vector3 targetPosition = currentTarget.position + Vector3.up * targetHeightOffset;

        // Smooth camera follow
        virtualCamera.LookAt.position = Vector3.Lerp(
            virtualCamera.LookAt.position,
            targetPosition,
            Time.deltaTime * cameraRotationSpeed
        );

        // Optional: Add camera offset adjustments here
        if (cameraOffset != null)
        {
            cameraOffset.Offset = Vector3.Lerp(
                cameraOffset.Offset,
                lockedShoulderOffset,
                Time.deltaTime * 5f
            );
        }
    }

    // Modified visibility check with height offset
    bool IsTargetVisible(Transform target, float heightOffset)
    {
        Vector3 targetPos = target.position + Vector3.up * heightOffset;
        Vector3 dirToTarget = (targetPos - cameraPivot.position).normalized;
        float distanceToTarget = Vector3.Distance(cameraPivot.position, targetPos);

        if (Physics.Raycast(cameraPivot.position, dirToTarget, distanceToTarget, obstructionLayers))
            return false;

        float angle = Vector3.Angle(cameraPivot.forward, dirToTarget);
        return angle < lockOnAngle;
    }

    IEnumerator TransitionCamera(bool lockingOn)
    {
        if (transitionRoutine != null) StopCoroutine(transitionRoutine);

        float elapsed = 0;
        var startOffset = thirdPersonFollow.ShoulderOffset;
        var startDamping = thirdPersonFollow.Damping;
        var startLookAt = virtualCamera.LookAt;

        var targetOffset = lockingOn ? lockedShoulderOffset : originalShoulderOffset;
        var targetDamping = lockingOn ? new Vector3(lockedDamping, lockedDamping, lockedDamping) : originalDamping;
        var targetLookAt = lockingOn ? currentTarget : originalLookAt;

        // Create temporary LookAt target for smooth transition
        GameObject tempLookAt = new GameObject("TempLookAt");
        tempLookAt.transform.position = virtualCamera.LookAt.position;
        virtualCamera.LookAt = tempLookAt.transform;

        while (elapsed < lockOnTransitionDuration)
        {
            float t = Mathf.SmoothStep(0, 1, elapsed / lockOnTransitionDuration);

            // Smoothly move temporary LookAt target
            tempLookAt.transform.position = Vector3.Lerp(
                startLookAt.position,
                targetLookAt.position,
                t
            );

            thirdPersonFollow.ShoulderOffset = Vector3.Lerp(startOffset, targetOffset, t);
            thirdPersonFollow.Damping = Vector3.Lerp(startDamping, targetDamping, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Cleanup temporary object
        Destroy(tempLookAt);
        virtualCamera.LookAt = targetLookAt;

        if (!lockingOn)
        {
            cameraPivot.localRotation = originalPivotRotation;
            currentTarget = null;
        }
    }

    void ReleaseTarget()
    {
        isLockedOn = false;
        StartCoroutine(TransitionCamera(false));
    }

    Transform GetOptimalTarget(List<Transform> targets)
    {
        Transform bestTarget = null;
        float bestScore = Mathf.NegativeInfinity;

        foreach (Transform target in targets)
        {
            Vector3 dirToTarget = (target.position - transform.position).normalized;
            float dot = Vector3.Dot(transform.forward, dirToTarget);
            float distance = Vector3.Distance(transform.position, target.position);

            // Score targets based on alignment and proximity
            float score = dot * 2f + (1 - distance / lockOnRadius);

            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = target;
            }
        }

        return bestTarget;
    }

    bool IsTargetVisible(Transform target)
    {
        Vector3 dirToTarget = (target.position - cameraPivot.position).normalized;
        float distanceToTarget = Vector3.Distance(cameraPivot.position, target.position);

        // Check for line of sight
        if (Physics.Raycast(cameraPivot.position, dirToTarget, distanceToTarget, obstructionLayers))
        {
            return false;
        }

        // Check if within view angle
        float angle = Vector3.Angle(cameraPivot.forward, dirToTarget);
        return angle < lockOnAngle;
    }

    void UpdateCameraFocus()
    {
        if (!isLockedOn || currentTarget == null || isTransitioning) return;

        Vector3 lookDirection = currentTarget.position - cameraPivot.position;
        lookDirection.y = 0;

        // Smooth pivot rotation
        cameraPivot.rotation = Quaternion.Slerp(
            cameraPivot.rotation,
            Quaternion.LookRotation(lookDirection),
            Time.deltaTime * 10f
        );
    }

    void OnDisable()
    {
        // Force reset if disabled during transition
        thirdPersonFollow.ShoulderOffset = originalShoulderOffset;
        thirdPersonFollow.Damping = originalDamping; // Fixed line
        virtualCamera.LookAt = originalLookAt;
    }

    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        Gizmos.color = lockOnGizmoColor;
        Gizmos.DrawWireSphere(transform.position, lockOnRadius);

        if (cameraPivot != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(cameraPivot.position, cameraPivot.position + cameraPivot.forward * lockOnRadius);
        }

        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(cameraPivot.position, currentTarget.position);
        }
    }
}